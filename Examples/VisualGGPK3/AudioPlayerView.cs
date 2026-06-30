using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class AudioPlayerView : Panel {
	private const int SeekScale = 10000;
	private const int VisualBarCount = 14;

	private readonly AudioPlayback playback = new();
	private readonly Drawable visual = new();
	private readonly Label titleLabel = new() {
		Wrap = WrapMode.Word,
		VerticalAlignment = VerticalAlignment.Center
	};
	private readonly Label pathLabel = new() {
		Wrap = WrapMode.Word,
		VerticalAlignment = VerticalAlignment.Center
	};
	private readonly Label statusLabel = new() {
		Text = "Ready",
		VerticalAlignment = VerticalAlignment.Center
	};
	private readonly Label detailsLabel = new() {
		Wrap = WrapMode.Word,
		VerticalAlignment = VerticalAlignment.Top
	};
	private readonly Label currentTimeLabel = new() {
		Text = "0:00",
		Width = 48,
		TextAlignment = TextAlignment.Right
	};
	private readonly Label totalTimeLabel = new() {
		Text = "0:00",
		Width = 48
	};
	private readonly Label volumeValueLabel = new() {
		Text = "100%",
		Width = 40,
		TextAlignment = TextAlignment.Right
	};
	private readonly Slider seekSlider = new() {
		MinValue = 0,
		MaxValue = SeekScale,
		Enabled = false
	};
	private readonly Slider volumeSlider = new() {
		MinValue = 0,
		MaxValue = 100,
		Value = 100
	};
	private readonly Label volumeLabel = new() {
		Text = "Volume",
		Width = 52
	};
	private readonly Button playPauseButton = new() { Text = "Play", Width = 96 };
	private readonly Button stopButton = new() { Text = "Stop", Width = 72, Enabled = false };
	private readonly UITimer positionTimer;
	private bool seekDragging;
	private string? playbackError;
	private int visualSeed;
	private double visualPhase;

	public AudioPlayerView() {
		AppTheme.ApplyPanel(this, raised: true);
		Padding = new Padding(18, 16);
		MinimumSize = new Size(480, 220);

		StyleLabels();
		AppTheme.StyleButton(playPauseButton, ThemeButtonVariant.Primary);
		AppTheme.StyleButton(stopButton, ThemeButtonVariant.Ghost);
		AppTheme.StyleSlider(seekSlider);
		AppTheme.StyleSlider(volumeSlider);

		visual.Size = new Size(76, 76);
		visual.Paint += OnVisualPaint;

		playPauseButton.Click += (_, _) => TogglePlayPauseCore();
		stopButton.Click += (_, _) => Stop();
		seekSlider.ValueChanged += (_, _) => OnSeekSliderChanged();
		seekSlider.MouseDown += (_, _) => seekDragging = true;
		seekSlider.MouseUp += (_, _) => {
			seekDragging = false;
			ApplySeekFromSlider();
		};
		volumeSlider.ValueChanged += (_, _) => {
			playback.Volume = (float)(volumeSlider.Value / 100.0);
			volumeValueLabel.Text = $"{volumeSlider.Value:0}%";
		};

		playback.StateChanged += OnPlaybackStateChanged;
		playback.PlaybackEnded += OnPlaybackEnded;

		positionTimer = new UITimer((_, _) => UpdatePositionUi()) { Interval = 0.1 };

		var headerRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 14,
			VerticalContentAlignment = VerticalAlignment.Center,
			Items = {
				visual,
				new StackLayoutItem(new StackLayout {
					Orientation = Orientation.Vertical,
					Spacing = 4,
					Items = {
						titleLabel,
						pathLabel,
						statusLabel
					}
				}, expand: true)
			}
		};

		var transportButtons = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Items = { playPauseButton, stopButton }
		};

		var seekRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalContentAlignment = VerticalAlignment.Center,
			Items = {
				currentTimeLabel,
				new StackLayoutItem(seekSlider, expand: true),
				totalTimeLabel
			}
		};

		var volumeRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 10,
			VerticalContentAlignment = VerticalAlignment.Center,
			Items = {
				volumeLabel,
				new StackLayoutItem(volumeSlider, expand: true),
				volumeValueLabel
			}
		};

		var transportCard = new Panel { Padding = new Padding(12, 10) };
		AppTheme.ApplyPanel(transportCard, raised: true);
		transportCard.Content = new StackLayout {
			Orientation = Orientation.Vertical,
			Spacing = 10,
			Items = {
				transportButtons,
				seekRow,
				volumeRow
			}
		};

		Content = new StackLayout {
			Orientation = Orientation.Vertical,
			Spacing = 12,
			Items = {
				headerRow,
				transportCard,
				detailsLabel
			}
		};
	}

	public void SetAudio(string fileName, string path, string details, ReadOnlyMemory<byte> data) {
		playbackError = null;
		playback.Load(fileName, data);
		visualSeed = fileName.GetHashCode(StringComparison.OrdinalIgnoreCase);
		titleLabel.Text = fileName;
		pathLabel.Text = path;
		detailsLabel.Text = details;
		volumeSlider.Value = (int)Math.Round(playback.Volume * 100);
		volumeValueLabel.Text = $"{volumeSlider.Value:0}%";
		UpdateTimeLabels();
		UpdateSeekSlider();
		UpdateTransportButtons();
		visual.Invalidate();
	}

	public void Unload() {
		positionTimer.Stop();
		playback.Unload();
		seekDragging = false;
		playbackError = null;
		visualSeed = 0;
		visualPhase = 0;
		titleLabel.Text = "";
		pathLabel.Text = "";
		detailsLabel.Text = "";
		statusLabel.Text = "Ready";
		currentTimeLabel.Text = "0:00";
		totalTimeLabel.Text = "0:00";
		volumeValueLabel.Text = "100%";
		seekSlider.Value = 0;
		UpdateTransportButtons();
		visual.Invalidate();
	}

	public void TogglePlayPause() => TogglePlayPauseCore();

	private void StyleLabels() {
		AppTheme.StyleHeaderLabel(titleLabel);
		titleLabel.Font = new Font(SystemFonts.Bold().Family, SystemFonts.Bold().Size + 1, FontStyle.Bold);
		AppTheme.StyleCaptionLabel(pathLabel);
		AppTheme.StyleHintLabel(statusLabel);
		AppTheme.StyleHintLabel(detailsLabel);
		AppTheme.StyleTimeLabel(currentTimeLabel);
		AppTheme.StyleTimeLabel(totalTimeLabel, muted: true);
		AppTheme.StyleCaptionLabel(volumeLabel);
		AppTheme.StyleCaptionLabel(volumeValueLabel);
	}

	private void TogglePlayPauseCore() {
		if (!playback.CanPlay)
			return;
		try {
			playback.TogglePlayPause();
			if (playback.State == AudioPlaybackState.Playing)
				positionTimer.Start();
			else
				positionTimer.Stop();
			UpdateTransportButtons();
			visual.Invalidate();
		} catch (Exception ex) {
			ShowPlaybackError(ex.Message);
		}
	}

	private void Stop() {
		positionTimer.Stop();
		playback.Stop();
		UpdateSeekSlider();
		UpdateTimeLabels();
		UpdateTransportButtons();
		visual.Invalidate();
	}

	private void OnSeekSliderChanged() {
		if (!seekDragging)
			ApplySeekFromSlider();
		else
			currentTimeLabel.Text = FormatTime(PositionFromSlider());
	}

	private void ApplySeekFromSlider() {
		if (!playback.CanPlay || playback.Duration <= TimeSpan.Zero)
			return;
		try {
			playback.Seek(PositionFromSlider());
			UpdateTimeLabels();
		} catch (Exception ex) {
			ShowPlaybackError(ex.Message);
		}
	}

	private TimeSpan PositionFromSlider() {
		if (playback.Duration <= TimeSpan.Zero)
			return TimeSpan.Zero;
		var ratio = seekSlider.Value / (double)SeekScale;
		return TimeSpan.FromTicks((long)(playback.Duration.Ticks * ratio));
	}

	private void UpdatePositionUi() {
		if (seekDragging)
			return;
		UpdateSeekSlider();
		UpdateTimeLabels();
		if (playback.State == AudioPlaybackState.Playing) {
			visualPhase += 0.22;
			visual.Invalidate();
		}
	}

	private void UpdateSeekSlider() {
		if (seekDragging || playback.Duration <= TimeSpan.Zero) {
			if (playback.Duration <= TimeSpan.Zero)
				seekSlider.Value = 0;
			return;
		}
		var ratio = playback.Position.TotalMilliseconds / playback.Duration.TotalMilliseconds;
		seekSlider.Value = (int)Math.Round(Math.Clamp(ratio, 0, 1) * SeekScale);
	}

	private void UpdateTimeLabels() {
		currentTimeLabel.Text = FormatTime(playback.Position);
		totalTimeLabel.Text = FormatTime(playback.Duration);
	}

	private void UpdateTransportButtons() {
		var canPlay = playback.CanPlay && playbackError is null;
		playPauseButton.Enabled = canPlay;
		stopButton.Enabled = canPlay && playback.State != AudioPlaybackState.Stopped;
		seekSlider.Enabled = canPlay && playback.Duration > TimeSpan.Zero;
		playPauseButton.Text = playback.State == AudioPlaybackState.Playing ? "Pause" : "Play";
		statusLabel.Text = playbackError is not null ? "Playback error"
			: playback.State switch {
				AudioPlaybackState.Playing => "Playing",
				AudioPlaybackState.Paused => "Paused",
				_ => canPlay ? "Ready" : "Unavailable"
			};
	}

	private void OnPlaybackStateChanged() {
		Application.Instance.AsyncInvoke(() => {
			UpdateTransportButtons();
			if (playback.State == AudioPlaybackState.Stopped)
				positionTimer.Stop();
			visual.Invalidate();
		});
	}

	private void OnPlaybackEnded() {
		Application.Instance.AsyncInvoke(() => {
			positionTimer.Stop();
			UpdateSeekSlider();
			UpdateTimeLabels();
			UpdateTransportButtons();
			visual.Invalidate();
		});
	}

	private void ShowPlaybackError(string message) {
		playbackError = message;
		positionTimer.Stop();
		playback.Stop();
		detailsLabel.Text = (detailsLabel.Text ?? "") + Environment.NewLine + "Playback failed: " + message;
		UpdateTransportButtons();
		visual.Invalidate();
	}

	private void OnVisualPaint(object? sender, PaintEventArgs e) {
		var g = e.Graphics;
		var w = Math.Max(1, visual.Width);
		var h = Math.Max(1, visual.Height);
		var bounds = new RectangleF(0, 0, w, h);
		var accent = AppTheme.Accent;
		g.FillRectangle(new Color(accent.R, accent.G, accent.B, 0.14f), bounds);
		g.DrawRectangle(new Color(accent.R, accent.G, accent.B, 0.35f), bounds.X, bounds.Y, bounds.Width, bounds.Height);

		var playing = playback.State == AudioPlaybackState.Playing;
		var gap = 3f;
		var barWidth = Math.Max(2f, (w - gap * (VisualBarCount + 1)) / VisualBarCount);
		for (var i = 0; i < VisualBarCount; i++) {
			var seed = visualSeed + i * 17;
			var baseLevel = 0.22f + Math.Abs(seed % 73) / 100f;
			var anim = playing ? (float)(Math.Sin(visualPhase + i * 0.65) * 0.18) : 0f;
			var level = Math.Clamp(baseLevel + anim, 0.12f, 0.92f);
			var barHeight = h * level;
			var x = gap + i * (barWidth + gap);
			var y = h - barHeight - gap;
			var barColor = playing
				? new Color(accent.R, accent.G, accent.B, 0.92f)
				: new Color(accent.R, accent.G, accent.B, 0.55f);
			g.FillRectangle(barColor, x, y, barWidth, barHeight);
		}
	}

	private static string FormatTime(TimeSpan time) {
		if (time.TotalHours >= 1)
			return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
		return $"{time.Minutes}:{time.Seconds:D2}";
	}
}
