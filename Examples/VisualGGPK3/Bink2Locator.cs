using System;
using System.Collections.Generic;
using System.IO;

namespace VisualGGPK3;

internal static class Bink2Locator {
	private static string? resolvedPath;

	public static bool IsAvailable => TryGetDllPath() is not null;

	public static string? TryGetDllPath() {
		if (resolvedPath is not null && File.Exists(resolvedPath))
			return resolvedPath;
		foreach (var path in GetCandidatePaths()) {
			if (!File.Exists(path))
				continue;
			resolvedPath = path;
			return path;
		}
		return null;
	}

	private static IEnumerable<string> GetCandidatePaths() {
		var appDir = AppContext.BaseDirectory;
		yield return Path.Combine(appDir, "bink2w64.dll");

		var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
		yield return Path.Combine(programFilesX86, "Grinding Gear Games", "Path of Exile", "bink2w64.dll");
		yield return Path.Combine(programFilesX86, "Steam", "steamapps", "common", "Path of Exile", "bink2w64.dll");
		yield return Path.Combine(programFilesX86, "Steam", "steamapps", "common", "Path of Exile 2", "bink2w64.dll");
		yield return Path.Combine(programFilesX86, "Grinding Gear Games", "Path of Exile 2", "bink2w64.dll");

		var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
		yield return Path.Combine(programFiles, "Grinding Gear Games", "Path of Exile", "bink2w64.dll");
		yield return Path.Combine(programFiles, "Grinding Gear Games", "Path of Exile 2", "bink2w64.dll");
	}
}
