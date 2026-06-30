using System.IO;
using System.Text.RegularExpressions;

namespace VisualGGPK3;

internal readonly struct UvSequenceGrid(int columns, int rows) {
	public int Columns { get; } = columns;
	public int Rows { get; } = rows;
	public int FrameCount => Columns * Rows;

	private static readonly Regex Pattern = new(@"(?<![\d.])(\d{1,2})x(\d{1,2})(?![\d.])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	public static bool TryParse(string fileName, string? path, out UvSequenceGrid grid) {
		if (TryParseSegment(Path.GetFileNameWithoutExtension(fileName), out grid))
			return true;
		if (!string.IsNullOrEmpty(path) && TryParsePath(path, out grid))
			return true;
		grid = default;
		return false;
	}

	private static bool TryParsePath(string path, out UvSequenceGrid grid) {
		var normalized = path.Replace('\\', '/').TrimEnd('/');
		var slash = normalized.LastIndexOf('/');
		var fileSegment = slash >= 0 ? normalized[(slash + 1)..] : normalized;
		if (TryParseSegment(Path.GetFileNameWithoutExtension(fileSegment), out grid))
			return true;
		return TryParseSegment(normalized, out grid);
	}

	private static bool TryParseSegment(string text, out UvSequenceGrid grid) {
		grid = default;
		var match = Pattern.Match(text);
		if (!match.Success)
			return false;
		if (!int.TryParse(match.Groups[1].Value, out var columns) || !int.TryParse(match.Groups[2].Value, out var rows))
			return false;
		if (columns is < 1 or > 32 || rows is < 1 or > 32)
			return false;
		grid = new UvSequenceGrid(columns, rows);
		return true;
	}
}
