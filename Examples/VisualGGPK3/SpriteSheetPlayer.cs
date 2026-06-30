using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class SpriteSheetPlayer : IDisposable {
	private Bitmap? atlas;
	private Bitmap? frameBitmap;
	private UvSequenceGrid grid;
	private int frameIndex;
	private UITimer? timer;
	private double fps = 12;
	private bool playing;
	private int frameWidth;
	private int frameHeight;

	public bool IsActive { get; private set; }
	public bool IsPlaying => playing;
	public int FrameCount => IsActive ? grid.FrameCount : 0;
	public int FrameIndex => frameIndex;
	public double Fps {
		get => fps;
		set {
			fps = Math.Clamp(value, 1, 60);
			if (timer is not null)
				timer.Interval = 1.0 / fps;
		}
	}

	public Bitmap? CurrentFrame => frameBitmap;

	public event Action? FrameChanged;
	public event Action? StateChanged;

	public bool TryLoad(Bitmap source, string fileName, string? path) {
		Unload();
		if (!UvSequenceGrid.TryParse(fileName, path, out grid))
			return false;
		frameWidth = source.Width / grid.Columns;
		frameHeight = source.Height / grid.Rows;
		if (frameWidth < 1 || frameHeight < 1)
			return false;
		atlas = source;
		frameBitmap = new Bitmap(frameWidth, frameHeight, PixelFormat.Format32bppRgba);
		frameIndex = 0;
		IsActive = true;
		RenderFrame(0);
		return true;
	}

	public void Play() {
		if (!IsActive || atlas is null)
			return;
		timer ??= new UITimer((_, _) => AdvanceFrame()) { Interval = 1.0 / fps };
		timer.Interval = 1.0 / fps;
		timer.Start();
		playing = true;
		StateChanged?.Invoke();
	}

	public void Pause() {
		timer?.Stop();
		playing = false;
		StateChanged?.Invoke();
	}

	public void TogglePlayPause() {
		if (IsPlaying)
			Pause();
		else
			Play();
	}

	public void Stop() {
		Pause();
		if (!IsActive)
			return;
		RenderFrame(0);
		StateChanged?.Invoke();
	}

	public void Seek(int index) {
		if (!IsActive)
			return;
		RenderFrame(Math.Clamp(index, 0, grid.FrameCount - 1));
	}

	public void Unload() {
		Pause();
		timer = null;
		playing = false;
		frameBitmap?.Dispose();
		frameBitmap = null;
		atlas = null;
		frameIndex = 0;
		IsActive = false;
		StateChanged?.Invoke();
	}

	public void Dispose() => Unload();

	private void AdvanceFrame() {
		if (!IsActive)
			return;
		var next = frameIndex + 1;
		if (next >= grid.FrameCount)
			next = 0;
		RenderFrame(next);
	}

	private void RenderFrame(int index) {
		if (atlas is null || frameBitmap is null)
			return;
		frameIndex = index;
		var col = index % grid.Columns;
		var row = index / grid.Columns;
		using var graphics = new Graphics(frameBitmap);
		graphics.Clear(Colors.Transparent);
		graphics.DrawImage(
			atlas,
			new RectangleF(col * frameWidth, row * frameHeight, frameWidth, frameHeight),
			new RectangleF(0, 0, frameWidth, frameHeight));
		FrameChanged?.Invoke();
	}
}
