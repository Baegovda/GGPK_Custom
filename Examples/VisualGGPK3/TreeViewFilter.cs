using System.IO;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

public static class TreeViewFilter {
	public static int Version => unchecked(FileFormatFilter.Version * 31 + FileSearchFilter.Version * 17 + FileExcludeFilter.Version);

	public static bool IsActive => FileFormatFilter.IsActive || FileSearchFilter.IsActive || FileExcludeFilter.IsActive;

	public static bool MatchesFile(FileTreeItem file) =>
		FileFormatFilter.Matches(file.Name)
		&& FileSearchFilter.MatchesPath(file.GetPath())
		&& FileExcludeFilter.MatchesPath(file.GetPath());

	public static bool MatchesPath(string path) =>
		FileFormatFilter.Matches(Path.GetFileName(path))
		&& FileSearchFilter.MatchesPath(path)
		&& FileExcludeFilter.MatchesPath(path);
}
