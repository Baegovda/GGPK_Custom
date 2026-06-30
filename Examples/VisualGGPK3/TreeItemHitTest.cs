#if Windows
using System;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class TreeItemHitTest {
	public static ITreeItem? GetSelectableItemAt(System.Windows.Controls.TreeView tree, System.Windows.Point pos) {
		if (IsExpanderHit(tree.InputHitTest(pos) as System.Windows.DependencyObject))
			return null;

		var dep = tree.InputHitTest(pos) as System.Windows.DependencyObject;
		while (dep is not null && dep is not System.Windows.Controls.TreeViewItem)
			dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
		if (dep is not System.Windows.Controls.TreeViewItem tvi)
			return null;

		var bounds = GetLabelBounds(tvi, tree);
		if (!bounds.IsEmpty && !bounds.Contains(pos))
			return null;

		return GetTreeItem(tvi);
	}

	public static System.Windows.Rect GetLabelBounds(
		System.Windows.Controls.TreeViewItem tvi,
		System.Windows.Controls.TreeView tree) {
		tvi.ApplyTemplate();
		var label = FindTemplatePart<System.Windows.FrameworkElement>(tvi, "ItemBg")
			?? FindTemplatePart<System.Windows.FrameworkElement>(tvi, "PART_Header");
		if (label is null || label.ActualWidth <= 0 || label.ActualHeight <= 0)
			return System.Windows.Rect.Empty;
		try {
			var transform = label.TransformToAncestor(tree);
			return transform.TransformBounds(new System.Windows.Rect(0, 0, label.ActualWidth, label.ActualHeight));
		} catch {
			return System.Windows.Rect.Empty;
		}
	}

	public static bool IsExpanderHit(System.Windows.DependencyObject? dep) {
		while (dep is not null) {
			if (dep is System.Windows.Controls.Primitives.ToggleButton { Name: "Expander" })
				return true;
			if (dep is System.Windows.Controls.TreeViewItem)
				break;
			dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
		}
		return false;
	}

	public static ITreeItem? GetTreeItem(System.Windows.Controls.TreeViewItem container) {
		if (container.Header is ITreeItem headerItem)
			return headerItem;
		if (container.DataContext is ITreeItem dataItem)
			return dataItem;
		var nodeProp = container.DataContext?.GetType().GetProperty("Node");
		if (nodeProp?.GetValue(container.DataContext) is ITreeItem nodeItem)
			return nodeItem;
		return null;
	}

	private static T? FindTemplatePart<T>(System.Windows.DependencyObject parent, string name) where T : System.Windows.FrameworkElement {
		if (parent is T element && element.Name == name)
			return element;
		var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++) {
			var found = FindTemplatePart<T>(System.Windows.Media.VisualTreeHelper.GetChild(parent, i), name);
			if (found is not null)
				return found;
		}
		return null;
	}
}
#endif
