using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal sealed class TreeMultiSelection {
	private static readonly ConditionalWeakTable<TreeView, TreeMultiSelection> instances = new();

	private readonly TreeView etoTree;
	private readonly HashSet<ITreeItem> selected = new(ReferenceEqualityComparer.Instance);
	private ITreeItem? anchor;
	private ITreeItem? primary;
	private bool internalChange;

#if Windows
	private readonly System.Windows.Controls.TreeView wpfTree;
	private static readonly System.Windows.Media.Brush PrimaryBrush = new System.Windows.Media.SolidColorBrush(
		System.Windows.Media.Color.FromRgb(0x33, 0x99, 0xFF));
	private static readonly System.Windows.Media.Brush SecondaryBrush = new System.Windows.Media.SolidColorBrush(
		System.Windows.Media.Color.FromRgb(0x55, 0x77, 0xAA));
#endif

	private TreeMultiSelection(TreeView etoTree) {
		this.etoTree = etoTree;
#if Windows
		wpfTree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)etoTree.Handler).Control;
		wpfTree.PreviewMouseDown += OnPreviewMouseDown;
		wpfTree.ItemContainerGenerator.StatusChanged += (_, _) => ApplyVisuals();
#endif
		etoTree.SelectionChanged += OnEtoSelectionChanged;
	}

	public static TreeMultiSelection Enable(TreeView tree) {
		if (instances.TryGetValue(tree, out var existing))
			return existing;
		var ms = new TreeMultiSelection(tree);
		instances.Add(tree, ms);
		return ms;
	}

	public static TreeMultiSelection? Get(TreeView tree) {
		instances.TryGetValue(tree, out var ms);
		return ms;
	}

	public IReadOnlyCollection<ITreeItem> Selected => selected;

	public ITreeItem? Primary => primary;

	public void SelectSingle(ITreeItem item) {
		selected.Clear();
		selected.Add(item);
		anchor = item;
		SetPrimary(item, notifyEto: false);
		ApplyVisuals();
	}

	public void RestoreSelection(IEnumerable<ITreeItem> items) {
		selected.Clear();
		foreach (var item in items)
			selected.Add(item);
		if (primary is not null && !selected.Contains(primary))
			primary = selected.FirstOrDefault();
		if (primary is not null)
			SetPrimary(primary, notifyEto: true);
		ApplyVisuals();
	}

	public void RefreshVisuals() => ApplyVisuals();

	private void OnEtoSelectionChanged(object? sender, EventArgs _) {
		if (internalChange)
			return;
#pragma warning disable CS0618
		var item = etoTree.SelectedItem;
#pragma warning restore CS0618
		if (item is null) {
			selected.Clear();
			primary = null;
			ApplyVisuals();
			return;
		}
		if (selected.Count <= 1 && (selected.Count == 0 || selected.Contains(item))) {
			SelectSingle(item);
			return;
		}
		if (!selected.Contains(item))
			SelectSingle(item);
		else
			SetPrimary(item, notifyEto: false);
	}

