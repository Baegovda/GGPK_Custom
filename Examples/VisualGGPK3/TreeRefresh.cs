using System.Linq;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class TreeRefresh {
	public static void ApplyFilterChange(TreeView tree) {
		if (tree.DataStore is not DirectoryTreeItem root)
			return;
#pragma warning disable CS0618
		var selected = tree.SelectedItem;
#pragma warning restore CS0618
		var multiSelected = TreeMultiSelection.Get(tree)?.Selected.ToArray();

		InvalidateFilterCacheDeep(root);
		CollapseEmptyExpanded(root);
		RefreshExpandedDirectories(tree, root);

#pragma warning disable CS0618
		if (selected is not null)
			tree.SelectedItem = selected;
#pragma warning restore CS0618
		if (multiSelected is { Length: > 0 })
			TreeMultiSelection.Get(tree)?.RestoreSelection(multiSelected);
		else
			TreeMultiSelection.Get(tree)?.RefreshVisuals();
	}

	public static void RefreshExpandedDirectories(TreeView tree, DirectoryTreeItem dir) {
#pragma warning disable CS0618
		tree.RefreshItem(dir);
#pragma warning restore CS0618
		if (!dir.Initialized || !dir.Expanded)
			return;
		foreach (var child in dir.ChildItems) {
			if (child is DirectoryTreeItem sub)
				RefreshExpandedDirectories(tree, sub);
		}
	}

	public static void RefreshPath(TreeView tree, FileTreeItem file) {
#pragma warning disable CS0618
		for (var node = file.Parent; node is not null; node = node.Parent)
			tree.RefreshItem(node);
		tree.RefreshItem(file);
#pragma warning restore CS0618
	}

	private static void InvalidateFilterCacheDeep(DirectoryTreeItem dir) {
		dir.InvalidateFilterCache();
		if (!dir.Initialized)
			return;
		foreach (var child in dir.EnumerateAllChildren()) {
			if (child is DirectoryTreeItem sub)
				InvalidateFilterCacheDeep(sub);
		}
	}

	private static void CollapseEmptyExpanded(DirectoryTreeItem dir) {
		if (!dir.Initialized)
			return;
		foreach (var child in dir.EnumerateAllChildren()) {
			if (child is DirectoryTreeItem sub)
				CollapseEmptyExpanded(sub);
		}
		dir.CollapseIfEmptyFiltered();
	}
}
