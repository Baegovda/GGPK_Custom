using System;
using System.Collections.Generic;
using System.IO;

namespace VisualGGPK3;

public static class FileFormatFilter {
	private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase);

	public static int Version { get; private set; }

	public static bool IsActive => Extensions.Count > 0;

	public static void Clear() => Set(null);

	public static void Set(string? text) {
		Extensions.Clear();
		if (!string.IsNullOrWhiteSpace(text)) {
			foreach (var part in text.Split([' ', ',', ';', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
				if (part.Length == 0)
					continue;
				Extensions.Add(part.StartsWith('.') ? part : '.' + part);
			}
		}
		++Version;
	}

	public static void SetPreset(string preset) => Set(preset switch {
		"Images" => ".dds .png .jpg .jpeg .bmp .gif .tiff .ico .header",
		"Text" => ".txt .xml .json .csv .filter .fx .hlsl .properties .ui .amd",
		"Data" => ".dat .dat64 .datc .datl",
		"Audio" => ".ogg .wav .bank .mp3",
		"Video" => ".bk2 .mp4",
		_ => null
	});

	public static bool Matches(string fileName) {
		if (!IsActive)
			return true;
		return Extensions.Contains(Path.GetExtension(fileName));
	}
}
