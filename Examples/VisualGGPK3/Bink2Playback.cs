using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Eto.Drawing;

namespace VisualGGPK3;

internal sealed class Bink2Playback : IDisposable {
	private IntPtr bink;
	private string? tempFilePath;
	private byte[]? frameBuffer;
	private Bitmap? frameBitmap;
	private int width;
	private int height;
	private uint totalFrames;
	private uint frameRate;
	private uint frameRateDiv;
	private bool playing;
	private bool canPlay;
	private string loadError = "";

	public bool CanPlay => canPlay && bink != IntPtr.Zero;
	public VideoPlaybackState State { get; private set; } = VideoPlaybackState.Stopped;
	public Bitmap? CurrentFrame => frameBitmap;

	public TimeSpan Duration {
		get {
			if (totalFrames == 0 || frameRate == 0)
				return TimeSpan.Zero;
			var seconds = totalFrames * frameRateDiv / (double)frameRate;
			return TimeSpan.FromSeconds(seconds);
		}
	}

	public TimeSpan Position {
		get {
			if (bink == IntPtr.Zero || frameRate == 0)
				return TimeSpan.Zero;
			var info = Bink2Native.ReadInfo(bink);
			var frameIndex = info.FrameNum > 0 ? info.FrameNum - 1 : 0u;
			return TimeSpan.FromSeconds(frameIndex * frameRateDiv / (double)frameRate);
		}
		set {
			if (!CanPlay || frameRate == 0)
				return;
			var targetFrame = (uint)Math.Clamp((int)Math.Round(value.TotalSeconds * frameRate / frameRateDiv) + 1, 1, (int)totalFrames);
			Bink2Native.GotoFrame(bink, targetFrame);
			RenderCurrentFrame();
			PositionChanged?.Invoke();
		}
	}

	public event Action? StateChanged;
	public event Action? PositionChanged;
	public event Action? FrameChanged;
	public event Action? PlaybackEnded;

	public void Load(string fileName, ReadOnlyMemory<byte> video) {
		Unload();
		if (!Bink2Native.IsAvailable) {
			loadError = Bink2Native.MissingDllMessage;
			canPlay = false;
			return;
		}
		if (video.IsEmpty) {
			loadError = "File is empty.";
			canPlay = false;
			return;
		}
		var extension = Path.GetExtension(fileName);
		if (!extension.Equals(".bk2", StringComparison.OrdinalIgnoreCase)) {
			loadError = "Not a BK2 file.";
			canPlay = false;
			return;
		}
		try {
			tempFilePath = Path.Combine(Path.GetTempPath(), "VisualGGPK3_" + Guid.NewGuid().ToString("N") + extension);
			File.WriteAllBytes(tempFilePath, video.ToArray());
			bink = Bink2Native.Open(tempFilePath);
			if (bink == IntPtr.Zero) {
				loadError = "BinkOpen failed. The file may be corrupt or use an unsupported Bink variant.";
				canPlay = false;
				return;
			}
			var info = Bink2Native.ReadInfo(bink);
			width = (int)Math.Max(1, info.Width);
			height = (int)Math.Max(1, info.Height);
			totalFrames = Math.Max(1, info.Frames);
			frameRate = Math.Max(1, info.FrameRate);
			frameRateDiv = Math.Max(1, info.FrameRateDiv);
			frameBuffer = new byte[width * height * 4];
			frameBitmap = CreateBitmapFromBuffer(frameBuffer, width, height);
			Bink2Native.GotoFrame(bink, 1);
			RenderCurrentFrame();
			canPlay = true;
			loadError = "";
			State = VideoPlaybackState.Stopped;
			StateChanged?.Invoke();
		} catch (Exception ex) {
			loadError = ex.Message;
			canPlay = false;
			Unload();
		}
	}

	public void PlayOrResume() {
		if (!CanPlay)
			return;
		Bink2Native.SetPaused(bink, false);
		playing = true;
		State = VideoPlaybackState.Playing;
		StateChanged?.Invoke();
	}

	public void Pause() {
		if (!CanPlay)
			return;
		Bink2Native.SetPaused(bink, true);
		playing = false;
		State = VideoPlaybackState.Paused;
		StateChanged?.Invoke();
	}

	public void TogglePlayPause() {
		if (State == VideoPlaybackState.Playing)
			Pause();
		else
			PlayOrResume();
	}

	public void Stop() {
		if (!CanPlay)
			return;
		Bink2Native.SetPaused(bink, true);
		playing = false;
		Bink2Native.GotoFrame(bink, 1);
		RenderCurrentFrame();
		State = VideoPlaybackState.Stopped;
		StateChanged?.Invoke();
		PositionChanged?.Invoke();
	}

