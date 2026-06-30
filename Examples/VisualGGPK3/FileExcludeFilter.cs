using System;
using System.Collections.Generic;

namespace VisualGGPK3;

public static class FileExcludeFilter {
	private static string rawQuery = "";
	private static string[] terms = [];

	public static int Version { get; private set; }

	public static bool IsActive => terms.Length > 0;

	public static string Text => rawQuery;

	public static IReadOnlyList<string> Terms => terms;

	public static void Clear() => Set(null);

	public static void Set(string? text) {
		rawQuery = text?.Trim() ?? "";
		terms = ParseTerms(rawQuery);
		++Version;
	}

	public static bool MatchesPath(string path) {
		if (!IsActive)
			return true;
		foreach (var term in terms) {
			if (path.Contains(term, StringComparison.OrdinalIgnoreCase))
				return false;
		}
		return true;
	}

	private static string[] ParseTerms(string text) {
		if (string.IsNullOrWhiteSpace(text))
			return [];
		return text.Split([',', ';', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}
}
