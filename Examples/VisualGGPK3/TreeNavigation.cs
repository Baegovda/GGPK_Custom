using System.Collections.Generic;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class TreeNavigation {
	public static bool ShouldExpandOnRightArrow(ITreeItem selected) =>
		selected is DirectoryTreeItem { Expanded: false, Expandable: true };

	public static ITreeItem? GetNextVisibleItem(ITreeItem root, ITreeItem current) {
		var visible = GetVisibleItems(root);
		var index = IndexOfVisible(visible, current);
		if (index < 0 || index >= visible.Count - 1)
			return null;
		return visible[index + 1];
	}

	public static ITreeItem? GetNextFileItem(ITreeItem root, ITreeItem current) {
		var visible = GetVisibleItems(root);
		var index = IndexOfVisible(visible, current);
		if (index < 0)
			return null;
		for (var i = index + 1; i < visible.Count; i++) {
			if (visible[i] is FileTreeItem)
				return visible[i];
		}
		return null;
	}

	public static int IndexOfVisible(IReadOnlyList<ITreeItem> visible, ITreeItem current) => FindVisibleIndex(visible, current);

	public static List<ITreeItem> GetVisibleItems(ITreeItem root) {
		var list = new List<ITreeItem>();
		CollectVisible(root, list);
		return list;
	}

	private static int FindVisibleIndex(IReadOnlyList<ITreeItem> visible, ITreeItem current) {
		for (var i = 0; i < visible.Count; i++) {
			if (TreeItemIdentity.Same(visible[i], current))
				return i;
		}
		return -1;
	}

	private static bool IsSameItem(ITreeItem a, ITreeItem b) => TreeItemIdentity.Same(a, b);

	private static void CollectVisible(ITreeItem node, List<ITreeItem> list) {
		list.Add(node);
		if (node is not DirectoryTreeItem dir || !dir.Expanded || !dir.Initialized)
			return;
		foreach (var child in dir.ChildItems)
			CollectVisible(child, list);
	}
}
