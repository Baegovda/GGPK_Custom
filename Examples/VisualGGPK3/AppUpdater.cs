using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VisualGGPK3;

internal readonly record struct UpdateInfo(
	Version Latest,
	Version? Minimum,
	string? DownloadUrl,
	string? AssetName);

internal readonly record struct UpdateProgress(string Message, double? Fraction);

internal static class AppUpdater {
	public const string Repo = "Baegovda/GGPK_Custom";
	public const string VersionUrl = "https://raw.githubusercontent.com/Baegovda/GGPK_Custom/main/.github/Version.txt";
	public const string ReleasesUrl = "https://github.com/Baegovda/GGPK_Custom/releases";
	public const string LatestReleaseApiUrl = "https://api.github.com/repos/Baegovda/GGPK_Custom/releases/latest";

	public static Version CurrentVersion {
		get {
			var version = Assembly.GetExecutingAssembly().GetName().Version!;
			return new Version(version.Major, version.Minor, version.Build, version.Revision);
		}
	}

	public static string FormatVersion(Version version) =>
		version.Revision == 0 ? version.ToString(3) : version.ToString(4);

	public static bool IsNewer(Version latest) => latest > CurrentVersion;

	public static async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default) {
		using var http = CreateHttpClient();
		var versionText = await http.GetStringAsync(VersionUrl, cancellationToken).ConfigureAwait(false);
		var lines = versionText.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (lines.Length == 0 || !Version.TryParse(lines[0], out var latest))
			return null;

		Version? minimum = null;
		if (lines.Length > 1 && Version.TryParse(lines[1], out var parsedMinimum))
			minimum = parsedMinimum;

		var (downloadUrl, assetName) = await TryGetDownloadAssetAsync(http, cancellationToken).ConfigureAwait(false);
		return new UpdateInfo(latest, minimum, downloadUrl, assetName);
	}

	public static async Task DownloadAndApplyAsync(
		UpdateInfo info,
		IProgress<UpdateProgress> progress,
		CancellationToken cancellationToken = default) {
		if (string.IsNullOrEmpty(info.DownloadUrl))
			throw new InvalidOperationException("No downloadable release package is available.");

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			throw new PlatformNotSupportedException("In-app updates are only supported on Windows.");

		var installDir = GetInstallDirectory();
		var exePath = Environment.ProcessPath ?? Path.Combine(installDir, "VisualGGPK3.exe");
		var exeName = Path.GetFileName(exePath);

		var stagingRoot = Path.Combine(Path.GetTempPath(), "VisualGGPK3-update");
		Directory.CreateDirectory(stagingRoot);
		var zipPath = Path.Combine(stagingRoot, info.AssetName ?? $"VisualGGPK3-{FormatVersion(info.Latest)}.zip");
		var extractDir = Path.Combine(stagingRoot, $"extract-{FormatVersion(info.Latest)}");

		try {
			if (Directory.Exists(extractDir))
				Directory.Delete(extractDir, true);
			if (File.Exists(zipPath))
				File.Delete(zipPath);

			progress.Report(new UpdateProgress($"Downloading v{FormatVersion(info.Latest)}…", 0));
			await DownloadFileAsync(info.DownloadUrl, zipPath, progress, cancellationToken).ConfigureAwait(false);

			progress.Report(new UpdateProgress("Extracting update…", null));
			ZipFile.ExtractToDirectory(zipPath, extractDir, true);

			var payloadDir = ResolvePayloadDirectory(extractDir);
			progress.Report(new UpdateProgress("Preparing to restart…", null));

			var scriptPath = WriteApplyScript(payloadDir, installDir, exePath, Process.GetCurrentProcess().Id);
			Process.Start(new ProcessStartInfo {
				FileName = "powershell.exe",
				Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
				UseShellExecute = true
			});
		} catch {
			TryDeleteDirectory(extractDir);
			TryDeleteFile(zipPath);
			throw;
		}
	}

	private static HttpClient CreateHttpClient() {
		var http = new HttpClient { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
		http.DefaultRequestHeaders.UserAgent.ParseAdd($"VisualGGPK3/{FormatVersion(CurrentVersion)}");
		return http;
	}

	private static async Task<(string? Url, string? Name)> TryGetDownloadAssetAsync(HttpClient http, CancellationToken cancellationToken) {
		try {
			using var response = await http.GetAsync(LatestReleaseApiUrl, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
				return (null, null);

			await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
			if (!document.RootElement.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
				return (null, null);

			var preferredName = GetPlatformAssetName();
			string? fallbackUrl = null;
			string? fallbackName = null;

			foreach (var asset in assets.EnumerateArray()) {
				if (!asset.TryGetProperty("name", out var nameElement))
					continue;
				var name = nameElement.GetString();
				if (string.IsNullOrEmpty(name) || !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
					continue;
				if (!asset.TryGetProperty("browser_download_url", out var urlElement))
					continue;
				var url = urlElement.GetString();
				if (string.IsNullOrEmpty(url))
					continue;

				if (!string.IsNullOrEmpty(preferredName) && string.Equals(name, preferredName, StringComparison.OrdinalIgnoreCase))
					return (url, name);

				if (name.StartsWith("VisualGGPK3-", StringComparison.OrdinalIgnoreCase)) {
					fallbackUrl ??= url;
					fallbackName ??= name;
				}
			}

			return (fallbackUrl, fallbackName);
		} catch (Exception ex) {
			Debug.WriteLine(ex.Message);
			return (null, null);
		}
	}

	private static string? GetPlatformAssetName() {
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return null;
		return RuntimeInformation.OSArchitecture switch {
			Architecture.Arm64 => "VisualGGPK3-win-arm64.zip",
			_ => "VisualGGPK3-win-x64.zip"
		};
	}

	private static async Task DownloadFileAsync(
		string url,
		string destinationPath,
		IProgress<UpdateProgress> progress,
		CancellationToken cancellationToken) {
		using var http = CreateHttpClient();
		using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		var total = response.Content.Headers.ContentLength;
		await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
		await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

		var buffer = new byte[81920];
		long readTotal = 0;
		int read;
		while ((read = await input.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0) {
			await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
			readTotal += read;
			if (total is > 0) {
				var fraction = Math.Clamp(readTotal / (double)total.Value, 0, 1);
				progress.Report(new UpdateProgress($"Downloading… {fraction * 100:0}%", fraction));
			}
		}
	}

	private static string GetInstallDirectory() {
		var installDir = AppContext.BaseDirectory;
		if (string.IsNullOrEmpty(installDir))
			installDir = Path.GetDirectoryName(Environment.ProcessPath);
		if (string.IsNullOrEmpty(installDir))
			throw new InvalidOperationException("Could not determine the install directory.");
		return Path.GetFullPath(installDir);
	}

	private static string ResolvePayloadDirectory(string extractDir) {
		var files = Directory.GetFiles(extractDir);
		var directories = Directory.GetDirectories(extractDir);
		if (files.Length == 0 && directories.Length == 1)
			return ResolvePayloadDirectory(directories[0]);
		return extractDir;
	}

	private static string WriteApplyScript(string sourceDir, string targetDir, string exePath, int processId) {
		var scriptPath = Path.Combine(Path.GetTempPath(), $"VisualGGPK3-apply-{Guid.NewGuid():N}.ps1");
		var content =
$@"$ErrorActionPreference = 'Stop'
Wait-Process -Id {processId} -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2
robocopy ""{sourceDir}"" ""{targetDir}"" /E /IS /IT /R:5 /W:2 /NFL /NDL /NJH /NJS /NC /NS /NP
if ($LASTEXITCODE -ge 8) {{ exit $LASTEXITCODE }}
Start-Process -FilePath ""{exePath}""
Remove-Item -LiteralPath ""{scriptPath}"" -Force -ErrorAction SilentlyContinue
";
		File.WriteAllText(scriptPath, content);
		return scriptPath;
	}

	private static void TryDeleteDirectory(string path) {
		try {
			if (Directory.Exists(path))
				Directory.Delete(path, true);
		} catch {
			// best effort cleanup
		}
	}

	private static void TryDeleteFile(string path) {
		try {
			if (File.Exists(path))
				File.Delete(path);
		} catch {
			// best effort cleanup
		}
	}
}
