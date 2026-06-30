using System;
using System.IO;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

public static class TreeViewFilter {
	private static string? revealPath;
	private static int revealVersion;

	public static int Version => unchecked(
		FileFormatFilter.Version * 31
		+ FileSearchFilter.Version * 17
		+ FileExcludeFilter.Version
		+ revealVersion * 13);

	public static bool IsActive => FileFormatFilter.IsActive || FileSearchFilter.IsActive || FileExcludeFilter.IsActive;

	public static void SetRevealPath(string? path) {
		var normalized = string.IsNullOrWhiteSpace(path) ? null : FavoritePaths.Normalize(path);
		if (string.Equals(revealPath, normalized, StringComparison.OrdinalIgnoreCase))
			return;
		revealPath = normalized;
		++revealVersion;
	}

	public static void ClearRevealPath() => SetRevealPath(null);

	public static bool MatchesFile(FileTreeItem file) =>
		MatchesPath(file.GetPath());

	public static bool MatchesPath(string path) {
		if (path.IndexOf('\\') >= 0)
			path = path.Replace('\\', '/');
		if (revealPath is not null && PathMatchesReveal(path, revealPath))
			return true;
		return FileFormatFilter.Matches(Path.GetFileName(path), path)
			&& FileSearchFilter.MatchesPath(path)
			&& FileExcludeFilter.MatchesPath(path);
	}

	private static bool PathMatchesReveal(string path, string reveal) {
		if (path.Equals(reveal, StringComparison.OrdinalIgnoreCase))
			return true;
		if (path.StartsWith(reveal + "/", StringComparison.OrdinalIgnoreCase))
			return true;
		if (reveal.StartsWith(path + "/", StringComparison.OrdinalIgnoreCase))
			return true;
		return false;
	}
}
