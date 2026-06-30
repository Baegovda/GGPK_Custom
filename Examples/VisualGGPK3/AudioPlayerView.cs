using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class AudioPlayerView : Panel {
	private const int SeekScale = 10000;

	private readonly AudioPlayback playback = new();
	private readonly Label titleLabel = new() {
		TextColor = Colors.White,
		Font = SystemFonts.Bold()
	};
	private readonly Label pathLabel = new() {
		TextColor = new Color(0.75f, 0.75f, 0.78f),
		Wrap = WrapMode.Word
	};
	private readonly Label detailsLabel = new() {
		TextColor = new Color(0.7f, 0.7f, 0.74f),
		Wrap = WrapMode.Word
	};
	private readonly Label currentTimeLabel = new() {
		Text = "0:00",
		TextColor = Colors.White,
		Width = 44
	};
	private readonly Label totalTimeLabel = new() {
		Text = "0:00",
		TextColor = Colors.White,
		Width = 44
	};
	private readonly Slider seekSlider = new() {
		MinValue = 0,
		MaxValue = SeekScale,
		Enabled = false
	};
	private readonly Slider volumeSlider = new() {
		MinValue = 0,
		MaxValue = 100,
		Value = 100,
		Width = 110
	};
	private readonly Label volumeLabel = new() {
		Text = "Vol",
		TextColor = new Color(0.8f, 0.8f, 0.82f)
	};
	private readonly Button playPauseButton = new() { Text = "Play", Width = 72 };
	private readonly Button stopButton = new() { Text = "Stop", Width = 72, Enabled = false };
	private readonly UITimer positionTimer;
	private bool seekDragging;
	private string? playbackError;

	public AudioPlayerView() {
		BackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.94f);
		Padding = new Padding(16);
		MinimumSize = new Size(420, 168);

		playPauseButton.Click += (_, _) => TogglePlayPauseCore();
		stopButton.Click += (_, _) => Stop();
		seekSlider.ValueChanged += (_, _) => OnSeekSliderChanged();
		seekSlider.MouseDown += (_, _) => seekDragging = true;
		seekSlider.MouseUp += (_, _) => {
			seekDragging = false;
			ApplySeekFromSlider();
		};
		volumeSlider.ValueChanged += (_, _) => playback.Volume = (float)(volumeSlider.Value / 100.0);

		playback.StateChanged += OnPlaybackStateChanged;
		playback.PlaybackEnded += OnPlaybackEnded;

		positionTimer = new UITimer((_, _) => UpdatePositionUi()) { Interval = 0.1 };

		var transportRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Items = {
				playPauseButton,
				stopButton,
				currentTimeLabel,
				new StackLayoutItem(seekSlider, expand: true),
				totalTimeLabel
			}
		};

		var volumeRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Items = {
				volumeLabel,
				volumeSlider
			}
		};

		var layout = new DynamicLayout { Spacing = new Size(6, 8) };
		layout.AddRow(titleLabel);
		layout.AddRow(pathLabel);
		layout.AddRow(transportRow);
		layout.AddRow(volumeRow);
		layout.AddRow(detailsLabel);
		Content = layout;
	}

	public void SetAudio(string fileName, string path, string details, ReadOnlyMemory<byte> data) {
		playbackError = null;
		playback.Load(fileName, data);
		titleLabel.Text = fileName;
		pathLabel.Text = path;
		detailsLabel.Text = details;
		volumeSlider.Value = (int)Math.Round(playback.Volume * 100);
		UpdateTimeLabels();
		UpdateSeekSlider();
		UpdateTransportButtons();
	}

	public void Unload() {
		positionTimer.Stop();
		playback.Unload();
		seekDragging = false;
		playbackError = null;
		titleLabel.Text = "";
		pathLabel.Text = "";
		detailsLabel.Text = "";
		currentTimeLabel.Text = "0:00";
		totalTimeLabel.Text = "0:00";
		seekSlider.Value = 0;
		UpdateTransportButtons();
	}

	public void TogglePlayPause() => TogglePlayPauseCore();

	private void TogglePlayPauseCore() {
		if (!playback.CanPlay)
			return;
		try {
			playback.TogglePlayPause();
			if (playback.State == AudioPlaybackState.Playing)
				positionTimer.Start();
			else
				positionTimer.Stop();
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
	}

	private void OnPlaybackStateChanged() {
		Application.Instance.AsyncInvoke(() => {
			UpdateTransportButtons();
			if (playback.State == AudioPlaybackState.Stopped)
				positionTimer.Stop();
		});
	}

	private void OnPlaybackEnded() {
		Application.Instance.AsyncInvoke(() => {
			positionTimer.Stop();
			UpdateSeekSlider();
			UpdateTimeLabels();
			UpdateTransportButtons();
		});
	}

	private void ShowPlaybackError(string message) {
		playbackError = message;
		positionTimer.Stop();
		playback.Stop();
		detailsLabel.Text = (detailsLabel.Text ?? "") + Environment.NewLine + "Playback failed: " + message;
		UpdateTransportButtons();
	}

	private static string FormatTime(TimeSpan time) {
		if (time.TotalHours >= 1)
			return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
		return $"{time.Minutes}:{time.Seconds:D2}";
	}
}
