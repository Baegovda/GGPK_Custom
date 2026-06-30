using System;
using System.IO;

using NAudio.Vorbis;
using NAudio.Wave;

namespace VisualGGPK3;

internal enum AudioPlaybackState {
	Stopped,
	Playing,
	Paused
}

internal sealed class AudioPlayback : IDisposable {
	private byte[]? data;
	private string extension = "";
	private WaveOutEvent? output;
	private WaveStream? reader;
	private TimeSpan pendingPosition;
	private float volume = 1f;

	public bool CanPlay { get; private set; }
	public TimeSpan Duration { get; private set; }
	public AudioPlaybackState State { get; private set; } = AudioPlaybackState.Stopped;

	public TimeSpan Position {
		get {
			if (reader is not null)
				return reader.CurrentTime;
			return pendingPosition;
		}
	}

	public float Volume {
		get => volume;
		set {
			volume = Math.Clamp(value, 0f, 1f);
			if (output is not null)
				output.Volume = volume;
		}
	}

	public event Action? StateChanged;
	public event Action? PositionChanged;
	public event Action? PlaybackEnded;

	public void Load(string fileName, ReadOnlyMemory<byte> audio) {
		StopInternal(disposeReader: true);
		extension = Path.GetExtension(fileName);
		data = audio.ToArray();
		CanPlay = !extension.Equals(".bank", StringComparison.OrdinalIgnoreCase);
		Duration = CanPlay ? ProbeDuration(data, extension) : TimeSpan.Zero;
		pendingPosition = TimeSpan.Zero;
		State = AudioPlaybackState.Stopped;
		StateChanged?.Invoke();
	}

	public void PlayOrResume() {
		if (!CanPlay || data is null)
			return;
		if (output is null) {
			StartPlayback();
			return;
		}
		if (State == AudioPlaybackState.Paused) {
			output.Play();
			State = AudioPlaybackState.Playing;
			StateChanged?.Invoke();
		}
	}

	public void Pause() {
		if (output is null || State != AudioPlaybackState.Playing)
			return;
		output.Pause();
		State = AudioPlaybackState.Paused;
		StateChanged?.Invoke();
	}

	public void TogglePlayPause() {
		if (State == AudioPlaybackState.Playing)
			Pause();
		else
			PlayOrResume();
	}

	public void Stop() {
		pendingPosition = TimeSpan.Zero;
		StopInternal(disposeReader: true);
		State = AudioPlaybackState.Stopped;
		StateChanged?.Invoke();
		PositionChanged?.Invoke();
	}

	public void Seek(TimeSpan position) {
		if (Duration <= TimeSpan.Zero)
			return;
		position = position < TimeSpan.Zero ? TimeSpan.Zero :
			position > Duration ? Duration : position;
		if (reader is not null)
			reader.CurrentTime = position;
		else
			pendingPosition = position;
		PositionChanged?.Invoke();
	}

	public void Unload() {
		StopInternal(disposeReader: true);
		data = null;
		extension = "";
		CanPlay = false;
		Duration = TimeSpan.Zero;
		pendingPosition = TimeSpan.Zero;
		State = AudioPlaybackState.Stopped;
	}

	public void Dispose() => Unload();

	public static string Describe(string fileName, ReadOnlyMemory<byte> audio) {
		var ext = Path.GetExtension(fileName);
		var sb = new System.Text.StringBuilder();
		sb.AppendLine($"Format: {(string.IsNullOrEmpty(ext) ? "unknown" : ext.TrimStart('.').ToUpperInvariant())}");
		if (ext.Equals(".bank", StringComparison.OrdinalIgnoreCase)) {
			sb.AppendLine("FMOD .bank files cannot be previewed in this viewer.");
			return sb.ToString().TrimEnd();
		}
		try {
			using var stream = new MemoryStream(audio.ToArray(), writable: false);
			using var wave = OpenReader(stream, ext);
			if (wave is not null) {
				sb.AppendLine($"Duration: {wave.TotalTime:hh\\:mm\\:ss\\.fff}");
				sb.AppendLine($"Sample rate: {wave.WaveFormat.SampleRate} Hz");
				sb.AppendLine($"Channels: {wave.WaveFormat.Channels}");
				sb.AppendLine($"Bits per sample: {wave.WaveFormat.BitsPerSample}");
			}
		} catch (Exception ex) {
			sb.AppendLine("Could not read audio metadata: " + ex.Message);
		}
		return sb.ToString().TrimEnd();
	}

	private void StartPlayback() {
		if (data is null)
			return;
		var stream = new MemoryStream(data, writable: false);
		reader = OpenReader(stream, extension) ?? throw new NotSupportedException($"Unsupported audio format: {extension}");
		if (pendingPosition > TimeSpan.Zero)
			reader.CurrentTime = pendingPosition;
		output = new WaveOutEvent { Volume = volume };
		output.Init(reader);
		output.PlaybackStopped += OnPlaybackStopped;
		output.Play();
		State = AudioPlaybackState.Playing;
		StateChanged?.Invoke();
	}

	private void OnPlaybackStopped(object? sender, StoppedEventArgs e) {
		var ended = State == AudioPlaybackState.Playing;
		StopInternal(disposeReader: true);
		pendingPosition = TimeSpan.Zero;
		State = AudioPlaybackState.Stopped;
		StateChanged?.Invoke();
		PositionChanged?.Invoke();
		if (ended)
			PlaybackEnded?.Invoke();
	}

	private void StopInternal(bool disposeReader) {
		if (output is not null) {
			output.PlaybackStopped -= OnPlaybackStopped;
			output.Stop();
			output.Dispose();
			output = null;
		}
		if (disposeReader) {
			reader?.Dispose();
			reader = null;
		}
	}

	private static TimeSpan ProbeDuration(byte[] audio, string extension) {
		try {
			using var stream = new MemoryStream(audio, writable: false);
			using var wave = OpenReader(stream, extension);
			return wave?.TotalTime ?? TimeSpan.Zero;
		} catch {
			return TimeSpan.Zero;
		}
	}

	private static WaveStream? OpenReader(Stream stream, string extension) {
		if (extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase))
			return new VorbisWaveReader(stream);
		if (extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase))
			return new Mp3FileReader(stream);
		if (extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
			return new WaveFileReader(stream);
		return TryOpenReader(stream);
	}

	private static WaveStream? TryOpenReader(Stream stream) {
		if (stream.Length >= 4) {
			Span<byte> header = stackalloc byte[4];
			var read = stream.Read(header);
			stream.Position = 0;
			if (read >= 4) {
				if (header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F')
					return new WaveFileReader(stream);
				if (header[0] == (byte)'O' && header[1] == (byte)'g' && header[2] == (byte)'g' && header[3] == (byte)'S')
					return new VorbisWaveReader(stream);
				if (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0)
					return new Mp3FileReader(stream);
			}
		}
		try { return new WaveFileReader(stream); } catch { stream.Position = 0; }
		try { return new Mp3FileReader(stream); } catch { stream.Position = 0; }
		try { return new VorbisWaveReader(stream); } catch { }
		return null;
	}
}
