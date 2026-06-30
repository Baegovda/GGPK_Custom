using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Eto.Forms;

namespace VisualGGPK3;

/// <summary>
/// Single append-only diagnostic log for profiling, crash analysis, and stutter detection.
/// Share <see cref="LogFilePath"/> with an agent for automated triage.
/// </summary>
internal static class DiagnosticLog {
	public const string FileName = "diagnostic.log";

	private const long TrimThresholdBytes = 4 * 1024 * 1024;
	private const long TrimKeepBytes = 2 * 1024 * 1024;
	private const int SlowOperationMs = 250;
	private const int StutterGapMs = 800;
	private const int HeartbeatIntervalMs = 30_000;
	private const int UiWatchIntervalMs = 250;

	private static readonly object Gate = new();
	private static readonly Stopwatch Clock = Stopwatch.StartNew();
	private static long sessionStartMs;
	private static string? lastUiContext;
	private static long lastUiTickMs;
	private static UITimer? uiWatchTimer;
	private static Timer? heartbeatTimer;
	private static bool initialized;

	public static string LogFilePath { get; private set; } = "";

	public static void Initialize() {
		if (initialized)
			return;
		initialized = true;

		LogFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"VisualGGPK3",
			FileName);

		try {
			var dir = Path.GetDirectoryName(LogFilePath)!;
			Directory.CreateDirectory(dir);
			TrimIfNeeded();
		} catch {
			return;
		}

		sessionStartMs = Clock.ElapsedMilliseconds;
		WriteRaw(BuildSessionHeader());

		AppDomain.CurrentDomain.UnhandledException += (_, e) => {
			LogCrash("AppDomain.UnhandledException", e.ExceptionObject as Exception, e.IsTerminating);
		};
		TaskScheduler.UnobservedTaskException += (_, e) => {
			LogCrash("TaskScheduler.UnobservedTaskException", e.Exception, isTerminating: false);
			e.SetObserved();
		};

