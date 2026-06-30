using System.Collections.Generic;

using LibGGPK3;
using LibGGPK3.Records;

using VisualGGPK3.TreeItems;
using Index = LibBundle3.Index;

namespace VisualGGPK3;

internal static class FavoriteFileLocator {
	public static FileTreeItem? Find(string path, Index? index, GGPK? ggpk, GGPKDirectoryTreeItem? ggpkRoot, BundleDirectoryTreeItem? bundleRoot) {
		path = FavoritePaths.Normalize(path);
		if (index is not null && index.TryGetFile(path, out _) && bundleRoot is not null) {
			var bundleFile = bundleRoot.FindFileByPath(path);
			if (bundleFile is not null)
				return bundleFile;
		}
		if (ggpk is not null && ggpkRoot is not null && ggpk.Root.TryFindNode(path, out var node) && node is FileRecord) {
			var ggpkFile = ggpkRoot.FindFileByPath(path);
			if (ggpkFile is not null)
				return ggpkFile;
		}
		return null;
	}

	public static void ExpandTo(FileTreeItem file) {
		var chain = new List<DirectoryTreeItem>();
		for (var dir = file.Parent; dir is not null; dir = dir.Parent)
			chain.Add(dir);
		for (var i = chain.Count - 1; i >= 0; i--)
			chain[i].Expanded = true;
	}
}
