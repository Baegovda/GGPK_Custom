using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VisualGGPK3;

internal static class FavoriteFilesStore {
	private static string SettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"favorites.txt");

	public static IReadOnlyList<string> Load() {
		try {
			if (!File.Exists(SettingsPath))
				return Array.Empty<string>();
			var list = new List<string>();
			foreach (var line in File.ReadAllLines(SettingsPath)) {
				var path = FavoritePaths.Normalize(line);
				if (string.IsNullOrEmpty(path) || path.StartsWith('#'))
					continue;
				if (!list.Any(p => FavoritePaths.Equals(p, path)))
					list.Add(path);
			}
			return list;
		} catch {
			return Array.Empty<string>();
		}
	}

	public static bool Contains(string path) {
		var normalized = FavoritePaths.Normalize(path);
		return Load().Any(p => FavoritePaths.Equals(p, normalized));
	}

	public static void Add(string path) {
		var normalized = FavoritePaths.Normalize(path);
		if (string.IsNullOrEmpty(normalized))
			return;
		var list = Load().ToList();
		if (list.Any(p => FavoritePaths.Equals(p, normalized)))
			return;
		list.Add(normalized);
		Save(list);
	}

	public static void Remove(string path) {
		var normalized = FavoritePaths.Normalize(path);
		var list = Load().Where(p => !FavoritePaths.Equals(p, normalized)).ToList();
		Save(list);
	}

	private static void Save(IReadOnlyList<string> paths) {
		try {
			var dir = Path.GetDirectoryName(SettingsPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllLines(SettingsPath, paths);
		} catch {
			// ignore persistence errors
		}
	}
}
