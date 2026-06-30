using System;
using System.IO;

using Eto.Drawing;
using Eto.Forms;

using LibVLCSharp.Eto;
using LibVLCSharp.Shared;

namespace VisualGGPK3;

internal sealed class VideoPlayerView : Panel {
	private const int SeekScale = 10000;

	private readonly VideoView videoView = new();
	private readonly ZoomableImageView binkFrameView = new();
	private readonly MediaPlayer? mediaPlayer;
	private readonly VideoPlayback? vlcPlayback;
	private readonly Bink2Playback binkPlayback = new();
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
	private readonly Button locateBinkButton = new() { Text = "Locate bink2w64.dll…", Visible = false };
	private readonly UITimer positionTimer;
	private readonly StackLayout volumeRow;
	private bool seekDragging;
	private bool binkMode;
	private string? playbackError;
	private string? pendingFileName;
	private string? pendingPath;
	private ReadOnlyMemory<byte> pendingData;

	public VideoPlayerView() {
		BackgroundColor = new Color(0.06f, 0.06f, 0.08f);
		Padding = new Padding(12);
		MinimumSize = new Size(480, 320);

		VideoPlayback.EnsureInitialized();
		mediaPlayer = new MediaPlayer(VideoPlayback.SharedLibVlc);
		vlcPlayback = new VideoPlayback(mediaPlayer);
		videoView.MediaPlayer = mediaPlayer;
		videoView.MinimumSize = new Size(320, 180);
		binkFrameView.MinimumSize = new Size(320, 180);
		binkFrameView.Visible = false;

		AppTheme.StyleButton(playPauseButton, ThemeButtonVariant.Primary);
		AppTheme.StyleButton(stopButton, ThemeButtonVariant.Ghost);
		AppTheme.StyleButton(locateBinkButton);
		AppTheme.StyleSlider(seekSlider);
		AppTheme.StyleSlider(volumeSlider);

		playPauseButton.Click += (_, _) => TogglePlayPauseCore();
		stopButton.Click += (_, _) => Stop();
		locateBinkButton.Click += (_, _) => OnLocateBinkDllClicked();
		seekSlider.ValueChanged += (_, _) => OnSeekSliderChanged();
		seekSlider.MouseDown += (_, _) => seekDragging = true;
		seekSlider.MouseUp += (_, _) => {
			seekDragging = false;
			ApplySeekFromSlider();
		};
		volumeSlider.ValueChanged += (_, _) => {
			if (!binkMode && vlcPlayback is not null)
				vlcPlayback.Volume = (int)volumeSlider.Value;
		};
		videoView.SizeChanged += (_, _) => ReattachVideoOutput();
		videoView.Load += (_, _) => ReattachVideoOutput();

		vlcPlayback!.StateChanged += OnPlaybackStateChanged;
		vlcPlayback.PositionChanged += OnPlaybackPositionChanged;
		vlcPlayback.PlaybackEnded += OnPlaybackEnded;
		binkPlayback.StateChanged += OnPlaybackStateChanged;
		binkPlayback.PositionChanged += OnPlaybackPositionChanged;
		binkPlayback.PlaybackEnded += OnPlaybackEnded;
		binkPlayback.FrameChanged += OnBinkFrameChanged;

		positionTimer = new UITimer((_, _) => OnPositionTimerTick()) { Interval = 0.1 };

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

		volumeRow = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Visible = true,
			Items = {
				volumeLabel,
				volumeSlider
			}
		};

		var controlsLayout = new DynamicLayout { Spacing = new Size(6, 6) };
		controlsLayout.AddRow(titleLabel);
		controlsLayout.AddRow(pathLabel);
		controlsLayout.AddRow(transportRow);
		controlsLayout.AddRow(locateBinkButton);
		controlsLayout.AddRow(volumeRow);
		controlsLayout.AddRow(detailsLabel);

		var surfaceLayout = new DynamicLayout { Spacing = new Size(0, 0) };
		surfaceLayout.Add(videoView, xscale: true, yscale: true);
		surfaceLayout.Add(binkFrameView, xscale: true, yscale: true);