	public bool TickPlayback() {
		if (!CanPlay || !playing)
			return false;
		if (!Bink2Native.IsFrameReady(bink))
			return true;
		if (!Bink2Native.DecodeFrame(bink))
			return true;
		CopyFrameToBitmap();
		var info = Bink2Native.ReadInfo(bink);
		if (info.FrameNum >= totalFrames) {
			Stop();
			PlaybackEnded?.Invoke();
			return false;
		}
		Bink2Native.AdvanceFrame(bink);
		PositionChanged?.Invoke();
		return true;
	}

	public double FrameIntervalSeconds => frameRate == 0 ? 1.0 / 30.0 : frameRateDiv / (double)frameRate;

	public void Unload() {
		playing = false;
		if (bink != IntPtr.Zero) {
			Bink2Native.Close(bink);
			bink = IntPtr.Zero;
		}
		if (tempFilePath is not null) {
			try { File.Delete(tempFilePath); } catch { }
			tempFilePath = null;
		}
		frameBuffer = null;
		frameBitmap?.Dispose();
		frameBitmap = null;
		canPlay = false;
		loadError = "";
		State = VideoPlaybackState.Stopped;
	}

	public void Dispose() => Unload();

	public static string Describe(string fileName, ReadOnlyMemory<byte> video) {
		var sb = new StringBuilder();
		sb.AppendLine("Format: BK2 (Bink 2)");
		if (!Bink2Native.IsAvailable) {
			sb.AppendLine(Bink2Native.MissingDllMessage);
			return sb.ToString().TrimEnd();
		}
		if (video.IsEmpty) {
			sb.AppendLine("File is empty.");
			return sb.ToString().TrimEnd();
		}
		var path = Path.Combine(Path.GetTempPath(), "VisualGGPK3_probe_" + Guid.NewGuid().ToString("N") + ".bk2");
		try {
			File.WriteAllBytes(path, video.ToArray());
			var handle = Bink2Native.Open(path);
			if (handle == IntPtr.Zero) {
				sb.AppendLine("Could not open BK2 file with Bink decoder.");
				return sb.ToString().TrimEnd();
			}
			try {
				var info = Bink2Native.ReadInfo(handle);
				sb.AppendLine($"Resolution: {info.Width} x {info.Height}");
				sb.AppendLine($"Frames: {info.Frames}");
				if (info.FrameRate > 0)
					sb.AppendLine($"Frame rate: {info.FrameRate / (double)Math.Max(1, info.FrameRateDiv):F3} fps");
				var seconds = info.Frames * info.FrameRateDiv / (double)Math.Max(1, info.FrameRate);
				if (seconds > 0)
					sb.AppendLine($"Duration: {TimeSpan.FromSeconds(seconds):hh\\:mm\\:ss\\.fff}");
				sb.AppendLine("Decoder: bink2w64.dll");
				sb.AppendLine($"DLL: {Bink2Locator.TryGetDllPath()}");
			} finally {
				Bink2Native.Close(handle);
			}
		} catch (Exception ex) {
			sb.AppendLine("Could not read BK2 metadata: " + ex.Message);
		} finally {
			try { File.Delete(path); } catch { }
		}
		return sb.ToString().TrimEnd();
	}

	public string GetLoadError() => loadError;

	private void RenderCurrentFrame() {
		if (!CanPlay || frameBuffer is null)
			return;
		if (!Bink2Native.IsFrameReady(bink))
			return;
		if (Bink2Native.DecodeFrame(bink))
			CopyFrameToBitmap();
	}

	private void CopyFrameToBitmap() {
		if (frameBuffer is null || frameBitmap is null || bink == IntPtr.Zero)
			return;
		var handle = GCHandle.Alloc(frameBuffer, GCHandleType.Pinned);
		try {
			if (!Bink2Native.CopyFrameToBuffer(bink, handle.AddrOfPinnedObject(), width, height))
				return;
			frameBitmap.Dispose();
			frameBitmap = CreateBitmapFromBuffer(frameBuffer, width, height);
			FrameChanged?.Invoke();
		} finally {
			handle.Free();
		}
	}

	private static Bitmap CreateBitmapFromBuffer(byte[] rgba, int w, int h) {
		var colors = new Color[w * h];
		for (var i = 0; i < colors.Length; ++i) {
			var o = i * 4;
			colors[i] = Color.FromArgb(rgba[o + 3], rgba[o + 2], rgba[o + 1], rgba[o]);
		}
		return new Bitmap(w, h, PixelFormat.Format32bppRgba, colors);
	}
}
