using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using Eto;
using Eto.Forms;

using LibBundle3.Nodes;

using Index = LibBundle3.Index;

using VisualGGPK3;

namespace VisualGGPK3.TreeItems;
[ContentProperty("ChildItems")]
public class BundleDirectoryTreeItem : DirectoryTreeItem, IDirectoryNode {
	public override BundleDirectoryTreeItem? Parent { get; }
	IDirectoryNode? ITreeNode.Parent => Parent;
#pragma warning disable CS0618
	protected internal BundleDirectoryTreeItem(string name, BundleDirectoryTreeItem? parent, TreeView tree) : base(name, tree) {
		Parent = parent;
	}

	public virtual List<ITreeNode> Children { get; } = [];

	protected internal IReadOnlyList<ITreeItem>? _ChildItems;
	protected internal int _filterVersion = -1;

	public override IReadOnlyList<ITreeItem> ChildItems {
		get {
			if (_ChildItems is not null && _filterVersion == TreeViewFilter.Version)
				return _ChildItems;
			_filterVersion = TreeViewFilter.Version;
			_ChildItems = BuildChildItems();
			return _ChildItems;
		}
	}

	private IReadOnlyList<ITreeItem> BuildChildItems() {
		SortChildren();
		var items = new List<ITreeItem>(Children.Count);
		foreach (var node in Children)
			items.Add((ITreeItem)node);
		if (!TreeViewFilter.IsActive)
			return items;
		return items.Where(FilterItem).ToList();
	}

	private void SortChildren() {
		var tmp = new ITreeNode[Children.Count];
		int j = 0, k = 0;
		for (var i = 0; i < Children.Count; ++i) {
			if (Children[i] is IDirectoryNode)
				Children[j++] = Children[i];
			else
				tmp[k++] = Children[i];
		}
		tmp.AsSpan()[..k].CopyTo(CollectionsMarshal.AsSpan(Children)[j..]);
	}

	private bool FilterItem(ITreeItem item) => item switch {
		FileTreeItem f => TreeViewFilter.MatchesFile(f),
		BundleDirectoryTreeItem d => d.HasMatchingDescendant(),
		_ => true
	};

	internal bool HasMatchingDescendant() {
		foreach (var child in Children) {
			if (child is BundleFileTreeItem f && TreeViewFilter.MatchesFile(f))
				return true;
			if (child is BundleDirectoryTreeItem d && d.HasMatchingDescendant())
				return true;
		}
		return false;
	}

	internal FileTreeItem? FindFileByPath(string path) {
		if (!Initialized)
			Expanded = true;
		if (!Initialized)
			return null;
		foreach (var node in Children) {
			if (node is BundleFileTreeItem file && FavoritePaths.Equals(file.GetPath(), path))
				return file;
			if (node is BundleDirectoryTreeItem dir) {
				var found = dir.FindFileByPath(path);
				if (found is not null)
					return found;
			}
		}
		return null;
	}

	protected internal override IEnumerable<ITreeItem> EnumerateAllChildren() {
		for (var i = 0; i < Children.Count; ++i)
			yield return (ITreeItem)Children[i];
	}

	protected internal override void InvalidateFilterCache() {
		_ChildItems = null;
		_filterVersion = -1;
	}

	public override int Extract(string path) { // TODO: Progress
		return Index.ExtractParallel(this, path);
	}

	public override int Extract(Action<string, ReadOnlyMemory<byte>> callback, string endsWith = "") { // TODO: Progress
		endsWith = endsWith.ToLowerInvariant();
		var basePath = GetPath().Length;
		return Index.ExtractParallel(Index.Recursefiles(this).Select(fn => fn.Record)
			.Where(fr => fr.Path.EndsWith(endsWith, StringComparison.Ordinal)), (fr, data) => {
			if (data.HasValue)
				callback(fr.Path[basePath..], data.Value);
			return false;
		});
	}

	public override int Replace(string path) { // TODO: Progress
		return Index.Replace(this, path);
	}

	public override string GetPath() => ITreeNode.GetPath(this);

	protected internal static Index.CreateDirectoryInstance GetFuncCreateInstance(TreeView tree) {
		IDirectoryNode CreateInstance(string name, IDirectoryNode? parent) {
			return new BundleDirectoryTreeItem(name, parent as BundleDirectoryTreeItem, tree);
		}
		return CreateInstance;
	}
}