		var layout = new DynamicLayout { Spacing = new Size(0, 8) };
		layout.Add(surfaceLayout, xscale: true, yscale: true);
		layout.AddSeparateRow(controlsLayout);
		Content = layout;
	}

	public void SetVideo(string fileName, string path, string details, ReadOnlyMemory<byte> data) {
		playbackError = null;
		pendingFileName = fileName;
		pendingPath = path;
		pendingData = data;
		UnloadCore();
		binkMode = fileName.EndsWith(".bk2", StringComparison.OrdinalIgnoreCase);
		if (binkMode) {
			videoView.Visible = false;
			binkFrameView.Visible = true;
			volumeRow.Visible = false;
			binkPlayback.Load(fileName, data);
			if (!string.IsNullOrEmpty(binkPlayback.GetLoadError()))
				playbackError = binkPlayback.GetLoadError();
			if (binkPlayback.CurrentFrame is not null)
				binkFrameView.Image = binkPlayback.CurrentFrame;
			positionTimer.Interval = Math.Max(1.0 / 60.0, binkPlayback.FrameIntervalSeconds);
		} else {
			videoView.Visible = true;
			binkFrameView.Visible = false;
			volumeRow.Visible = true;
			vlcPlayback!.Load(fileName, data);
			ReattachVideoOutput();
			volumeSlider.Value = vlcPlayback.Volume;
			positionTimer.Interval = 0.1;
		}
		titleLabel.Text = fileName;
		pathLabel.Text = path;
		detailsLabel.Text = details;
		if (playbackError is not null)
			detailsLabel.Text = details + Environment.NewLine + playbackError;
		UpdateTimeLabels();
		UpdateSeekSlider();
		UpdateTransportButtons();
		UpdateLocateBinkButton();
	}

	public void Unload() {
		positionTimer.Stop();
		UnloadCore();
		seekDragging = false;
		playbackError = null;
		binkMode = false;
		titleLabel.Text = "";
		pathLabel.Text = "";
		detailsLabel.Text = "";
		currentTimeLabel.Text = "0:00";
		totalTimeLabel.Text = "0:00";
		seekSlider.Value = 0;
		videoView.Visible = true;
		binkFrameView.Visible = false;
		volumeRow.Visible = true;
		locateBinkButton.Visible = false;
		UpdateTransportButtons();
	}

	public void TogglePlayPause() => TogglePlayPauseCore();

	private void UnloadCore() {
		videoView.MediaPlayer = null;
		vlcPlayback?.Unload();
		binkPlayback.Unload();
		binkFrameView.Image = null;
	}

	private void ReattachVideoOutput() {
		if (mediaPlayer is not null)
			videoView.MediaPlayer = mediaPlayer;
	}

	private void TogglePlayPauseCore() {
		if (binkMode) {
			if (!binkPlayback.CanPlay)
				return;
			try {
				binkPlayback.TogglePlayPause();
				if (binkPlayback.State == VideoPlaybackState.Playing)
					positionTimer.Start();
				else
					positionTimer.Stop();
			} catch (Exception ex) {
				ShowPlaybackError(ex.Message);
			}
			return;
		}
		if (vlcPlayback is null || !vlcPlayback.CanPlay)
			return;
		try {
			vlcPlayback.TogglePlayPause();
			if (vlcPlayback.State == VideoPlaybackState.Playing)
				positionTimer.Start();
			else
				positionTimer.Stop();
		} catch (Exception ex) {
			ShowPlaybackError(ex.Message);
		}
	}

	private void Stop() {
		positionTimer.Stop();
		if (binkMode)
			binkPlayback.Stop();
		else
			vlcPlayback?.Stop();
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
		if (Duration <= TimeSpan.Zero)
			return;
		try {
			if (binkMode)
				binkPlayback.Position = PositionFromSlider();
			else if (vlcPlayback is not null)
				vlcPlayback.Position = PositionFromSlider();
			UpdateTimeLabels();
		} catch (Exception ex) {
			ShowPlaybackError(ex.Message);
		}
	}

	private TimeSpan Duration => binkMode ? binkPlayback.Duration : vlcPlayback?.Duration ?? TimeSpan.Zero;

	private TimeSpan CurrentPosition => binkMode ? binkPlayback.Position : vlcPlayback?.Position ?? TimeSpan.Zero;

	private VideoPlaybackState CurrentState => binkMode ? binkPlayback.State : vlcPlayback?.State ?? VideoPlaybackState.Stopped;

	private bool CanPlay => binkMode ? binkPlayback.CanPlay : vlcPlayback?.CanPlay == true;

	private TimeSpan PositionFromSlider() {
		if (Duration <= TimeSpan.Zero)
			return TimeSpan.Zero;
		var ratio = seekSlider.Value / (double)SeekScale;
		return TimeSpan.FromTicks((long)(Duration.Ticks * ratio));
	}

	private void OnPositionTimerTick() {
		if (binkMode && binkPlayback.State == VideoPlaybackState.Playing)
			binkPlayback.TickPlayback();
		if (seekDragging)
			return;
		UpdateSeekSlider();
		UpdateTimeLabels();
	}

	private void UpdateSeekSlider() {
		if (seekDragging || Duration <= TimeSpan.Zero) {
			if (Duration <= TimeSpan.Zero)
				seekSlider.Value = 0;
			return;
		}
		var ratio = CurrentPosition.TotalMilliseconds / Duration.TotalMilliseconds;
		seekSlider.Value = (int)Math.Round(Math.Clamp(ratio, 0, 1) * SeekScale);
	}

	private void UpdateTimeLabels() {
		currentTimeLabel.Text = FormatTime(CurrentPosition);
		totalTimeLabel.Text = FormatTime(Duration);
	}

	private void UpdateTransportButtons() {
		var canPlay = CanPlay && playbackError is null;
		playPauseButton.Enabled = canPlay;
		stopButton.Enabled = canPlay && CurrentState != VideoPlaybackState.Stopped;
		seekSlider.Enabled = canPlay && Duration > TimeSpan.Zero;
		playPauseButton.Text = CurrentState == VideoPlaybackState.Playing ? "Pause" : "Play";
	}

	private void OnPlaybackStateChanged() {
		Application.Instance.AsyncInvoke(() => {
			UpdateTransportButtons();
			if (CurrentState == VideoPlaybackState.Stopped)
				positionTimer.Stop();
		});
	}

	private void OnPlaybackPositionChanged() {
		Application.Instance.AsyncInvoke(UpdatePositionUi);
	}

	private void OnPlaybackEnded() {
		Application.Instance.AsyncInvoke(() => {
			positionTimer.Stop();
			UpdateSeekSlider();
			UpdateTimeLabels();
			UpdateTransportButtons();
		});
	}

	private void OnBinkFrameChanged() {
		Application.Instance.AsyncInvoke(() => {
			if (binkPlayback.CurrentFrame is not null) {
				binkFrameView.Image = binkPlayback.CurrentFrame;
				binkFrameView.InvalidateImage();
			}
		});
	}

	private void UpdatePositionUi() {
		if (seekDragging)
			return;
		UpdateSeekSlider();
		UpdateTimeLabels();
	}

	private void UpdateLocateBinkButton() =>
		locateBinkButton.Visible = binkMode && !binkPlayback.CanPlay;

	private void OnLocateBinkDllClicked() {
		using var ofd = new OpenFileDialog {
			FileName = "bink2w64.dll",
			Filters = {
				new FileFilter("Bink 2 decoder", "bink2w64.dll", ".dll"),
				new FileFilter("All files", "*")
			}
		};
		var parent = ParentWindow ?? Application.Instance.MainForm;
		if (ofd.ShowDialog(parent) != DialogResult.Ok)
			return;
		try {
			Bink2Locator.SetCustomPath(ofd.FileName);
			if (pendingFileName is not null)
				SetVideo(pendingFileName, pendingPath ?? "", Bink2Playback.Describe(pendingFileName, pendingData), pendingData);
		} catch (Exception ex) {
			MessageBox.Show(parent, ex.Message, "Bink 2 decoder", MessageBoxType.Error);
		}
	}

	private void ShowPlaybackError(string message) {
		playbackError = message;
		positionTimer.Stop();
		if (binkMode)
			binkPlayback.Stop();
		else
			vlcPlayback?.Stop();
		detailsLabel.Text = (detailsLabel.Text ?? "") + Environment.NewLine + "Playback failed: " + message;
		UpdateTransportButtons();
	}

	private static string FormatTime(TimeSpan time) {
		if (time.TotalHours >= 1)
			return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
		return $"{time.Minutes}:{time.Seconds:D2}";
	}
}
