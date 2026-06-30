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
		GGPKDirectoryTreeItem d => d.HasMatchingDescendant(),
		_ => true
	};

	internal bool HasMatchingDescendant() => HasMatchingInRecord(Record);

	internal FileTreeItem? FindFileByPath(string path) {
		if (!Initialized)
			Expanded = true;
		if (!Initialized)
			return null;
		foreach (var item in GetAllChildren()) {
			if (item is GGPKFileTreeItem file && FavoritePaths.Equals(file.GetPath(), path))
				return file;
			if (item is GGPKDirectoryTreeItem dir) {
				var found = dir.FindFileByPath(path);
				if (found is not null)
					return found;
			}
		}
		return null;
	}

	private static bool HasMatchingInRecord(DirectoryRecord record) {
		foreach (var tn in record) {
			if (tn is FileRecord fr && TreeViewFilter.MatchesPath(fr.GetPath()))
				return true;
			if (tn is DirectoryRecord dr && HasMatchingInRecord(dr))
				return true;
		}
		return false;
	}

	protected internal override IEnumerable<ITreeItem> EnumerateAllChildren() => GetAllChildren();

	protected internal override void InvalidateFilterCache() {
		_ChildItems = null;
		_filterVersion = -1;
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