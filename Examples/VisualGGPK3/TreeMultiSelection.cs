using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal sealed class TreeMultiSelection {
	private const double DragThreshold = 4;

	private static readonly ConditionalWeakTable<TreeView, TreeMultiSelection> instances = new();

	private readonly TreeView etoTree;
	private readonly HashSet<ITreeItem> selected = new(PathItemComparer.Instance);
	private ITreeItem? anchor;
	private ITreeItem? primary;
	private bool internalChange;

#if Windows
	private readonly System.Windows.Controls.TreeView wpfTree;
	private static readonly System.Windows.Media.Brush PrimaryBrush = WpfDarkTheme.PrimarySelectionBrush;
	private static readonly System.Windows.Media.Brush SecondaryBrush = WpfDarkTheme.SecondarySelectionBrush;
	private System.Windows.Point dragStartPoint;
	private ITreeItem? dragAnchorItem;
	private bool dragSelectActive;
	private bool marqueePending;
	private bool marqueeActive;
	private TreeMarqueeAdorner? marqueeAdorner;
#endif

	private TreeMultiSelection(TreeView etoTree) {
		this.etoTree = etoTree;
#if Windows
		wpfTree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)etoTree.Handler).Control;
		wpfTree.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
		wpfTree.PreviewMouseMove += OnPreviewMouseMove;
		wpfTree.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
		wpfTree.LostMouseCapture += (_, _) => FinishMarqueeSelection();
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
		SetPrimary(item, notifyEto: true);
		ApplyVisuals();
	}

	public void RestoreSelection(IEnumerable<ITreeItem> items) {
		selected.Clear();
		foreach (var item in items)
			selected.Add(item);
		if (primary is not null && !ContainsSelected(primary))
			primary = selected.FirstOrDefault();
		if (primary is not null)
			SetPrimary(primary, notifyEto: true);
		ApplyVisuals();
	}

	public void RefreshVisuals() => ApplyVisuals();

	private bool ContainsSelected(ITreeItem item) {
		foreach (var selectedItem in selected) {
			if (TreeItemIdentity.Same(selectedItem, item))
				return true;
		}
		return false;
	}

	private ITreeItem? FindSelected(ITreeItem item) {
		foreach (var selectedItem in selected) {
			if (TreeItemIdentity.Same(selectedItem, item))
				return selectedItem;
		}
		return null;
	}

	private void OnEtoSelectionChanged(object? sender, EventArgs _) {
		if (internalChange)
			return;
#pragma warning disable CS0618
		var item = etoTree.SelectedItem as ITreeItem;
#pragma warning restore CS0618
		if (item is null) {
			if (selected.Count == 0) {
				primary = null;
				ApplyVisuals();
			}
			return;
		}

		if (selected.Count == 0) {
			SelectSingle(item);
			return;
		}

		var known = FindSelected(item);
		if (known is not null) {
			primary = known;
			ApplyVisuals();
			return;
		}

		if (selected.Count == 1)
			SelectSingle(item);
	}

