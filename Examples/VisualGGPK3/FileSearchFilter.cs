using System;

namespace VisualGGPK3;

public static class FileSearchFilter {
	private static string Query = "";

	public static int Version { get; private set; }

	public static bool IsActive => Query.Length > 0;

	public static string Text => Query;

	public static void Clear() => Set(null);

	public static void Set(string? text) {
		Query = text?.Trim() ?? "";
		++Version;
	}

	public static bool MatchesPath(string path) {
		if (!IsActive)
			return true;
		return path.Contains(Query, StringComparison.OrdinalIgnoreCase);
	}
}
