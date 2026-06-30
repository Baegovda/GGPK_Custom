#if Windows
using System;
using System.Linq;

using Eto.Forms;

namespace VisualGGPK3;

internal static class FavoritesTreeDragDrop {
	private const string Format = "VisualGGPK3.FavoriteDrag";
	private static System.Windows.Point dragStart;
	private static bool dragging;
	private static FavoritesPanel? panel;

	public static void Enable(TreeView tree, FavoritesPanel favoritesPanel) {
		panel = favoritesPanel;
		var wpfTree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)tree.Handler).Control;
		wpfTree.AllowDrop = true;
		wpfTree.PreviewMouseLeftButtonDown += (_, e) => {
			dragStart = e.GetPosition(wpfTree);
			dragging = false;
		};
		wpfTree.PreviewMouseMove += (_, e) => {
			if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed || dragging)
				return;
			var pos = e.GetPosition(wpfTree);
			if (Math.Abs(pos.X - dragStart.X) < 4 && Math.Abs(pos.Y - dragStart.Y) < 4)
				return;
			var item = GetItemAt(wpfTree, dragStart);
			var payload = GetPayload(item);
			if (payload is null)
				return;
			dragging = true;
			System.Windows.DragDrop.DoDragDrop(wpfTree, new System.Windows.DataObject(Format, payload), System.Windows.DragDropEffects.Move);
			dragging = false;
		};
		wpfTree.DragOver += (_, e) => {
			if (!e.Data.GetDataPresent(Format)) {
				e.Effects = System.Windows.DragDropEffects.None;
				e.Handled = true;
				return;
			}
			var target = GetItemAt(wpfTree, e.GetPosition(wpfTree));
			var payload = e.Data.GetData(Format) as string;
			if (payload is null || !CanDrop(payload, target)) {
				e.Effects = System.Windows.DragDropEffects.None;
				e.Handled = true;
				return;
			}
			e.Effects = System.Windows.DragDropEffects.Move;
			e.Handled = true;
		};
		wpfTree.Drop += (_, e) => {
			if (!e.Data.GetDataPresent(Format))
				return;
			var payload = e.Data.GetData(Format) as string;
			if (payload is null)
				return;
			var target = GetItemAt(wpfTree, e.GetPosition(wpfTree));
			if (!CanDrop(payload, target))
				return;
			var groupId = GetDropGroupId(target);
			if (payload.StartsWith("entry:", StringComparison.Ordinal))
				panel?.MoveEntry(payload["entry:".Length..], groupId);
			else if (payload.StartsWith("group:", StringComparison.Ordinal))
				panel?.MoveGroup(payload["group:".Length..], groupId);
			e.Handled = true;
		};
	}

	private static string? GetPayload(ITreeItem? item) => item switch {
		FavoriteEntryTreeItem entry => $"entry:{entry.ArchivePath}",
		FavoriteGroupTreeItem group => $"group:{group.Id}",
		_ => null
	};

	private static bool CanDrop(string payload, ITreeItem? target) {
		if (payload.StartsWith("group:", StringComparison.Ordinal)) {
			var groupId = payload["group:".Length..];
			return target switch {
				null => true,
				FavoriteGroupTreeItem g => g.Id != groupId && !IsDescendant(groupId, g.Id),
				FavoriteEntryTreeItem e => e.GroupId != groupId,
				_ => true
			};
		}
		if (payload.StartsWith("entry:", StringComparison.Ordinal)) {
			var path = payload["entry:".Length..];
			return target switch {
				FavoriteEntryTreeItem e => !FavoritePaths.Equals(e.ArchivePath, path),
				_ => true
			};
		}
		return false;
	}

	private static bool IsDescendant(string ancestorId, string candidateId) {
		var data = FavoritesStore.LoadData();
		for (var id = candidateId; id is not null;) {
			if (id == ancestorId)
				return true;
			id = data.Groups.FirstOrDefault(g => g.Id == id)?.ParentId;
		}
		return false;
	}

	private static string? GetDropGroupId(ITreeItem? target) => target switch {
		FavoriteGroupTreeItem g => g.Id,
		FavoriteEntryTreeItem e => e.GroupId,
		_ => null
	};

	private static ITreeItem? GetItemAt(System.Windows.Controls.TreeView tree, System.Windows.Point pos) {
		var dep = tree.InputHitTest(pos) as System.Windows.DependencyObject;
		while (dep is not null && dep is not System.Windows.Controls.TreeViewItem)
			dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
		if (dep is not System.Windows.Controls.TreeViewItem tvi)
			return null;
		return tvi.DataContext as ITreeItem ?? tvi.Header as ITreeItem;
	}
}
#endif