#if Windows
	private void OnPreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
		if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
			return;

		FinishMarqueeSelection();

		dragStartPoint = e.GetPosition(wpfTree);
		dragSelectActive = false;
		marqueePending = false;
		marqueeActive = false;

		var item = GetItemAt(wpfTree, dragStartPoint);
		dragAnchorItem = item;
		var mods = System.Windows.Input.Keyboard.Modifiers;
		var ctrl = mods.HasFlag(System.Windows.Input.ModifierKeys.Control);
		var shift = mods.HasFlag(System.Windows.Input.ModifierKeys.Shift);

		if (item is null) {
			if (!ctrl && !shift)
				ClearSelection();
			marqueePending = !ctrl && !shift;
			e.Handled = true;
			return;
		}

		if (ctrl && !shift) {
			Toggle(item);
			e.Handled = true;
			return;
		}

		if (shift && anchor is not null) {
			SelectRange(anchor, item, additive: ctrl);
			e.Handled = true;
			return;
		}

		SelectSingle(item);
		e.Handled = true;
	}

	private void OnPreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
		if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
			return;

		var pos = e.GetPosition(wpfTree);

		if (marqueePending || marqueeActive) {
			if (!marqueeActive) {
				if (Math.Abs(pos.X - dragStartPoint.X) < DragThreshold && Math.Abs(pos.Y - dragStartPoint.Y) < DragThreshold)
					return;
				if (!BeginMarquee(dragStartPoint))
					return;
				marqueePending = false;
				marqueeActive = true;
			}
			UpdateMarquee(pos);
			e.Handled = true;
			return;
		}

		if (dragAnchorItem is null || anchor is null)
			return;

		if (!dragSelectActive) {
			if (Math.Abs(pos.X - dragStartPoint.X) < DragThreshold && Math.Abs(pos.Y - dragStartPoint.Y) < DragThreshold)
				return;
			dragSelectActive = true;
		}

		var current = GetItemAt(wpfTree, pos);
		if (current is null)
			return;

		var ctrl = System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control);
		SelectRange(anchor, current, additive: ctrl);
		e.Handled = true;
	}

	private void OnPreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e) {
		if (e.ChangedButton != System.Windows.Input.MouseButton.Left)
			return;

		if (marqueeActive || marqueeAdorner is not null) {
			var end = e.GetPosition(wpfTree);
			var rect = CreateRect(dragStartPoint, end);
			SelectItemsInRect(rect, additive: System.Windows.Input.Keyboard.Modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control));
			FinishMarqueeSelection();
			dragSelectActive = false;
			dragAnchorItem = null;
			e.Handled = true;
			return;
		}

		dragSelectActive = false;
		dragAnchorItem = null;
		marqueePending = false;
	}

	private void FinishMarqueeSelection() {
		EndMarquee();
		marqueeActive = false;
		marqueePending = false;
		if (wpfTree.IsMouseCaptured)
			wpfTree.ReleaseMouseCapture();
	}

	private void ClearSelection() {
		selected.Clear();
		anchor = null;
		SetPrimary(null, notifyEto: true);
		ApplyVisuals();
	}

	private bool BeginMarquee(System.Windows.Point start) {
		var layer = FindAdornerLayer(wpfTree);
		if (layer is null)
			return false;
		marqueeAdorner = new TreeMarqueeAdorner(wpfTree, start);
		layer.Add(marqueeAdorner);
		wpfTree.CaptureMouse();
		return true;
	}

	private static System.Windows.Documents.AdornerLayer? FindAdornerLayer(System.Windows.DependencyObject element) {
		for (var current = element; current is not null; current = System.Windows.Media.VisualTreeHelper.GetParent(current)) {
			if (current is not System.Windows.Media.Visual visual)
				continue;
			var layer = System.Windows.Documents.AdornerLayer.GetAdornerLayer(visual);
			if (layer is not null)
				return layer;
		}
		return null;
	}

	private void UpdateMarquee(System.Windows.Point end) => marqueeAdorner?.Update(end);

	private void EndMarquee() {
		if (marqueeAdorner is null)
			return;
		var layer = FindAdornerLayer(wpfTree);
		layer?.Remove(marqueeAdorner);
		marqueeAdorner = null;
	}

	private void SelectItemsInRect(System.Windows.Rect rect, bool additive) {
		if (rect.Width < DragThreshold && rect.Height < DragThreshold)
			return;

		var hits = new List<ITreeItem>();
		foreach (var tvi in EnumerateTreeViewItems(wpfTree)) {
			if (!tvi.IsVisible || tvi.ActualHeight <= 0)
				continue;
			var item = GetTreeItem(tvi);
			if (item is null)
				continue;
			var bounds = GetTreeViewItemBounds(tvi);
			if (bounds.IsEmpty || bounds.Width <= 0 || bounds.Height <= 0)
				continue;
			if (rect.IntersectsWith(bounds))
				hits.Add(item);
		}
		if (hits.Count == 0)
			return;

		if (!additive)
			selected.Clear();
		foreach (var item in hits)
			selected.Add(item);
		anchor = hits[^1];
		SetPrimary(hits[^1], notifyEto: true);
		ApplyVisuals();
	}

	private static System.Windows.Rect GetTreeViewItemBounds(System.Windows.Controls.TreeViewItem tvi) {
		try {
			var root = FindRootTreeView(tvi);
			if (root is null)
				return System.Windows.Rect.Empty;
			var transform = tvi.TransformToAncestor(root);
			return transform.TransformBounds(new System.Windows.Rect(0, 0, tvi.ActualWidth, tvi.ActualHeight));
		} catch {
			return System.Windows.Rect.Empty;
		}
	}

	private static System.Windows.Controls.TreeView? FindRootTreeView(System.Windows.DependencyObject node) {
		while (node is not null) {
			if (node is System.Windows.Controls.TreeView tree)
				return tree;
			node = System.Windows.Media.VisualTreeHelper.GetParent(node);
		}
		return null;
	}

	private static System.Windows.Rect CreateRect(System.Windows.Point a, System.Windows.Point b) {
		var x = Math.Min(a.X, b.X);
		var y = Math.Min(a.Y, b.Y);
		return new System.Windows.Rect(x, y, Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
	}

	private void Toggle(ITreeItem item) {
		ITreeItem? removed = null;
		foreach (var selectedItem in selected.ToList()) {
			if (!TreeItemIdentity.Same(selectedItem, item))
				continue;
			removed = selectedItem;
			selected.Remove(selectedItem);
			break;
		}
		if (removed is null)
			selected.Add(item);
		anchor = item;
		if (selected.Count == 0) {
			primary = null;
			SetPrimary(null, notifyEto: true);
		} else if (primary is null || !ContainsSelected(primary))
			SetPrimary(item, notifyEto: true);
		else
			ApplyVisuals();
	}

	private void SelectRange(ITreeItem from, ITreeItem to, bool additive = false) {
		if (etoTree.DataStore is not ITreeItem root)
			return;
		var visible = TreeNavigation.GetVisibleItems(root);
		var start = TreeNavigation.IndexOfVisible(visible, from);
		var end = TreeNavigation.IndexOfVisible(visible, to);
		if (start < 0 || end < 0)
			return;
		if (start > end)
			(start, end) = (end, start);

		if (!additive)
			selected.Clear();
		for (var i = start; i <= end; i++)
			selected.Add(visible[i]);
		anchor = from;
		SetPrimary(visible[end], notifyEto: true);
		ApplyVisuals();
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
			if (item is not null && ContainsSelected(item)) {
				tvi.Background = TreeItemIdentity.Same(item, primary) ? PrimaryBrush : SecondaryBrush;
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

	private sealed class TreeMarqueeAdorner : System.Windows.Documents.Adorner {
		private readonly System.Windows.Point startPoint;
		private System.Windows.Point endPoint;

		public TreeMarqueeAdorner(System.Windows.UIElement adornedElement, System.Windows.Point start)
			: base(adornedElement) {
			startPoint = start;
			endPoint = start;
		}

		public void Update(System.Windows.Point end) {
			endPoint = end;
			InvalidateVisual();
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext) {
			var rect = CreateRect(startPoint, endPoint);
			var fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 91, 141, 239));
			var pen = new System.Windows.Media.Pen(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(200, 91, 141, 239)), 1);
			drawingContext.DrawRectangle(fill, pen, rect);
		}
	}
