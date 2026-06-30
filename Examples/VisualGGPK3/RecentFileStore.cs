using System;
using System.IO;

namespace VisualGGPK3;

internal static class RecentFileStore {
	private static string SettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"last.txt");

	public static string? Load() {
		try {
			if (!File.Exists(SettingsPath))
				return null;
			var path = File.ReadAllText(SettingsPath).Trim();
			return string.IsNullOrEmpty(path) ? null : path;
		} catch {
			return null;
		}
	}

	public static void Save(string path) {
		try {
			var dir = Path.GetDirectoryName(SettingsPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllText(SettingsPath, Path.GetFullPath(path));
		} catch {
			// ignore persistence errors
		}
	}
}
