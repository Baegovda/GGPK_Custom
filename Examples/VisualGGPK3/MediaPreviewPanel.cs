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
		BackgroundColor = Colors.Transparent,
		Visible = false
	};
	private readonly CheckBox autoHideCheck = new() {
		Text = "Auto hide",
		TextColor = Colors.White
	};
	private readonly TextArea infoText = new() {
		ReadOnly = true,
		BackgroundColor = Colors.Transparent,
		TextColor = Colors.White,
		Border = BorderType.None,
		Wrap = true
	};
	private readonly UITimer autoHideTimer;
	private bool audioMode;
	private bool videoMode;
	private bool spriteMode;
	private bool statusMode;
	private bool imageInfoMode;
	private bool overlayAutoHidden;
	private bool suppressAutoHideEvents;
	private Size lastOverlaySize;
	private Point lastOverlayPosition;
	private Bitmap? imageSourceBitmap;

	public MediaPreviewPanel() {
		var header = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 4,
			Items = {
				new StackLayoutItem(new Panel(), expand: true),
				autoHideCheck
			}
		};
		var overlayLayout = new DynamicLayout { Spacing = new Size(4, 4) };
		overlayLayout.AddRow(header);
		overlayLayout.AddRow(spritePlayerView);
		overlayLayout.AddRow(infoText);
		overlayBox.Content = overlayLayout;

		spritePlayerView.FrameChanged += OnSpriteFrameChanged;

		autoHideCheck.CheckedChanged += (_, _) => OnAutoHideCheckChanged();
		overlayBox.MouseEnter += (_, _) => ScheduleAutoHide();
		overlayBox.MouseMove += (_, _) => ScheduleAutoHide();
		overlayHoverZone.MouseEnter += (_, _) => RevealInfoOverlay();

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
		overlayAutoHidden = false;
		audioPlayerView.Visible = false;
		videoPlayerView.Visible = false;
		overlayHoverZone.Visible = false;
		autoHideCheck.Visible = false;
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
		BackgroundColor = Colors.Transparent;

		UnloadSprite();
		imageView.Image = null;
		imageSourceBitmap?.Dispose();
		imageSourceBitmap = bitmap;

		if (spritePlayerView.TryLoad(bitmap, fileName, path)) {
			spriteMode = true;
		} else {
			spriteMode = false;
			imageView.Image = bitmap;
		}

		infoText.Text = info;
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
		BackgroundColor = Colors.Transparent;
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
	}

	private void ReleaseImageAssets() {
		imageView.Image = null;
		UnloadSprite();
		imageSourceBitmap?.Dispose();
		imageSourceBitmap = null;
	}

	private void OnSpriteFrameChanged(Bitmap frame) {
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
			var playerHeight = Math.Clamp(audioPlayerView.MinimumSize.Height, 168, Height - 48);
			audioPlayerView.Size = new Size(playerWidth, playerHeight);
			pixelLayout.Move(audioPlayerView, (Width - playerWidth) / 2, (Height - playerHeight) / 2);
			return;
		}

		if (statusMode && overlayBox.Visible) {
			var statusWidth = Math.Min(Width - OverlayMargin * 2, 720);
			var statusHeight = Height - OverlayMargin * 2;
			overlayBox.Size = new Size(statusWidth, statusHeight);
			infoText.Height = Math.Max(24, statusHeight - overlayBox.Padding.Vertical - OverlayHeaderHeight);
			pixelLayout.Move(overlayBox, OverlayMargin, OverlayMargin);
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
		var spriteHeight = spritePlayerView.Visible ? SpriteControlsHeight : 0;
		infoText.Height = Math.Max(24, overlayHeight - spriteHeight - overlayBox.Padding.Vertical - OverlayHeaderHeight);
		overlayBox.Visible = true;
		overlayHoverZone.Visible = false;
		pixelLayout.Move(overlayBox, lastOverlayPosition.X, lastOverlayPosition.Y);
	}

	private void ComputeImageOverlaySize(out int overlayWidth, out int overlayHeight) {
		overlayWidth = Math.Min(Width - OverlayMargin * 2, 460);
		var spriteHeight = spritePlayerView.Visible ? SpriteControlsHeight : 0;
		var infoHeight = Math.Clamp(MeasureInfoHeight(overlayWidth), 48, Math.Max(48, Height / 3));
		overlayHeight = Math.Min(Height - OverlayMargin * 2, infoHeight + overlayBox.Padding.Vertical + OverlayHeaderHeight + spriteHeight);
	}

	private int MeasureInfoHeight(int width) {
		if (string.IsNullOrEmpty(infoText.Text))
			return 48;
		var lines = infoText.Text.Split('\n').Length;
		return Math.Max(48, lines * 18 + 12);
	}

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