#else
	public void OnMouseDown(ITreeItem? item, bool control, bool shift) {
		if (item is null)
			return;
		if (control && !shift) {
			Toggle(item);
			return;
		}
		if (shift && anchor is not null)
			SelectRange(anchor, item, additive: control);
		else
			SelectSingle(item);
	}

	private void Toggle(ITreeItem item) {
		ITreeItem? removed = null;
		foreach (var selectedItem in selected.ToList()) {
			if (!TreeItemIdentity.Same(selectedItem, item))
				continue;
			removed = selectedItem;
			selected.Remove(selectedItem);
			break;
		}
		if (removed is null)
			selected.Add(item);
		anchor = item;
		if (selected.Count == 0)
			SetPrimary(null, notifyEto: true);
		else
			SetPrimary(item, notifyEto: true);
	}

	private void SelectRange(ITreeItem from, ITreeItem to, bool additive = false) {
		if (etoTree.DataStore is not ITreeItem root)
			return;
		var visible = TreeNavigation.GetVisibleItems(root);
		var start = TreeNavigation.IndexOfVisible(visible, from);
		var end = TreeNavigation.IndexOfVisible(visible, to);
		if (start < 0 || end < 0)
			return;
		if (start > end)
			(start, end) = (end, start);
		if (!additive)
			selected.Clear();
		for (var i = start; i <= end; i++)
			selected.Add(visible[i]);
		anchor = from;
		SetPrimary(visible[end], notifyEto: true);
		ApplyVisuals();
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

file sealed class PathItemComparer : IEqualityComparer<ITreeItem> {
	public static readonly PathItemComparer Instance = new();

	public bool Equals(ITreeItem? x, ITreeItem? y) => TreeItemIdentity.Same(x, y);

	public int GetHashCode(ITreeItem obj) {
		var key = TreeItemIdentity.GetKey(obj);
		return key?.GetHashCode(StringComparison.Ordinal) ?? RuntimeHelpers.GetHashCode(obj);
	}
}
