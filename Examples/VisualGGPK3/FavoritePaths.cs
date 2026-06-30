using System;

namespace VisualGGPK3;

internal static class FavoritePaths {
	public static string Normalize(string path) => path.Replace('\\', '/').Trim();

	public static bool Equals(string a, string b) =>
		string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
}
