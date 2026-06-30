using System;
using System.IO;

namespace VisualGGPK3;

internal static class Bink2SettingsStore {
	private static string SettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"bink2.txt");

	public static string? Load() {
		try {
			if (!File.Exists(SettingsPath))
				return null;
			var path = File.ReadAllText(SettingsPath).Trim();
			return path.Length == 0 ? null : path;
		} catch {
			return null;
		}
	}

	public static void Save(string dllPath) {
		try {
			var dir = Path.GetDirectoryName(SettingsPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllText(SettingsPath, dllPath);
		} catch {
			// ignore persistence errors
		}
	}

	public static void Clear() {
		try {
			if (File.Exists(SettingsPath))
				File.Delete(SettingsPath);
		} catch {
		}
	}
}
