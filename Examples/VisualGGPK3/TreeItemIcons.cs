using System;
using System.IO;
using System.Reflection;

using Eto.Drawing;

namespace VisualGGPK3;

internal static class TreeItemIcons {
	public static readonly Bitmap File;
	public static readonly Bitmap Directory;
	public static readonly Bitmap FavoriteStar;

	static TreeItemIcons() {
		try {
			using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("VisualGGPK3.Resources.file.ico");
			File = new(s);
		} catch {
			File = null!;
		}
		try {
			using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("VisualGGPK3.Resources.dir.ico");
			Directory = new(s);
		} catch {
			Directory = null!;
		}
		FavoriteStar = CreateStarBitmap(16);
	}

	private static Bitmap CreateStarBitmap(int size) {
		var bitmap = new Bitmap(size, size, PixelFormat.Format32bppRgba);
		using var graphics = new Graphics(bitmap);
		graphics.Clear(Colors.Transparent);
		var fill = Color.FromRgb(0xf5c52d);
		var outline = Color.FromRgb(0xc99a1e);
		var points = CreateStarPoints(size);
		graphics.FillPolygon(fill, points);
		graphics.DrawPolygon(new Pen(outline, 1), points);
		return bitmap;
	}

	private static PointF[] CreateStarPoints(int size) {
		var center = size / 2f;
		var outer = size / 2f - 1f;
		var inner = outer * 0.42f;
		var points = new PointF[10];
		for (var i = 0; i < 10; i++) {
			var radius = i % 2 == 0 ? outer : inner;
			var angle = Math.PI / 2 + i * Math.PI / 5;
			points[i] = new PointF(
				center + (float)(radius * Math.Cos(angle)),
				center - (float)(radius * Math.Sin(angle)));
		}
		return points;
	}
}