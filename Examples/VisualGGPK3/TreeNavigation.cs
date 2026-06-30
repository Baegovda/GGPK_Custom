using System.Collections.Generic;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class TreeNavigation {
	public static ITreeItem? GetNextVisibleItem(ITreeItem root, ITreeItem current) {
		ITreeItem? next = null;
		var seeking = false;
		if (Walk(root))
			return next;
		return next;

		bool Walk(ITreeItem node) {
			if (next is not null)
				return false;
			if (seeking) {
				next = node;
				return false;
			}
			if (ReferenceEquals(node, current))
				seeking = true;
			if (node is DirectoryTreeItem dir && dir.Expanded && dir.Initialized) {
				foreach (var child in dir.ChildItems) {
					if (!Walk(child))
						return false;
				}
			}
			return true;
		}
	}

	public static List<ITreeItem> GetVisibleItems(ITreeItem root) {
		var list = new List<ITreeItem>();
		CollectVisible(root, list);
		return list;
	}

	private static void CollectVisible(ITreeItem node, List<ITreeItem> list) {
		list.Add(node);
		if (node is not DirectoryTreeItem dir || !dir.Expanded || !dir.Initialized)
			return;
		foreach (var child in dir.ChildItems)
			CollectVisible(child, list);
	}
}
