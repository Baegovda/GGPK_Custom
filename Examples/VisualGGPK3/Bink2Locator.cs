using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace VisualGGPK3;

internal static class Bink2Locator {
	private static string? resolvedPath;

	private static readonly string[] RelativeDllPaths = [
		@"Daum Games\Path of Exile\bink2w64.dll",
		@"Daum Games\Path of Exile2\bink2w64.dll",
		@"Kakao Games\Path of Exile\bink2w64.dll",
		@"Kakao Games\Path of Exile 2\bink2w64.dll",
		@"Grinding Gear Games\Path of Exile\bink2w64.dll",
		@"Grinding Gear Games\Path of Exile 2\bink2w64.dll",
		@"Steam\steamapps\common\Path of Exile\bink2w64.dll",
		@"Steam\steamapps\common\Path of Exile 2\bink2w64.dll",
		@"Program Files (x86)\Steam\steamapps\common\Path of Exile\bink2w64.dll",
		@"Program Files (x86)\Steam\steamapps\common\Path of Exile 2\bink2w64.dll",
		@"Program Files (x86)\Grinding Gear Games\Path of Exile\bink2w64.dll",
		@"Program Files (x86)\Grinding Gear Games\Path of Exile 2\bink2w64.dll",
		@"Program Files\Grinding Gear Games\Path of Exile\bink2w64.dll",
		@"Program Files\Grinding Gear Games\Path of Exile 2\bink2w64.dll",
		@"Epic Games\PathOfExile\bink2w64.dll",
	];

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

	public static void SetCustomPath(string dllPath) {
		if (!File.Exists(dllPath))
			throw new FileNotFoundException("bink2w64.dll not found.", dllPath);
		if (!Path.GetFileName(dllPath).Equals("bink2w64.dll", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException("Select bink2w64.dll from your Path of Exile install folder.", nameof(dllPath));
		resolvedPath = Path.GetFullPath(dllPath);
		Bink2SettingsStore.Save(resolvedPath);
	}

	public static void InvalidateCache() => resolvedPath = null;

	private static IEnumerable<string> GetCandidatePaths() {
		var saved = Bink2SettingsStore.Load();
		if (!string.IsNullOrEmpty(saved))
			yield return saved;

		yield return Path.Combine(AppContext.BaseDirectory, "bink2w64.dll");

		foreach (var path in RelativeDllPaths) {
			foreach (var root in GetDriveRoots())
				yield return Path.Combine(root, path);
		}

		foreach (var steamRoot in GetSteamInstallRoots()) {
			yield return Path.Combine(steamRoot, @"steamapps\common\Path of Exile\bink2w64.dll");
			yield return Path.Combine(steamRoot, @"steamapps\common\Path of Exile 2\bink2w64.dll");
		}
	}

	private static IEnumerable<string> GetDriveRoots() {
		foreach (var drive in DriveInfo.GetDrives()) {
			if (drive.DriveType != DriveType.Fixed || !drive.IsReady)
				continue;
			yield return drive.RootDirectory.FullName;
		}
	}

	private static IEnumerable<string> GetSteamInstallRoots() {
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var root in GetDriveRoots()) {
			foreach (var rel in new[] {
				@"Program Files (x86)\Steam",
				@"Program Files\Steam",
				@"Steam"
			}) {
				var steam = Path.Combine(root, rel);
				if (!seen.Add(steam) || !Directory.Exists(steam))
					continue;
				yield return steam;
				var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
				if (!File.Exists(vdf))
					continue;
				foreach (var lib in ParseSteamLibraryFolders(vdf)) {
					if (seen.Add(lib))
						yield return lib;
				}
			}
		}
	}

	private static IEnumerable<string> ParseSteamLibraryFolders(string vdfPath) {
		string text;
		try {
			text = File.ReadAllText(vdfPath);
		} catch {
			yield break;
		}
		foreach (Match match in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\"")) {
			var path = match.Groups[1].Value.Replace(@"\\", @"\");
			if (Directory.Exists(path))
				yield return path;
		}
	}
}
