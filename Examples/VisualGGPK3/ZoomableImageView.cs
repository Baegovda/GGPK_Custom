using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

public sealed class ZoomableImageView : Panel {
	private readonly PixelLayout layout = new();
	private readonly Scrollable scrollable = new();
	private readonly Drawable canvas = new();
	private readonly Label zoomLabel = new() {
		Text = "100%",
		TextColor = Colors.White
	};
	private readonly Panel zoomBadge = new() {
		BackgroundColor = new Color(0, 0, 0, 0.7f),
		Padding = new Padding(6, 3),
		Visible = false
	};
	private Bitmap? sourceBitmap;
	private double zoom = 1.0;
	private bool panning;
	private Point panStartMouse;
	private Point panStartScroll;

	private const double ZoomStep = 1.15;
	private const double MinZoom = 0.05;
	private const double MaxZoom = 32;

	public ZoomableImageView() {
		scrollable.Border = BorderType.None;
		scrollable.Content = canvas;
		canvas.Paint += OnPaint;
		zoomBadge.Content = zoomLabel;

		layout.Add(scrollable, 0, 0);
		layout.Add(zoomBadge, 6, 6);
		Content = layout;

		SizeChanged += (_, _) => Relayout();
#if Windows
		Load += (_, _) => WindowsHookScrollableInput();
#else
		canvas.MouseDown += OnPanMouseDown;
		canvas.MouseMove += OnPanMouseMove;
		canvas.MouseUp += (_, _) => EndPan();
		canvas.MouseEnter += (_, _) => UpdatePanCursor();
		scrollable.MouseMove += OnPanMouseMove;
		MouseUp += (_, _) => EndPan();
		canvas.MouseWheel += OnMouseWheel;
		scrollable.MouseWheel += OnMouseWheel;
		MouseWheel += OnMouseWheel;
#endif
	}

	public Image? Image {
		get => sourceBitmap;
		set {
			sourceBitmap = value as Bitmap;
			zoom = 1.0;
			scrollable.ScrollPosition = Point.Empty;
			zoomBadge.Visible = sourceBitmap is not null;
			UpdateZoomLabel();
			ApplyZoom();
			UpdatePanCursor();
		}
	}

	public void InvalidateImage() => canvas.Invalidate();

	private void OnPanMouseDown(object? sender, MouseEventArgs e) {
		if (e.Buttons == MouseButtons.Middle) {
			ResetZoom();
			return;
		}
		if (e.Buttons != MouseButtons.Primary || !CanPan())
			return;
		panning = true;
		panStartMouse = ToScrollablePoint(sender as Control, e.Location);
		panStartScroll = scrollable.ScrollPosition;
		canvas.Cursor = Cursors.SizeAll;
	}

	private void OnPanMouseMove(object? sender, MouseEventArgs e) {
		if (!panning)
			return;
		var current = ToScrollablePoint(sender as Control, e.Location);
		var delta = current - panStartMouse;
		SetScrollPosition(new Point(panStartScroll.X - delta.X, panStartScroll.Y - delta.Y));
	}

	private void EndPan() {
		if (!panning)
			return;
		panning = false;
#if Windows
		ReleaseWpfMouseCapture();
#endif
		UpdatePanCursor();
	}

	private bool CanPan() {
		if (sourceBitmap is null)
			return false;
		var client = scrollable.ClientSize;
		var content = canvas.Size;
		return content.Width > client.Width || content.Height > client.Height;
	}

	private void SetScrollPosition(Point pos) {
		var client = scrollable.ClientSize;
		var content = canvas.Size;
		var maxX = Math.Max(0, content.Width - client.Width);
		var maxY = Math.Max(0, content.Height - client.Height);
		scrollable.ScrollPosition = new Point(
			Math.Clamp(pos.X, 0, maxX),
			Math.Clamp(pos.Y, 0, maxY));
	}

	private Point ToScrollablePoint(Control? sender, PointF location) {
		if (sender is null || ReferenceEquals(sender, scrollable))
			return Point.Round(location);
		var screen = sender.PointToScreen(location);
		return Point.Round(scrollable.PointFromScreen(screen));
	}

	private void UpdatePanCursor() {
		if (panning)
			return;
		canvas.Cursor = CanPan() ? Cursors.Move : Cursors.Default;
	}

	private void OnPaint(object? sender, PaintEventArgs e) {
		if (sourceBitmap is null)
			return;
		var (_, offset, imageSize) = GetCanvasLayout();
		e.Graphics.DrawImage(sourceBitmap, offset.X, offset.Y, imageSize.Width, imageSize.Height);
	}

	private void OnMouseWheel(object? sender, MouseEventArgs e) {
		if (sourceBitmap is null || e.Delta.Height == 0)
			return;
		e.Handled = true;
		ZoomAt(GetViewportCursor(sender as Control, e), e.Delta.Height > 0);
	}

