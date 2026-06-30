using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Eto.Drawing;

using Pfim;

using SystemExtensions;

using Index = LibBundle3.Index;

namespace VisualGGPK3;

internal static class ImageBitmapDecoder {
	public static bool IsExportableImagePath(string path) {
		var name = Path.GetFileName(path);
		if (name.EndsWith(".dds.header", StringComparison.OrdinalIgnoreCase))
			return true;
		return Path.GetExtension(name).ToLowerInvariant() switch {
			".dds" or ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".ico" => true,
			_ => false
		};
	}

	public static Bitmap Decode(ReadOnlySpan<byte> data, string fileName, Index? bundleIndex) {
		if (fileName.EndsWith(".dds.header", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".dds", StringComparison.OrdinalIgnoreCase)) {
			var resolved = ResolveDdsData(data, fileName, bundleIndex);
			return CreateDdsBitmap(resolved).Bitmap;
		}
		return new Bitmap(data.ToArray());
	}

	public static ReadOnlySpan<byte> ResolveDdsData(ReadOnlySpan<byte> data, string fileName, Index? bundleIndex) {
		if (!fileName.EndsWith(".header", StringComparison.OrdinalIgnoreCase))
			return data;
		data = data[0] == 3 ? data[28..] : data[16..];
		while (data.Length > 0 && data[0] == '*') {
			data = data[1..];
			if (data.Length > 384 * 1024)
				throw new StackOverflowException();
			Span<char> path = stackalloc char[data.Length * 2];
			var pathSpan = path[Encoding.UTF8.GetChars(data, path)..];
			if (bundleIndex is null || !bundleIndex.TryGetFile(pathSpan, out var file))
				throw new FileNotFoundException(null, pathSpan.ToString());
			data = file.Read().Span;
			var pathText = pathSpan.ToString();
			if (pathText.EndsWith(".header", StringComparison.OrdinalIgnoreCase))
				data = data[0] == 3 ? data[28..] : data[16..];
		}
		return data;
	}

	public static (Bitmap Bitmap, string Info) CreateDdsBitmap(ReadOnlySpan<byte> data) {
		unsafe {
			fixed (byte* p = data) {
				using var image = Dds.Create(new UnmanagedMemoryStream(p, data.Length), new(allocator: ArrayPoolAllocator.Instance));
				var format = image.Format switch {
					Pfim.ImageFormat.Rgba32 => PixelFormat.Format32bppRgba,
					Pfim.ImageFormat.Rgb24 => PixelFormat.Format24bppRgb,
					_ => throw ThrowHelper.Create<NotSupportedException>()
				};
				var info = $"DDS format: {image.Format}\nStride: {image.Stride} bytes/row\nDecoded pixels: {image.Data.Length:N0} bytes";
				var bitmap = new Bitmap(image.Width, image.Height, format, ToColors(image.Data, format));
				return (bitmap, info);
			}
		}

		static IEnumerable<Color> ToColors(byte[] data, PixelFormat format) {
			var argb = format == PixelFormat.Format32bppRgba;
			var bpp = argb ? 4 : 3;

			for (var i = 0; i < data.Length; i += bpp) {
				var pixel = MemoryMarshal.Read<int>(new(data, i, sizeof(int)));
				yield return argb ? Color.FromArgb(pixel) : Color.FromRgb(pixel);
			}
		}
	}
}