#if Windows
	private void OnPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
		if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
			return;
		var item = GetItemAt(wpfTree, e.GetPosition(wpfTree));
		if (item is null)
			return;

		var mods = System.Windows.Input.Keyboard.Modifiers;
		if (mods.HasFlag(System.Windows.Input.ModifierKeys.Control)) {
			Toggle(item);
			e.Handled = true;
			return;
		}
		if (mods.HasFlag(System.Windows.Input.ModifierKeys.Shift) && anchor is not null) {
			SelectRange(anchor, item);
			e.Handled = true;
		}
	}

	private void Toggle(ITreeItem item) {
		if (!selected.Remove(item))
			selected.Add(item);
		anchor = item;
		if (selected.Count == 0) {
			primary = null;
			SetPrimary(null, notifyEto: true);
		} else if (primary is null || !selected.Contains(primary))
			SetPrimary(item, notifyEto: true);
		else
			ApplyVisuals();
	}

	private void SelectRange(ITreeItem from, ITreeItem to) {
		if (etoTree.DataStore is not ITreeItem root)
			return;
		var visible = TreeNavigation.GetVisibleItems(root);
		var start = visible.IndexOf(from);
		var end = visible.IndexOf(to);
		if (start < 0 || end < 0)
			return;
		if (start > end)
			(start, end) = (end, start);
		selected.Clear();
		for (var i = start; i <= end; i++)
			selected.Add(visible[i]);
		SetPrimary(to, notifyEto: true);
	}

	private static ITreeItem? GetItemAt(System.Windows.Controls.TreeView tree, System.Windows.Point pos) {
		var dep = tree.InputHitTest(pos) as System.Windows.DependencyObject;
		while (dep is not null && dep is not System.Windows.Controls.TreeViewItem)
			dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
		return dep is System.Windows.Controls.TreeViewItem tvi ? GetTreeItem(tvi) : null;
	}

	private static ITreeItem? GetTreeItem(System.Windows.Controls.TreeViewItem container) {
		if (container.Header is ITreeItem headerItem)
			return headerItem;
		if (container.DataContext is ITreeItem dataItem)
			return dataItem;
		var nodeProp = container.DataContext?.GetType().GetProperty("Node");
		if (nodeProp?.GetValue(container.DataContext) is ITreeItem nodeItem)
			return nodeItem;
		return null;
	}

	private void ApplyVisuals() {
		foreach (var tvi in EnumerateTreeViewItems(wpfTree)) {
			var item = GetTreeItem(tvi);
			if (item is not null && selected.Contains(item)) {
				tvi.Background = ReferenceEquals(item, primary) ? PrimaryBrush : SecondaryBrush;
			} else {
				tvi.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
			}
		}
	}

	private static IEnumerable<System.Windows.Controls.TreeViewItem> EnumerateTreeViewItems(System.Windows.Controls.ItemsControl parent) {
		if (parent.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
			yield break;
		foreach (var child in parent.Items) {
			if (parent.ItemContainerGenerator.ContainerFromItem(child) is not System.Windows.Controls.TreeViewItem tvi)
				continue;
			yield return tvi;
			foreach (var nested in EnumerateTreeViewItems(tvi))
				yield return nested;
		}
	}
#else
	private void Toggle(ITreeItem item) {
		if (!selected.Remove(item))
			selected.Add(item);
		anchor = item;
		if (selected.Count == 0)
			SetPrimary(null, notifyEto: true);
		else
			SetPrimary(item, notifyEto: true);
	}

	private void SelectRange(ITreeItem from, ITreeItem to) {
		if (etoTree.DataStore is not ITreeItem root)
			return;
		var visible = TreeNavigation.GetVisibleItems(root);
		var start = visible.IndexOf(from);
		var end = visible.IndexOf(to);
		if (start < 0 || end < 0)
			return;
		if (start > end)
			(start, end) = (end, start);
		selected.Clear();
		for (var i = start; i <= end; i++)
			selected.Add(visible[i]);
		SetPrimary(to, notifyEto: true);
	}

	public void OnMouseDown(ITreeItem? item, bool control, bool shift) {
		if (item is null)
			return;
		if (control) {
			Toggle(item);
			return;
		}
		if (shift && anchor is not null)
			SelectRange(anchor, item);
	}

	private void ApplyVisuals() { }
#endif

	private void SetPrimary(ITreeItem? item, bool notifyEto) {
		primary = item;
		if (!notifyEto)
			return;
		internalChange = true;
		try {
#pragma warning disable CS0618
			etoTree.SelectedItem = item;
#pragma warning restore CS0618
		} finally {
			internalChange = false;
		}
	}
}

file sealed class ReferenceEqualityComparer : IEqualityComparer<ITreeItem> {
	public static readonly ReferenceEqualityComparer Instance = new();
	public bool Equals(ITreeItem? x, ITreeItem? y) => ReferenceEquals(x, y);
	public int GetHashCode(ITreeItem obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}
