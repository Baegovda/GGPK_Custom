using System;

namespace VisualGGPK3;

internal static class FavoritePaths {
	public static string Normalize(string path) => path.Replace('\\', '/').Trim();

	public static bool IsDirectory(string path) => Normalize(path).EndsWith('/');

	public static string DirectoryLookupPath(string path) => Normalize(path).TrimEnd('/');

	public static bool Equals(string a, string b) =>
		string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
}
