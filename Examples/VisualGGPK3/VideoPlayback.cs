using System;
using System.IO;
using System.Text;

using LibVLCSharp.Shared;

namespace VisualGGPK3;

internal enum VideoPlaybackState {
	Stopped,
	Playing,
	Paused
}

internal sealed class VideoPlayback : IDisposable {
	private static LibVLC? sharedLibVlc;
	private static readonly object InitLock = new();

	private readonly MediaPlayer player;
	private Media? media;
	private string? tempFilePath;
	private byte[]? data;
	private string extension = "";
	private bool canPlay;

	public VideoPlayback(MediaPlayer mediaPlayer) {
		player = mediaPlayer;
		player.EndReached += OnEndReached;
		player.Playing += OnPlaying;
		player.Paused += OnPaused;
		player.Stopped += OnStopped;
		player.TimeChanged += OnTimeChanged;
	}

	public bool CanPlay => canPlay;
	public MediaPlayer Player => player;
	public VideoPlaybackState State { get; private set; } = VideoPlaybackState.Stopped;

	public TimeSpan Duration {
		get {
			var length = player.Length;
			if (length > 0)
				return TimeSpan.FromMilliseconds(length);
			var mediaDuration = player.Media?.Duration ?? 0;
			return mediaDuration > 0 ? TimeSpan.FromMilliseconds(mediaDuration) : TimeSpan.Zero;
		}
	}

	public TimeSpan Position {
		get => TimeSpan.FromMilliseconds(Math.Max(0, player.Time));
		set {
			if (Duration <= TimeSpan.Zero)
				return;
			var ms = (long)Math.Clamp(value.TotalMilliseconds, 0, Duration.TotalMilliseconds);
			player.Time = ms;
		}
	}

	public int Volume {
		get => player.Volume;
		set => player.Volume = Math.Clamp(value, 0, 100);
	}

	public event Action? StateChanged;
	public event Action? PositionChanged;
	public event Action? PlaybackEnded;

	public static LibVLC SharedLibVlc {
		get {
			EnsureInitialized();
			return sharedLibVlc!;
		}
	}

	public static void EnsureInitialized() {
		lock (InitLock) {
			sharedLibVlc ??= new LibVLC();
		}
	}

	public void Load(string fileName, ReadOnlyMemory<byte> video) {
		UnloadMedia();
		EnsureInitialized();
		extension = Path.GetExtension(fileName);
		data = video.ToArray();
		canPlay = data.Length > 0;
		if (!canPlay)
			return;
		tempFilePath = Path.Combine(Path.GetTempPath(), "VisualGGPK3_" + Guid.NewGuid().ToString("N") + extension);
		File.WriteAllBytes(tempFilePath, data);
		media = new Media(SharedLibVlc, tempFilePath, FromType.FromPath);
		player.Media = media;
		State = VideoPlaybackState.Stopped;
		StateChanged?.Invoke();
	}

	public void PlayOrResume() {
		if (!CanPlay)
			return;
		player.Play();
	}

	public void Pause() {
		if (!CanPlay || State != VideoPlaybackState.Playing)
			return;
		player.Pause();
	}

	public void TogglePlayPause() {
		if (State == VideoPlaybackState.Playing)
			Pause();
		else
			PlayOrResume();
	}

	public void Stop() {
		player.Stop();
		State = VideoPlaybackState.Stopped;
		StateChanged?.Invoke();
		PositionChanged?.Invoke();
	}

	public void Unload() {
		player.Stop();
		UnloadMedia();
		data = null;
		extension = "";
		canPlay = false;
		State = VideoPlaybackState.Stopped;
	}

	public void Dispose() {
		player.EndReached -= OnEndReached;
		player.Playing -= OnPlaying;
		player.Paused -= OnPaused;
		player.Stopped -= OnStopped;
		player.TimeChanged -= OnTimeChanged;
		Unload();
	}

	public static string Describe(string fileName, ReadOnlyMemory<byte> video) {
		var ext = Path.GetExtension(fileName);
		var sb = new StringBuilder();
		sb.AppendLine($"Format: {(string.IsNullOrEmpty(ext) ? "unknown" : ext.TrimStart('.').ToUpperInvariant())}");
		if (video.IsEmpty) {
			sb.AppendLine("File is empty.");
			return sb.ToString().TrimEnd();
		}
		try {
			EnsureInitialized();
			var probePath = Path.Combine(Path.GetTempPath(), "VisualGGPK3_probe_" + Guid.NewGuid().ToString("N") + ext);
			try {
				File.WriteAllBytes(probePath, video.ToArray());
				using var probeMedia = new Media(SharedLibVlc, probePath, FromType.FromPath);
				probeMedia.Parse();
				if (probeMedia.Duration > 0)
					sb.AppendLine($"Duration: {TimeSpan.FromMilliseconds(probeMedia.Duration):hh\\:mm\\:ss\\.fff}");
				foreach (var track in probeMedia.Tracks) {
					if (track.TrackType != TrackType.Video)
						continue;
					var v = track.Data.Video;
					if (v.Width > 0 && v.Height > 0)
						sb.AppendLine($"Resolution: {v.Width} x {v.Height}");
					break;
				}
			} finally {
				try { File.Delete(probePath); } catch { }
			}
		} catch (Exception ex) {
			sb.AppendLine("Could not read video metadata: " + ex.Message);
		}
		return sb.ToString().TrimEnd();
	}

	private void UnloadMedia() {
		player.Media = null;
		media?.Dispose();
		media = null;
		if (tempFilePath is not null) {
			try { File.Delete(tempFilePath); } catch { }
			tempFilePath = null;
		}
	}

	private void OnEndReached(object? sender, EventArgs e) {
		Stop();
		PlaybackEnded?.Invoke();
	}

	private void OnPlaying(object? sender, EventArgs e) {
		State = VideoPlaybackState.Playing;
		StateChanged?.Invoke();
	}

	private void OnPaused(object? sender, EventArgs e) {
		State = VideoPlaybackState.Paused;
		StateChanged?.Invoke();
	}

	private void OnStopped(object? sender, EventArgs e) {
		State = VideoPlaybackState.Stopped;
		StateChanged?.Invoke();
	}

	private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e) => PositionChanged?.Invoke();
}
