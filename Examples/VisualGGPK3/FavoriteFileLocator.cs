using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class FavoriteFileLocator {
	public static FileTreeItem? Find(string path, GGPKDirectoryTreeItem? ggpkRoot, BundleDirectoryTreeItem? bundleRoot) {
		if (ggpkRoot is not null) {
			var ggpkFile = ggpkRoot.FindFileByPath(path);
			if (ggpkFile is not null)
				return ggpkFile;
		}
		if (bundleRoot is not null)
			return bundleRoot.FindFileByPath(path);
		return null;
	}

	public static void ExpandTo(FileTreeItem file) {
		var dir = file.Parent;
		while (dir is not null) {
			dir.Expanded = true;
			dir = dir.Parent;
		}
	}
}
