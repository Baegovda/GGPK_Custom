using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class SpriteSheetPlayerView : Panel {
	private readonly SpriteSheetPlayer player = new();
	private readonly Button playPauseButton = new() { Text = "Play", Width = 72 };
	private readonly Button stopButton = new() { Text = "Stop", Width = 72, Enabled = false };
	private readonly Label frameLabel = new() {
		Text = "0 / 0",
		TextColor = Colors.White,
		Width = 72
	};
	private readonly Slider frameSlider = new() {
		MinValue = 0,
		MaxValue = 0,
		Enabled = false
	};
	private readonly NumericStepper fpsStepper = new() {
		MinValue = 1,
		MaxValue = 60,
		Value = 12,
		Width = 56
	};
	private readonly Label fpsLabel = new() {
		Text = "FPS",
		TextColor = new Color(0.8f, 0.8f, 0.82f)
	};
	private bool seekDragging;

	public SpriteSheetPlayerView() {
		AppTheme.StyleButton(playPauseButton, ThemeButtonVariant.Primary);
		AppTheme.StyleButton(stopButton, ThemeButtonVariant.Ghost);
		AppTheme.StyleSlider(frameSlider);

		playPauseButton.Click += (_, _) => player.TogglePlayPause();
		stopButton.Click += (_, _) => player.Stop();
		frameSlider.ValueChanged += (_, _) => OnFrameSliderChanged();
		frameSlider.MouseDown += (_, _) => seekDragging = true;
		frameSlider.MouseUp += (_, _) => {
			seekDragging = false;
			player.Seek(frameSlider.Value);
			UpdateFrameLabel();
		};
		fpsStepper.ValueChanged += (_, _) => player.Fps = fpsStepper.Value;
		player.FrameChanged += OnPlayerFrameChanged;
		player.StateChanged += UpdateTransportButtons;

		var transportRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Items = {
				playPauseButton,
				stopButton,
				frameLabel,
				new StackLayoutItem(frameSlider, expand: true),
				fpsLabel,
				fpsStepper
			}
		};
		Content = transportRow;
		Visible = false;
	}

	public event Action<Bitmap>? FrameChanged;

	public bool TryLoad(Bitmap source, string fileName, string? path) {
		if (!player.TryLoad(source, fileName, path)) {
			Visible = false;
			return false;
		}
		fpsStepper.Value = player.Fps;
		frameSlider.MaxValue = Math.Max(0, player.FrameCount - 1);
		frameSlider.Value = 0;
		frameSlider.Enabled = player.FrameCount > 1;
		UpdateFrameLabel();
		UpdateTransportButtons();
		Visible = true;
		if (player.CurrentFrame is not null)
			FrameChanged?.Invoke(player.CurrentFrame);
		if (player.FrameCount > 1)
			player.Play();
		return true;
	}

	public void Unload() {
		player.Unload();
		Visible = false;
		seekDragging = false;
		frameSlider.Value = 0;
		frameSlider.MaxValue = 0;
		frameLabel.Text = "0 / 0";
		UpdateTransportButtons();
	}

	public void TogglePlayPause() => player.TogglePlayPause();

	public void Pause() => player.Pause();

	public Bitmap? CurrentFrame => player.CurrentFrame;

	public bool IsActive => player.IsActive;

	private void OnFrameSliderChanged() {
		if (seekDragging)
			UpdateFrameLabel();
		else {
			player.Seek(frameSlider.Value);
			UpdateFrameLabel();
		}
	}

	private void OnPlayerFrameChanged() {
		if (seekDragging)
			return;
		frameSlider.Value = player.FrameIndex;
		UpdateFrameLabel();
		if (player.CurrentFrame is not null)
			FrameChanged?.Invoke(player.CurrentFrame);
	}

	private void UpdateFrameLabel() {
		var shown = seekDragging ? frameSlider.Value : player.FrameIndex;
		frameLabel.Text = player.FrameCount > 0
			? $"{shown + 1} / {player.FrameCount}"
			: "0 / 0";
	}

	private void UpdateTransportButtons() {
		playPauseButton.Enabled = player.IsActive;
		stopButton.Enabled = player.IsActive && (player.IsPlaying || player.FrameIndex > 0);
		playPauseButton.Text = player.IsPlaying ? "Pause" : "Play";
	}
}
