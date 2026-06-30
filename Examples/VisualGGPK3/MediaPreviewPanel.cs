using System;
using System.Text;

using Eto.Drawing;
using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal sealed class MediaPreviewPanel : Panel {
	private const int OverlayMargin = 8;
	private const double AutoHideSeconds = 5.0;
	private const int OverlayHeaderHeight = 28;
	private const int SpriteControlsHeight = 36;
	private const int OverlayMinWidth = 280;
	private const int OverlayMaxWidth = 560;
	private const int StatusOverlayMaxWidth = 380;
	private const int OverlayInfoMinHeight = 36;
	private const int OverlayInfoMaxHeight = 120;
	private const int StatusOverlayMaxHeight = 260;
	private const int OverlayLineHeight = 16;
	private const int PathBlockHeight = 34;

	private readonly PixelLayout pixelLayout = new();
	private readonly ZoomableImageView imageView = new();
	private readonly AudioPlayerView audioPlayerView = new();
	private readonly VideoPlayerView videoPlayerView = new();
	private readonly SpriteSheetPlayerView spritePlayerView = new();
	private readonly Panel overlayBox = new() {
		BackgroundColor = new Color(0, 0, 0, 0.72f),
		Padding = new Padding(10),
		Visible = false
	};
	private readonly Panel overlayHoverZone = new() {
		// Nearly invisible so WPF still hit-tests the hover zone after auto-hide.
		BackgroundColor = new Color(0, 0, 0, 0.004f),
		Visible = false
	};
	private readonly CheckBox autoHideCheck = new() {
		Text = "Auto hide",
		TextColor = Colors.White
	};
	private readonly Button viewOriginalButton = new() {
		Text = "View original",
		Width = 104,
		Visible = false,
		ToolTip = "Show the full sprite sheet atlas"
	};
	private readonly TextArea infoText = new() {
		ReadOnly = true,
		BackgroundColor = Colors.Transparent,
		TextColor = Colors.White,
		Border = BorderType.None,
		Wrap = true
	};
	private readonly Label pathCaption = new() {
		Text = "Path",
		TextColor = new Color(0.72f, 0.72f, 0.76f)
	};
	private readonly Label pathValue = new() {
		TextColor = Colors.White,
		Wrap = WrapMode.None,
		VerticalAlignment = VerticalAlignment.Top
	};
	private readonly Label detailsLabel = new() {
		TextColor = Colors.White,
		Wrap = WrapMode.Word,
		VerticalAlignment = VerticalAlignment.Top
	};
	private readonly Panel imageInfoPanel;
	private readonly UITimer autoHideTimer;
	private bool audioMode;
	private bool videoMode;
	private bool spriteMode;
	private bool statusMode;
	private bool imageInfoMode;
	private bool overlayAutoHidden;
	private bool viewingOriginalAtlas;
	private bool suppressAutoHideEvents;
	private Size lastOverlaySize;
	private Point lastOverlayPosition;
	private Bitmap? imageSourceBitmap;
	private readonly Panel overlayHeader;

	public MediaPreviewPanel() {
		viewOriginalButton.Click += (_, _) => ToggleOriginalView();
		AppTheme.StyleButton(viewOriginalButton, ThemeButtonVariant.Ghost);
		viewOriginalButton.TextColor = Colors.White;

		var header = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 8,
			Items = {
				viewOriginalButton,
				new StackLayoutItem(new Panel(), expand: true),
				autoHideCheck
			}
		};
		var overlayHeader = new Panel { Content = header, Height = OverlayHeaderHeight };
		imageInfoPanel = new StackLayout {
			Orientation = Orientation.Vertical,
			Spacing = 2,
			Items = {
				pathCaption,
				new StackLayoutItem(pathValue, expand: true),
				new StackLayoutItem(detailsLabel, expand: true)
			}
		};
		var overlayLayout = new StackLayout {
			Orientation = Orientation.Vertical,
			Spacing = 6,
			Items = {
				overlayHeader,
				spritePlayerView,
				imageInfoPanel,
				infoText
			}
		};
		overlayBox.Content = overlayLayout;
		this.overlayHeader = overlayHeader;

		spritePlayerView.FrameChanged += OnSpriteFrameChanged;

		autoHideCheck.CheckedChanged += (_, _) => OnAutoHideCheckChanged();
		overlayBox.MouseEnter += (_, _) => ScheduleAutoHide();
		overlayBox.MouseMove += (_, _) => ScheduleAutoHide();
		overlayHoverZone.MouseEnter += (_, _) => TryRevealInfoOverlayFromHover();
		overlayHoverZone.MouseMove += (_, _) => TryRevealInfoOverlayFromHover();
		MouseMove += OnOverlayHoverProbe;
		imageView.MouseMove += OnOverlayHoverProbe;

		autoHideTimer = new UITimer((_, _) => HideInfoOverlay()) { Interval = AutoHideSeconds };

		var settings = LayoutSettingsStore.Load();
		suppressAutoHideEvents = true;
		autoHideCheck.Checked = settings.InfoAutoHide;
		suppressAutoHideEvents = false;

		pixelLayout.Add(imageView, 0, 0);
		pixelLayout.Add(audioPlayerView, 0, 0);
		pixelLayout.Add(videoPlayerView, 0, 0);
		pixelLayout.Add(overlayHoverZone, OverlayMargin, OverlayMargin);
		pixelLayout.Add(overlayBox, OverlayMargin, OverlayMargin);
		Content = pixelLayout;

		audioPlayerView.Visible = false;
		videoPlayerView.Visible = false;
		SizeChanged += (_, _) => Relayout();
		audioPlayerView.SizeChanged += (_, _) => Relayout();
		videoPlayerView.SizeChanged += (_, _) => Relayout();
	}

	public ZoomableImageView ImageView => imageView;

	public bool InfoAutoHideEnabled => autoHideCheck.Checked == true;

	private bool AutoHideEnabled => autoHideCheck.Checked == true;

	public void ShowStatus(string text) {
		CancelAutoHide();
		ReleaseImageAssets();
		UnloadAudio();
		UnloadVideo();
		statusMode = true;
		imageInfoMode = false;
		audioMode = false;
		videoMode = false;
		spriteMode = false;
		viewingOriginalAtlas = false;
		viewOriginalButton.Visible = false;
		overlayHeader.Visible = false;
		overlayAutoHidden = false;
		audioPlayerView.Visible = false;
		videoPlayerView.Visible = false;
		overlayHoverZone.Visible = false;
		autoHideCheck.Visible = false;
		imageInfoPanel.Visible = false;
		infoText.Visible = true;
		BackgroundColor = Colors.Transparent;
		infoText.Text = text;
		overlayBox.Visible = true;
		Relayout();
	}

	public void ShowImage(Bitmap bitmap, string fileName, string? path, string info) {
		CancelAutoHide();
		statusMode = false;
		imageInfoMode = true;
		UnloadAudio();
		UnloadVideo();
		audioMode = false;
		videoMode = false;
		overlayAutoHidden = false;
		audioPlayerView.Visible = false;
		videoPlayerView.Visible = false;
		overlayHoverZone.Visible = false;
		autoHideCheck.Visible = true;
		imageInfoPanel.Visible = true;
		infoText.Visible = false;
		BackgroundColor = Colors.Transparent;

		UnloadSprite();
		imageView.Image = null;
		imageSourceBitmap?.Dispose();
		imageSourceBitmap = bitmap;

		if (spritePlayerView.TryLoad(bitmap, fileName, path)) {
			spriteMode = true;
			viewingOriginalAtlas = false;
			viewOriginalButton.Text = "View original";
			viewOriginalButton.Visible = true;
		} else {
			spriteMode = false;
			viewingOriginalAtlas = false;
			viewOriginalButton.Visible = false;
			imageView.Image = bitmap;
		}

		overlayHeader.Visible = true;
		pathValue.Text = path ?? "";
		pathValue.ToolTip = path;
		detailsLabel.Text = info;
		overlayBox.Visible = true;
		Relayout();
		ScheduleAutoHide();
	}

	public void ShowAudio(FileTreeItem fileItem, ReadOnlyMemory<byte> data) {
		CancelAutoHide();
		statusMode = false;
		imageInfoMode = false;
		overlayAutoHidden = false;
		ReleaseImageAssets();
		UnloadAudio();
		UnloadVideo();
		audioMode = true;
		videoMode = false;
		overlayBox.Visible = false;
		overlayHoverZone.Visible = false;
		BackgroundColor = new Color(0.06f, 0.06f, 0.08f);
		audioPlayerView.SetAudio(
			fileItem.Name,
			fileItem.GetPath(),
			BuildAudioDetails(fileItem, data),
			data);
		audioPlayerView.Visible = true;
		Relayout();
	}

	public void ShowVideo(FileTreeItem fileItem, ReadOnlyMemory<byte> data) {
		CancelAutoHide();
		statusMode = false;
		imageInfoMode = false;
		overlayAutoHidden = false;
		ReleaseImageAssets();
		UnloadAudio();
		UnloadVideo();
		audioMode = false;
		videoMode = true;
		overlayBox.Visible = false;
		overlayHoverZone.Visible = false;
		BackgroundColor = new Color(0.06f, 0.06f, 0.08f);
		videoPlayerView.SetVideo(
			fileItem.Name,
			fileItem.GetPath(),
			BuildVideoDetails(fileItem, data),
			data);
		videoPlayerView.Visible = true;
		Relayout();
	}

	public void Clear() {
		CancelAutoHide();
		statusMode = false;
		imageInfoMode = false;
		overlayAutoHidden = false;
		ReleaseImageAssets();
		UnloadAudio();
		UnloadVideo();
		audioMode = false;
		videoMode = false;
		audioPlayerView.Visible = false;
		videoPlayerView.Visible = false;
		overlayBox.Visible = false;
		overlayHoverZone.Visible = false;
		imageInfoPanel.Visible = false;
		infoText.Visible = false;
		BackgroundColor = Colors.Transparent;
		pathValue.Text = "";
		detailsLabel.Text = "";
		infoText.Text = "";
	}

	public bool IsAudioMode => audioMode;

	public bool ToggleAudioPlayback() {
		if (!audioMode)
			return false;
		audioPlayerView.TogglePlayPause();
		return true;
	}

	public bool IsSpriteMode => spriteMode;

	public bool ToggleSpritePlayback() {
		if (!spriteMode)
			return false;
		spritePlayerView.TogglePlayPause();
		return true;
	}

	public bool IsVideoMode => videoMode;

	public bool ToggleVideoPlayback() {
		if (!videoMode)
			return false;
		videoPlayerView.TogglePlayPause();
		return true;
	}

	public void UnloadAudio() => audioPlayerView.Unload();

	public void UnloadVideo() => videoPlayerView.Unload();

	private void UnloadSprite() {
		spritePlayerView.Unload();
		spriteMode = false;
		viewingOriginalAtlas = false;
		viewOriginalButton.Visible = false;
	}

	private void ToggleOriginalView() {
		if (!spriteMode || imageSourceBitmap is null)
			return;
		viewingOriginalAtlas = !viewingOriginalAtlas;
		viewOriginalButton.Text = viewingOriginalAtlas ? "View sequence" : "View original";
		if (viewingOriginalAtlas) {
			spritePlayerView.Pause();
			imageView.Image = imageSourceBitmap;
		} else if (spritePlayerView.CurrentFrame is { } frame) {
			imageView.Image = frame;
		}
		imageView.InvalidateImage();
		RevealInfoOverlay();
		ScheduleAutoHide();
	}

	private void ReleaseImageAssets() {
		imageView.Image = null;
		UnloadSprite();
		imageSourceBitmap?.Dispose();
		imageSourceBitmap = null;
	}

	private void OnSpriteFrameChanged(Bitmap frame) {
		if (viewingOriginalAtlas)
			return;
		imageView.Image = frame;
		imageView.InvalidateImage();
	}

	private void OnAutoHideCheckChanged() {
		if (suppressAutoHideEvents)
			return;
		PersistInfoAutoHideSetting();
		if (!AutoHideEnabled) {
			CancelAutoHide();
			RevealInfoOverlay();
			return;
		}
		if (imageInfoMode)
			ScheduleAutoHide();
	}

	private void ScheduleAutoHide() {
		if (!imageInfoMode || statusMode || audioMode || videoMode || !AutoHideEnabled || overlayAutoHidden)
			return;
		autoHideTimer.Stop();
		autoHideTimer.Interval = AutoHideSeconds;
		autoHideTimer.Start();
	}

	private void CancelAutoHide() => autoHideTimer.Stop();

	private void HideInfoOverlay() {
		autoHideTimer.Stop();
		if (!imageInfoMode || !AutoHideEnabled)
			return;
		overlayAutoHidden = true;
		overlayBox.Visible = false;
		overlayHoverZone.Visible = true;
		Relayout();
		ProbeMouseForOverlayReveal();
	}

	private void RevealInfoOverlay() {
		if (!imageInfoMode)
			return;
		overlayAutoHidden = false;
		overlayBox.Visible = true;
		overlayHoverZone.Visible = false;
		Relayout();
		ScheduleAutoHide();
	}

	private void TryRevealInfoOverlayFromHover() {
		if (!overlayAutoHidden || !imageInfoMode || !AutoHideEnabled)
			return;
		RevealInfoOverlay();
	}

	private void OnOverlayHoverProbe(object? sender, MouseEventArgs e) {
		if (!overlayAutoHidden || !imageInfoMode || !AutoHideEnabled)
			return;
		var point = Point.Round(e.Location);
		if (sender is Control { Parent: not null } source && !ReferenceEquals(source, this)) {
			var screen = source.PointToScreen(point);
			point = Point.Round(PointFromScreen(screen));
		}
		if (IsPointInOverlayHoverZone(point))
			RevealInfoOverlay();
	}

	private void ProbeMouseForOverlayReveal() {
		if (!overlayAutoHidden || !imageInfoMode || !AutoHideEnabled)
			return;
		var local = Point.Round(PointFromScreen(Mouse.Position));
		if (IsPointInOverlayHoverZone(local))
			RevealInfoOverlay();
	}

	private bool IsPointInOverlayHoverZone(Point point) {
		if (lastOverlaySize.Width <= 0 || lastOverlaySize.Height <= 0)
			return false;
		var zone = new RectangleF(lastOverlayPosition, lastOverlaySize);
		return zone.Contains(point);
	}

	private void Relayout() {
		if (Width <= 0 || Height <= 0)
			return;

		imageView.Size = new Size(Width, Height);
		pixelLayout.Move(imageView, 0, 0);

		if (videoMode && videoPlayerView.Visible) {
			videoPlayerView.Size = new Size(Width, Height);
			pixelLayout.Move(videoPlayerView, 0, 0);
			return;
		}

		if (audioMode && audioPlayerView.Visible) {
			var playerWidth = Math.Clamp(Width - 48, 420, 640);
			var playerHeight = Math.Clamp(audioPlayerView.MinimumSize.Height, 220, Height - 48);
			audioPlayerView.Size = new Size(playerWidth, playerHeight);
			pixelLayout.Move(audioPlayerView, (Width - playerWidth) / 2, (Height - playerHeight) / 2);
			return;
		}

		if (statusMode && overlayBox.Visible) {
			spritePlayerView.Visible = false;
			imageInfoPanel.Visible = false;
			infoText.Visible = true;
			var statusWidth = Math.Min(Width - OverlayMargin * 2, StatusOverlayMaxWidth);
			var statusInfoHeight = Math.Clamp(MeasureWrappedTextHeight(infoText.Text, statusWidth), OverlayInfoMinHeight, StatusOverlayMaxHeight);
			var statusOverlayHeight = statusInfoHeight + overlayBox.Padding.Vertical;
			lastOverlaySize = new Size(statusWidth, statusOverlayHeight);
			lastOverlayPosition = new Point(OverlayMargin, Height - statusOverlayHeight - OverlayMargin);
			overlayBox.Size = lastOverlaySize;
			infoText.Size = new Size(statusWidth - overlayBox.Padding.Horizontal, statusInfoHeight);
			overlayHoverZone.Visible = false;
			pixelLayout.Move(overlayBox, lastOverlayPosition.X, lastOverlayPosition.Y);
			return;
		}

		if (!imageInfoMode)
			return;

		ComputeImageOverlaySize(out var overlayWidth, out var overlayHeight);
		lastOverlaySize = new Size(overlayWidth, overlayHeight);
		lastOverlayPosition = new Point(OverlayMargin, Height - overlayHeight - OverlayMargin);

		if (overlayAutoHidden && AutoHideEnabled) {
			overlayBox.Visible = false;
			overlayHoverZone.Size = lastOverlaySize;
			overlayHoverZone.Visible = true;
			pixelLayout.Move(overlayHoverZone, lastOverlayPosition.X, lastOverlayPosition.Y);
			return;
		}

		overlayBox.Size = lastOverlaySize;
		var spriteHeight = spriteMode ? SpriteControlsHeight : 0;
		imageInfoPanel.Visible = true;
		infoText.Visible = false;
		var contentWidth = Math.Max(120, overlayWidth - overlayBox.Padding.Horizontal);
		var detailsHeight = Math.Clamp(
			MeasureWrappedTextHeight(detailsLabel.Text, contentWidth),
			OverlayLineHeight,
			OverlayInfoMaxHeight - PathBlockHeight);
		var infoHeight = PathBlockHeight + detailsHeight + 4;
		imageInfoPanel.Size = new Size(contentWidth, infoHeight);
		pathValue.Size = new Size(contentWidth, OverlayLineHeight + 2);
		detailsLabel.Size = new Size(contentWidth, detailsHeight);
		spritePlayerView.Height = spriteHeight;
		spritePlayerView.Visible = spriteMode;
		overlayBox.Visible = true;
		overlayHoverZone.Visible = false;
		pixelLayout.Move(overlayBox, lastOverlayPosition.X, lastOverlayPosition.Y);
	}

	private void ComputeImageOverlaySize(out int overlayWidth, out int overlayHeight) {
		overlayWidth = ComputeOverlayContentWidth();
		var spriteHeight = spriteMode ? SpriteControlsHeight : 0;
		var contentWidth = Math.Max(120, overlayWidth - overlayBox.Padding.Horizontal);
		var detailsHeight = Math.Clamp(
			MeasureWrappedTextHeight(detailsLabel.Text, contentWidth),
			OverlayLineHeight,
			OverlayInfoMaxHeight - PathBlockHeight);
		var infoHeight = Math.Max(OverlayInfoMinHeight, PathBlockHeight + detailsHeight + 4);
		overlayHeight = infoHeight + overlayBox.Padding.Vertical + OverlayHeaderHeight + spriteHeight;
	}

	private int ComputeOverlayContentWidth() {
		var available = Width - OverlayMargin * 2;
		var preferred = (int)(Width * 0.72);
		return Math.Clamp(Math.Min(preferred, available), OverlayMinWidth, Math.Min(available, OverlayMaxWidth));
	}

	private int MeasureWrappedTextHeight(string text, int width) {
		if (string.IsNullOrEmpty(text))
			return OverlayLineHeight;

		var textWidth = Math.Max(120, width - overlayBox.Padding.Horizontal);
		var charsPerLine = Math.Max(24, textWidth / 7);
		var totalLines = 0;
		foreach (var line in text.Split('\n')) {
			var length = string.IsNullOrEmpty(line) ? 1 : line.Length;
			totalLines += (length + charsPerLine - 1) / charsPerLine;
		}
		return Math.Max(OverlayLineHeight, totalLines * OverlayLineHeight + 4);
	}

	private int MeasureInfoHeight(int width) => MeasureWrappedTextHeight(infoText.Text, width);

	private static string BuildVideoDetails(FileTreeItem fileItem, ReadOnlyMemory<byte> data) {
		var sb = new StringBuilder();
		sb.AppendLine($"Size: {FormatByteSize(data.Length)}");
		if (fileItem.Name.EndsWith(".bk2", StringComparison.OrdinalIgnoreCase))
			sb.Append(Bink2Playback.Describe(fileItem.Name, data));
		else
			sb.Append(VideoPlayback.Describe(fileItem.Name, data));
		return sb.ToString().TrimEnd();
	}

	private static string BuildAudioDetails(FileTreeItem fileItem, ReadOnlyMemory<byte> data) {
		var sb = new StringBuilder();
		sb.AppendLine($"Size: {FormatByteSize(data.Length)}");
		sb.Append(AudioPlayback.Describe(fileItem.Name, data));
		return sb.ToString().TrimEnd();
	}

	private static string FormatByteSize(long bytes) => bytes switch {
		< 1024 => $"{bytes} bytes",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB ({bytes:N0} bytes)",
		_ => $"{bytes / (1024.0 * 1024):F2} MB ({bytes:N0} bytes)"
	};

	private void PersistInfoAutoHideSetting() {
		var layout = LayoutSettingsStore.Load();
		LayoutSettingsStore.Save(new LayoutSettingsStore.Layout(
			layout.MainSplitter,
			layout.InnerSplitter,
			AutoHideEnabled,
			layout.FilterType,
			layout.FilterExclude));
	}
}