		heartbeatTimer = new Timer(_ => LogHeartbeat(), null, HeartbeatIntervalMs, HeartbeatIntervalMs);
		Write(LogLevel.Info, "session", "Diagnostic logging started", new Dictionary<string, object?> {
			["path"] = LogFilePath
		});
	}

	public static void AttachApplication(Application app) {
		app.UnhandledException += (_, e) => {
			LogCrash("Application.UnhandledException", e.ExceptionObject as Exception, isTerminating: false);
		};
		app.Terminating += (_, _) => LogSessionEnd("terminating");
	}

	public static void StartUiWatchdog() {
		if (uiWatchTimer is not null)
			return;
		lastUiTickMs = Clock.ElapsedMilliseconds;
		uiWatchTimer = new UITimer((_, _) => {
			var now = Clock.ElapsedMilliseconds;
			var gap = now - lastUiTickMs;
			lastUiTickMs = now;
			if (gap >= StutterGapMs)
				Write(LogLevel.Warn, "perf.stutter", "UI thread gap spike", new Dictionary<string, object?> {
					["gap_ms"] = gap,
					["context"] = lastUiContext ?? ""
				});
		}) { Interval = UiWatchIntervalMs / 1000.0 };
	}

	public static void LogSessionEnd(string reason) {
		Write(LogLevel.Info, "session.end", reason, new Dictionary<string, object?> {
			["uptime_ms"] = Clock.ElapsedMilliseconds - sessionStartMs
		});
		Flush();
	}

	public static void Info(string category, string message, IReadOnlyDictionary<string, object?>? data = null) =>
		Write(LogLevel.Info, category, message, data);

	public static void Warn(string category, string message, IReadOnlyDictionary<string, object?>? data = null) =>
		Write(LogLevel.Warn, category, message, data);

	public static void Error(string category, string message, Exception? ex = null, IReadOnlyDictionary<string, object?>? data = null) {
		var merged = data is null ? new Dictionary<string, object?>() : new Dictionary<string, object?>(data);
		if (ex is not null) {
			merged["ex"] = ex.GetType().FullName;
			merged["message"] = ex.Message;
			if (!string.IsNullOrEmpty(ex.StackTrace))
				merged["stack"] = ex.StackTrace.Replace("\r", " ").Replace("\n", " | ");
		}
		Write(LogLevel.Error, category, message, merged);
	}

	public static T Measure<T>(string category, string operation, Func<T> action, IReadOnlyDictionary<string, object?>? data = null) {
		SetUiContext($"{category}.{operation}");
		var sw = Stopwatch.StartNew();
		try {
			return action();
		} catch (Exception ex) {
			Error(category, $"{operation} failed", ex, data);
			throw;
		} finally {
			sw.Stop();
			LogTimed(category, operation, sw.ElapsedMilliseconds, data);
			SetUiContext(null);
		}
	}

	public static void Measure(string category, string operation, Action action, IReadOnlyDictionary<string, object?>? data = null) {
		Measure(category, operation, () => {
			action();
			return 0;
		}, data);
	}

	public static async Task<T> MeasureAsync<T>(string category, string operation, Func<Task<T>> action, IReadOnlyDictionary<string, object?>? data = null) {
		SetUiContext($"{category}.{operation}");
		var sw = Stopwatch.StartNew();
		try {
			return await action().ConfigureAwait(false);
		} catch (Exception ex) {
			Error(category, $"{operation} failed", ex, data);
			throw;
		} finally {
			sw.Stop();
			LogTimed(category, operation, sw.ElapsedMilliseconds, data);
			SetUiContext(null);
		}
	}

	public static async Task MeasureAsync(string category, string operation, Func<Task> action, IReadOnlyDictionary<string, object?>? data = null) {
		await MeasureAsync(category, operation, async () => {
			await action().ConfigureAwait(false);
			return 0;
		}, data).ConfigureAwait(false);
	}

	public static IDisposable BeginScope(string category, string operation, IReadOnlyDictionary<string, object?>? data = null) =>
		new Scope(category, operation, data);

	private static void LogTimed(string category, string operation, long elapsedMs, IReadOnlyDictionary<string, object?>? data) {
		if (elapsedMs < 50 && !category.StartsWith("user", StringComparison.Ordinal))
			return;
		var merged = data is null ? new Dictionary<string, object?> { ["elapsed_ms"] = elapsedMs } : new Dictionary<string, object?>(data) { ["elapsed_ms"] = elapsedMs };
		var level = elapsedMs >= SlowOperationMs ? LogLevel.Warn : LogLevel.Debug;
		var tag = elapsedMs >= SlowOperationMs ? "perf.slow" : "perf";
		Write(level, tag, $"{category}.{operation}", merged);
	}

	public static void User(string action, IReadOnlyDictionary<string, object?>? data = null) =>
		Write(LogLevel.Info, "user", action, data);

	private static void LogHeartbeat() {
		try {
			var proc = Process.GetCurrentProcess();
			GC.RefreshMemoryLimit();
			var info = GC.GetGCMemoryInfo();
			Write(LogLevel.Info, "perf.heartbeat", "runtime snapshot", new Dictionary<string, object?> {
				["uptime_ms"] = Clock.ElapsedMilliseconds - sessionStartMs,
				["working_set_mb"] = Math.Round(proc.WorkingSet64 / (1024.0 * 1024), 1),
				["private_mb"] = Math.Round(proc.PrivateMemorySize64 / (1024.0 * 1024), 1),
				["gc_heap_mb"] = Math.Round(info.HeapSizeBytes / (1024.0 * 1024), 1),
				["gc_fragmented_mb"] = Math.Round(info.FragmentedBytes / (1024.0 * 1024), 1),
				["threadpool_pending"] = ThreadPool.PendingWorkItemCount,
				["threadpool_threads"] = ThreadPool.ThreadCount,
				["ui_context"] = lastUiContext ?? ""
			});
		} catch {
			// ignore heartbeat failures
		}
	}

	private static void LogCrash(string source, Exception? ex, bool isTerminating) {
		var data = new Dictionary<string, object?> {
			["source"] = source,
			["terminating"] = isTerminating,
			["uptime_ms"] = Clock.ElapsedMilliseconds - sessionStartMs,
			["ui_context"] = lastUiContext ?? ""
		};
		Error("session.crash", "Unhandled exception", ex, data);
		Flush();
	}

	private static void SetUiContext(string? context) => lastUiContext = context;

	private static string BuildSessionHeader() {
		var asm = typeof(DiagnosticLog).Assembly.GetName();
		var sb = new StringBuilder();
		sb.AppendLine("# VisualGGPK3 diagnostic.log — share this single file for profiling / bugs / stutter analysis");
		sb.AppendLine("# Columns: timestamp | level | category | message | key=value ...");
		sb.AppendLine("# Levels: DBG INF WRN ERR");
		sb.AppendLine("# Notable categories: session.* perf.slow perf.stutter perf.heartbeat user.* preview.*");
		sb.AppendLine($"# session_start={FormatTimestamp(DateTimeOffset.Now)} pid={Environment.ProcessId}");
		sb.AppendLine($"# version={asm.Version} runtime={Environment.Version} os={Environment.OSVersion}");
		sb.AppendLine($"# log_path={LogFilePath}");
		sb.AppendLine("# ---");
		return sb.ToString();
	}

	private enum LogLevel { Debug, Info, Warn, Error }

	private static void Write(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? data = null) {
		if (string.IsNullOrEmpty(LogFilePath))
			return;
		var line = FormatLine(level, category, message, data);
		WriteRaw(line);
	}

	private static string FormatLine(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? data) {
		var sb = new StringBuilder();
		sb.Append(FormatTimestamp(DateTimeOffset.Now));
		sb.Append(" | ");
		sb.Append(level switch {
			LogLevel.Debug => "DBG",
			LogLevel.Info => "INF",
			LogLevel.Warn => "WRN",
			LogLevel.Error => "ERR",
			_ => "???"
		});
		sb.Append(" | ");
		sb.Append(category);
		sb.Append(" | ");
		sb.Append(Escape(message));
		if (data is not null) {
			foreach (var (key, value) in data) {
				if (string.IsNullOrEmpty(key) || value is null)
					continue;
				sb.Append(' ');
				sb.Append(key);
				sb.Append('=');
				sb.Append(Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));
			}
		}
		return sb.ToString();
	}

	private static string Escape(string value) {
		if (value.IndexOfAny([' ', '|', '\r', '\n', '\t']) < 0)
			return value;
		return '"' + value.Replace("\"", "\\\"") + '"';
	}

	private static string FormatTimestamp(DateTimeOffset time) =>
		time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);

	private static void WriteRaw(string text) {
		lock (Gate) {
			try {
				using var fs = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
				using var writer = new StreamWriter(fs, Encoding.UTF8);
				if (text.Contains('\n', StringComparison.Ordinal)) {
					writer.Write(text);
					if (!text.EndsWith('\n'))
						writer.WriteLine();
				} else {
					writer.WriteLine(text);
				}
				writer.Flush();
				fs.Flush(flushToDisk: true);
			} catch {
				// never throw from logging
			}
		}
	}

	private static void Flush() {
		// writes are flushed per line; nothing extra required
	}

	private static void TrimIfNeeded() {
		if (!File.Exists(LogFilePath))
			return;
		var len = new FileInfo(LogFilePath).Length;
		if (len <= TrimThresholdBytes)
			return;
		try {
			var keep = (int)Math.Min(TrimKeepBytes, len);
			var buffer = new byte[keep];
			using (var read = File.Open(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				read.Seek(-keep, SeekOrigin.End);
				var offset = 0;
				while (offset < keep) {
					var n = read.Read(buffer, offset, keep - offset);
					if (n <= 0)
						break;
					offset += n;
				}
			}
			var text = Encoding.UTF8.GetString(buffer);
			var firstLine = text.IndexOf('\n');
			if (firstLine >= 0)
				text = text[(firstLine + 1)..];
			var header = $"# --- log trimmed {FormatTimestamp(DateTimeOffset.Now)} kept_last_bytes={keep} ---{Environment.NewLine}";
			File.WriteAllText(LogFilePath, header + text, Encoding.UTF8);
		} catch {
			// if trim fails, continue appending
		}
	}

	private sealed class Scope : IDisposable {
		private readonly string category;
		private readonly string operation;
		private readonly IReadOnlyDictionary<string, object?>? data;
		private readonly Stopwatch sw = Stopwatch.StartNew();
		private bool disposed;

		public Scope(string category, string operation, IReadOnlyDictionary<string, object?>? data) {
			this.category = category;
			this.operation = operation;
			this.data = data;
			SetUiContext($"{category}.{operation}");
		}

		public void Dispose() {
			if (disposed)
				return;
			disposed = true;
			sw.Stop();
			LogTimed(category, operation, sw.ElapsedMilliseconds, data);
			SetUiContext(null);
		}
	}
}