	private void ZoomAt(PointF viewport, bool zoomIn) {
		var oldZoom = zoom;
		var factor = zoomIn ? ZoomStep : 1 / ZoomStep;
		zoom = Math.Clamp(zoom * factor, MinZoom, MaxZoom);
		if (Math.Abs(zoom - oldZoom) < 1e-9)
			return;

		var (_, oldOffset, oldImageSize) = GetCanvasLayout(oldZoom);
		var scrollPos = scrollable.ScrollPosition;
		var imageX = (scrollPos.X + viewport.X - oldOffset.X) / oldZoom;
		var imageY = (scrollPos.Y + viewport.Y - oldOffset.Y) / oldZoom;

		ApplyZoom();

		var (_, newOffset, _) = GetCanvasLayout();
		SetScrollPosition(new Point(
			(int)Math.Round(newOffset.X + imageX * zoom - viewport.X),
			(int)Math.Round(newOffset.Y + imageY * zoom - viewport.Y)));
	}

	private PointF GetViewportCursor(Control? sender, MouseEventArgs e) {
		if (sender is Scrollable)
			return e.Location;
		if (sender is null)
			return e.Location;
		var screen = sender.PointToScreen(e.Location);
		return scrollable.PointFromScreen(screen);
	}

	private void ApplyZoom() {
		if (sourceBitmap is null)
			return;

		var (canvasSize, _, _) = GetCanvasLayout();
		canvas.Size = canvasSize;
		canvas.MinimumSize = canvasSize;
		canvas.Invalidate();
		scrollable.UpdateScrollSizes();
		if (!CanPan())
			scrollable.ScrollPosition = Point.Empty;
		else
			SetScrollPosition(scrollable.ScrollPosition);
		UpdateZoomLabel();
		UpdatePanCursor();
	}

	private Size GetScaledImageSize(double forZoom) {
		if (sourceBitmap is null)
			return Size.Empty;
		return new Size(
			Math.Max(1, (int)Math.Round(sourceBitmap.Width * forZoom)),
			Math.Max(1, (int)Math.Round(sourceBitmap.Height * forZoom)));
	}

	private (Size canvasSize, Point imageOffset, Size imageSize) GetCanvasLayout() => GetCanvasLayout(zoom);

	private (Size canvasSize, Point imageOffset, Size imageSize) GetCanvasLayout(double forZoom) {
		var imageSize = GetScaledImageSize(forZoom);
		if (sourceBitmap is null)
			return (Size.Empty, Point.Empty, Size.Empty);
		var client = scrollable.ClientSize;
		var clientWidth = client.Width > 0 ? client.Width : imageSize.Width;
		var clientHeight = client.Height > 0 ? client.Height : imageSize.Height;
		var canvasWidth = Math.Max(imageSize.Width, clientWidth);
		var canvasHeight = Math.Max(imageSize.Height, clientHeight);
		return (
			new Size(canvasWidth, canvasHeight),
			new Point((canvasWidth - imageSize.Width) / 2, (canvasHeight - imageSize.Height) / 2),
			imageSize);
	}

	private void ResetZoom() {
		if (sourceBitmap is null)
			return;
		zoom = 1.0;
		scrollable.ScrollPosition = Point.Empty;
		ApplyZoom();
	}

	private void UpdateZoomLabel() => zoomLabel.Text = $"{Math.Round(zoom * 100)}%";

	private void Relayout() {
		if (Width <= 0 || Height <= 0)
			return;
		scrollable.Size = new Size(Width, Height);
		layout.Move(scrollable, 0, 0);
		layout.Move(zoomBadge, 6, 6);
		if (sourceBitmap is not null)
			ApplyZoom();
	}

#if Windows
	private System.Windows.UIElement? wpfScrollHost;

	private void WindowsHookScrollableInput() {
		wpfScrollHost = ((Eto.Wpf.Forms.Controls.ScrollableHandler)scrollable.Handler).Control;
		wpfScrollHost.PreviewMouseWheel += (_, e) => {
			if (sourceBitmap is null)
				return;
			e.Handled = true;
			var p = e.GetPosition(wpfScrollHost);
			ZoomAt(new PointF((float)p.X, (float)p.Y), e.Delta > 0);
		};
		wpfScrollHost.PreviewMouseDown += (_, e) => {
			if (e.ChangedButton != System.Windows.Input.MouseButton.Middle)
				return;
			ResetZoom();
			e.Handled = true;
		};
		wpfScrollHost.PreviewMouseLeftButtonDown += (_, e) => {
			if (!CanPan())
				return;
			panning = true;
			var p = e.GetPosition(wpfScrollHost);
			panStartMouse = new Point((int)p.X, (int)p.Y);
			panStartScroll = scrollable.ScrollPosition;
			canvas.Cursor = Cursors.SizeAll;
			wpfScrollHost.CaptureMouse();
			e.Handled = true;
		};
		wpfScrollHost.PreviewMouseMove += (_, e) => {
			if (!panning)
				return;
			var p = e.GetPosition(wpfScrollHost);
			var current = new Point((int)p.X, (int)p.Y);
			var delta = current - panStartMouse;
			SetScrollPosition(new Point(panStartScroll.X - delta.X, panStartScroll.Y - delta.Y));
			e.Handled = true;
		};
		wpfScrollHost.PreviewMouseLeftButtonUp += (_, e) => {
			if (!panning)
				return;
			EndPan();
			e.Handled = true;
		};
		wpfScrollHost.MouseEnter += (_, _) => UpdatePanCursor();
	}

	private void ReleaseWpfMouseCapture() => wpfScrollHost?.ReleaseMouseCapture();
#endif
}
