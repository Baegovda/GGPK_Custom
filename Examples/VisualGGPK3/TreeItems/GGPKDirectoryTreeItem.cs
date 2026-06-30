using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using Eto;
using Eto.Forms;

using LibGGPK3;
using LibGGPK3.Records;

using VisualGGPK3;

namespace VisualGGPK3.TreeItems;
[ContentProperty("ChildItems")]
public class GGPKDirectoryTreeItem : DirectoryTreeItem {
	public virtual DirectoryRecord Record { get; }
	public override GGPKDirectoryTreeItem? Parent { get; }
#pragma warning disable CS0618
	protected internal GGPKDirectoryTreeItem(DirectoryRecord record, GGPKDirectoryTreeItem? parent, TreeView tree) : base(record.Name, tree) {
		Record = record;
		Parent = parent;
	}

	protected internal ReadOnlyCollection<ITreeItem>? _ChildItems;
	protected internal int _filterVersion = -1;
	private int _visibleUnderFilterVersion = -1;
	private bool _visibleUnderFilter;
	private List<ITreeItem>? _allChildren;

	public override ReadOnlyCollection<ITreeItem> ChildItems {
		get {
			if (_ChildItems is not null && _filterVersion == TreeViewFilter.Version)
				return _ChildItems;
			_filterVersion = TreeViewFilter.Version;
			_ChildItems = BuildChildItems();
			return _ChildItems;
		}
	}

	private List<ITreeItem> GetAllChildren() {
		if (_allChildren is not null)
			return _allChildren;
		_allChildren = Record.OrderBy(tn => tn, TreeNode.NodeComparer.Instance).Select(
			t => t is FileRecord f ?
				(ITreeItem)new GGPKFileTreeItem(f, this) :
				new GGPKDirectoryTreeItem((DirectoryRecord)t, this, Tree)
		).ToList();
		return _allChildren;
	}

	private ReadOnlyCollection<ITreeItem> BuildChildItems() {
		var list = GetAllChildren();
		if (!TreeViewFilter.IsActive)
			return new(list);
		return new(list.Where(FilterItem).ToList());
	}

	private bool FilterItem(ITreeItem item) => item switch {
		FileTreeItem f => TreeViewFilter.MatchesFile(f),
		GGPKDirectoryTreeItem d => d.HasFilteredVisibleChild(),
		_ => true
	};

	internal bool HasFilteredVisibleChild() {
		if (!TreeViewFilter.IsActive)
			return GetAllChildren().Count > 0;
		if (_visibleUnderFilterVersion == TreeViewFilter.Version)
			return _visibleUnderFilter;
		var visible = false;
		foreach (var child in GetAllChildren()) {
			if (FilterItem(child)) {
				visible = true;
				break;
			}
		}
		_visibleUnderFilterVersion = TreeViewFilter.Version;
		_visibleUnderFilter = visible;
		return visible;
	}

	internal bool HasMatchingDescendant() => HasFilteredVisibleChild();

	internal FileTreeItem? FindFileByPath(string path) {
		path = FavoritePaths.Normalize(path);
		if (!Record.TryFindNode(path, out var node) || node is not FileRecord)
			return null;
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (segments.Length == 0)
			return null;
		var dir = this;
		for (var i = 0; i < segments.Length - 1; i++) {
			var childDir = dir.FindChildDirectory(segments[i]);
			if (childDir is null)
				return null;
			childDir.Expanded = true;
			dir = childDir;
		}
		dir.Expanded = true;
		return dir.FindChildFile(segments[^1]);
	}

	internal DirectoryTreeItem? FindDirectoryByPath(string path) {
		path = FavoritePaths.DirectoryLookupPath(path);
		if (string.IsNullOrEmpty(path))
			return this;
		var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
		var dir = this;
		foreach (var segment in segments) {
			var childDir = dir.FindChildDirectory(segment);
			if (childDir is null)
				return null;
			childDir.Expanded = true;
			dir = childDir;
		}
		dir.Expanded = true;
		return dir;
	}

	internal GGPKDirectoryTreeItem? FindChildDirectory(string name) {
		if (!Initialized)
			Expanded = true;
		if (!Initialized)
			return null;
		foreach (var item in GetAllChildren()) {
			if (item is GGPKDirectoryTreeItem dir && string.Equals(dir.Name, name, StringComparison.OrdinalIgnoreCase))
				return dir;
		}
		return null;
	}

	internal FileTreeItem? FindChildFile(string name) {
		if (!Initialized)
			return null;
		foreach (var item in GetAllChildren()) {
			if (item is GGPKFileTreeItem file && string.Equals(file.Name, name, StringComparison.OrdinalIgnoreCase))
				return file;
		}
		return null;
	}

	protected internal override IEnumerable<ITreeItem> EnumerateAllChildren() => GetAllChildren();

	public override bool Expandable => !Initialized ? HasFilteredVisibleChild() : Count > 0;

	protected internal override void InvalidateFilterCache() {
		_ChildItems = null;
		_filterVersion = -1;
		_visibleUnderFilterVersion = -1;
	}

	public override int Extract(string path) {
		return GGPK.Extract(Record, path); // TODO: Progress
	}

	public override int Extract(Action<string, ReadOnlyMemory<byte>> callback, string endsWith = "") {
		endsWith = endsWith.ToLowerInvariant();
		var count = 0;
		foreach (var (fr, path) in TreeNode.RecurseFiles(Record).AsParallel()) {
			if (!path.EndsWith(endsWith, StringComparison.Ordinal))
				continue;
			callback(path, fr.Read());
			++count;
		}
		return count;
	}

	public override int Replace(string path) {
		return GGPK.Replace(Record, path); // TODO: Progress
	}

	public override string GetPath() => Record.GetPath();
}