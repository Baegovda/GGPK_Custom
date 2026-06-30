using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class FavoriteListRoot : ITreeItem {
	public IReadOnlyList<ITreeItem> ChildItems { get; }
	public string Text { get => ""; set { } }
	public Image? Image => null;
	public bool Expanded { get; set; } = true;
	public bool Expandable => ChildItems.Count > 0;
	public bool Initialized { get; set; } = true;
	string IListItem.Key => "root";

	ITreeItem ITreeItem<ITreeItem>.Parent {
		get => null!;
		set => throw new InvalidOperationException();
	}

	ITreeItem IDataStore<ITreeItem>.this[int index] => ChildItems[index];
	int IDataStore<ITreeItem>.Count => ChildItems.Count;

	public FavoriteListRoot(IReadOnlyList<ITreeItem> children) => ChildItems = children;
}

internal sealed class FavoriteGroupTreeItem : ITreeItem {
	public string Id { get; }
	public string Text { get; set; }
	public Image Image => TreeItemIcons.Directory;
	public FavoriteGroupTreeItem? Parent { get; }
	public IReadOnlyList<ITreeItem> ChildItems { get; private set; }
	public bool Expanded { get; set; } = true;
	public bool Expandable => ChildItems.Count > 0;
	public bool Initialized { get; set; } = true;
	public int Order { get; }
	string IListItem.Key => Id;

	ITreeItem ITreeItem<ITreeItem>.Parent {
		get => Parent!;
		set => throw new InvalidOperationException();
	}

	ITreeItem IDataStore<ITreeItem>.this[int index] => ChildItems[index];
	int IDataStore<ITreeItem>.Count => ChildItems.Count;

	public FavoriteGroupTreeItem(FavoriteGroup group, FavoriteGroupTreeItem? parent, IReadOnlyList<ITreeItem> children) {
		Id = group.Id;
		Text = group.Name;
		Parent = parent;
		ChildItems = children;
		Order = group.Order;
	}

	public void SetChildren(IReadOnlyList<ITreeItem> children) => ChildItems = children;
}

internal sealed class FavoriteEntryTreeItem : ITreeItem {
	public string ArchivePath { get; }
	public string Text { get; private init; }
	string IListItem.Text { get => Text; set => throw new InvalidOperationException(); }
	public Image Image => FavoritePaths.IsDirectory(ArchivePath) ? TreeItemIcons.Directory : TreeItemIcons.File;
	public string? GroupId { get; }
	public FavoriteGroupTreeItem? Parent { get; }
	public IReadOnlyList<ITreeItem> ChildItems { get; } = [];
	public bool Expanded { get; set; }
	public bool Expandable => false;
	public bool Initialized { get; set; } = true;
	public bool IsArchiveDirectory => FavoritePaths.IsDirectory(ArchivePath);
	public int Order { get; }
	string IListItem.Key => ArchivePath;

	ITreeItem ITreeItem<ITreeItem>.Parent {
		get => Parent!;
		set => throw new InvalidOperationException();
	}

	ITreeItem IDataStore<ITreeItem>.this[int index] => throw new InvalidOperationException();
	int IDataStore<ITreeItem>.Count => 0;

	public FavoriteEntryTreeItem(FavoriteEntry entry, FavoriteGroupTreeItem? parent) {
		ArchivePath = FavoritePaths.Normalize(entry.Path);
		GroupId = entry.GroupId;
		Parent = parent;
		Order = entry.Order;
		if (IsArchiveDirectory) {
			var trimmed = ArchivePath.TrimEnd('/');
			var name = Path.GetFileName(trimmed);
			Text = string.IsNullOrEmpty(name) ? trimmed : name;
		} else {
			var name = Path.GetFileName(ArchivePath);
			Text = string.IsNullOrEmpty(name) ? ArchivePath : name;
		}
	}
}

internal static class FavoriteTreeBuilder {
	public static IReadOnlyList<ITreeItem> Build(FavoritesData data) {
		return BuildChildren(data, parentGroupId: null, parentItem: null);
	}

	private static List<ITreeItem> BuildChildren(FavoritesData data, string? parentGroupId, FavoriteGroupTreeItem? parentItem) {
		var items = new List<(int Order, ITreeItem Item)>();
		foreach (var group in data.Groups.Where(g => g.ParentId == parentGroupId).OrderBy(g => g.Order)) {
			var node = new FavoriteGroupTreeItem(group, parentItem, []);
			var children = BuildChildren(data, group.Id, node);
			node.SetChildren(children);
			items.Add((group.Order, node));
		}
		foreach (var entry in data.Entries.Where(e => e.GroupId == parentGroupId).OrderBy(e => e.Order)) {
			items.Add((entry.Order, new FavoriteEntryTreeItem(entry, parentItem)));
		}
		return items.OrderBy(t => t.Order).Select(t => t.Item).ToList();
	}
}
