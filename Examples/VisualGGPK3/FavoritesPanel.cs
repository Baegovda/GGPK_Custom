using System;

using System.Collections.Generic;

using System.Linq;



using Eto;

using Eto.Drawing;

using Eto.Forms;



namespace VisualGGPK3;



internal sealed class FavoritesPanel : Panel {

	private readonly TreeView tree = new();

	private readonly Label emptyHint = new() {

		Text = "Right-click a file or folder in the tree\nand choose Add to favorites.\n\nUse + Folder to organize.",

		TextColor = Colors.Gray,

		Wrap = WrapMode.Word,

		VerticalAlignment = VerticalAlignment.Center,

		TextAlignment = TextAlignment.Center

	};

	private readonly Button newFolderButton = new() { Text = "+ Folder", ToolTip = "Create a favorites folder" };
	private readonly ImageView headerIcon = new() {
		Image = TreeItemIcons.FavoriteStar,
		Size = new Size(18, 18)
	};
	private readonly Label headerLabel = new() {
		Text = "Favorites",
		Font = SystemFonts.Bold(),
		VerticalAlignment = VerticalAlignment.Center
	};

	private bool suppressEvents;

	private bool skipNextSelectionNavigate;

	private HashSet<string> expandedGroupIds = [];



	public event EventHandler<string>? FavoriteSelected;

	public event EventHandler<string>? FavoriteRemoveRequested;



	private readonly Panel listHost = new();



	public FavoritesPanel() {

		tree.SelectionChanged += (_, _) => OnSelectionChanged();

		tree.KeyDown += OnTreeKeyDown;

		tree.MouseUp += OnTreeMouseUp;
#if Windows
		HookFavoriteClick(tree);
#else
		tree.MouseDown += OnFavoriteMouseDown;
#endif

#if Windows

		FavoritesTreeDragDrop.Enable(tree, this);

#endif



		newFolderButton.Click += (_, _) => CreateFolder(GetSelectedParentGroupId());



		var header = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			VerticalContentAlignment = VerticalAlignment.Center,
			Items = {
				headerIcon,
				new StackLayoutItem(headerLabel, expand: true),
				newFolderButton
			}
		};



		Content = new TableLayout {

			Spacing = new Size(4, 4),

			Padding = new Padding(4),

			Rows = {

				new TableRow(header) { ScaleHeight = false },

				new TableRow(listHost) { ScaleHeight = true }

			}

		};



		listHost.Content = tree;

		RefreshList();

	}

	public void ApplyTheme() {
		AppTheme.ApplyPanel(this);
		AppTheme.ApplyTreeHost(tree);
		AppTheme.StyleHeaderLabel(headerLabel);
		AppTheme.StyleButton(newFolderButton, ThemeButtonVariant.Ghost);
		emptyHint.TextColor = AppTheme.TextMuted;
#if Windows
		WpfDarkTheme.StyleTreeView(tree);
#endif
	}



	public string? GetAddTargetGroupId() {

		return tree.SelectedItem switch {

			FavoriteGroupTreeItem g => g.Id,

			FavoriteEntryTreeItem e => e.GroupId,

			_ => null

		};

	}



	internal void MoveEntry(string path, string? groupId) {

		FavoritesStore.MoveEntry(path, groupId);

		RefreshList();

	}



	internal void MoveGroup(string groupId, string? parentId) {

		FavoritesStore.MoveGroup(groupId, parentId);

		RefreshList();

	}



	public void RefreshList() {

		suppressEvents = true;

		CollectExpandedGroups();

		var data = FavoritesStore.LoadData();

		var roots = FavoriteTreeBuilder.Build(data);

		var hasItems = data.Groups.Count > 0 || data.Entries.Count > 0;

		listHost.Content = hasItems ? tree : emptyHint;

		if (hasItems) {
			var root = new FavoriteListRoot(roots);
			tree.DataStore = root;
			RestoreExpanded(roots);
		}

		suppressEvents = false;

	}



	private void CollectExpandedGroups() {

		if (tree.DataStore is FavoriteListRoot root)
			expandedGroupIds = CollectExpanded(root.ChildItems);

	}



	private static HashSet<string> CollectExpanded(IEnumerable<ITreeItem> items) {

		var set = new HashSet<string>();

		foreach (var item in items) {

			if (item is FavoriteGroupTreeItem g) {

				if (g.Expanded)

					set.Add(g.Id);

				foreach (var id in CollectExpanded(g.ChildItems))

					set.Add(id);

			}

		}

		return set;

	}



	private void RestoreExpanded(IEnumerable<ITreeItem> items) {

		foreach (var item in items) {

			if (item is FavoriteGroupTreeItem g) {

				g.Expanded = expandedGroupIds.Contains(g.Id) || g.Expanded;

				RestoreExpanded(g.ChildItems);

			}

		}

	}



	private void OnSelectionChanged() {

		if (suppressEvents)

			return;

		if (skipNextSelectionNavigate) {

			skipNextSelectionNavigate = false;

			return;

		}

		if (tree.SelectedItem is not FavoriteEntryTreeItem entry)

			return;

		ActivateFavorite(entry);

	}



	private void OnFavoriteMouseDown(object? sender, MouseEventArgs e) {

		if (e.Buttons != MouseButtons.Primary)

			return;

		var item = tree.GetNodeAt(e.Location);

		if (item is FavoriteEntryTreeItem entry)

			ActivateFavorite(entry, fromMouse: true);

	}



#if Windows
	private void HookFavoriteClick(TreeView tree) {

		var wpfTree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)tree.Handler).Control;

		wpfTree.PreviewMouseLeftButtonDown += (_, e) => {

			var pos = e.GetPosition(wpfTree);

			var item = tree.GetNodeAt(new PointF((float)pos.X, (float)pos.Y));

			if (item is FavoriteEntryTreeItem entry)

				ActivateFavorite(entry, fromMouse: true);

		};

	}
#endif



	private void ActivateFavorite(FavoriteEntryTreeItem entry, bool fromMouse = false) {

		if (suppressEvents)

			return;

		if (fromMouse)

			skipNextSelectionNavigate = true;

		suppressEvents = true;

		try {

			tree.SelectedItem = entry;

		} finally {

			suppressEvents = false;

		}

		FavoriteSelected?.Invoke(this, entry.ArchivePath);

	}



	private void OnTreeKeyDown(object? sender, KeyEventArgs e) {

		if (e.Key != Keys.Delete)

			return;

		if (tree.SelectedItem is FavoriteEntryTreeItem entry) {

			e.Handled = true;

			FavoriteRemoveRequested?.Invoke(this, entry.ArchivePath);

			return;

		}

		if (tree.SelectedItem is FavoriteGroupTreeItem group) {

			e.Handled = true;

			DeleteGroup(group);

		}

	}



	private void OnTreeMouseUp(object? sender, MouseEventArgs e) {

		if (e.Buttons != MouseButtons.Alternate)

			return;

		var loc = Point.Round(e.Location);
		var item = tree.GetNodeAt(loc);
		if (item is FavoriteEntryTreeItem entry)
			ShowEntryMenu(entry, loc);
		else if (item is FavoriteGroupTreeItem group)
			ShowGroupMenu(group, loc);
		else
			ShowRootMenu(loc);

	}



	private void ShowRootMenu(Point location) {

		var menu = new ContextMenu(

			new ButtonMenuItem((_, _) => CreateFolder(null)) { Text = "New folder" }

		);

		menu.Show(tree, location);

	}



	private void ShowGroupMenu(FavoriteGroupTreeItem group, Point location) {

		var menu = new ContextMenu(

			new ButtonMenuItem((_, _) => CreateFolder(group.Id)) { Text = "New subfolder" },

			new ButtonMenuItem((_, _) => RenameGroup(group)) { Text = "Rename folder" },

			new ButtonMenuItem((_, _) => DeleteGroup(group)) { Text = "Delete folder" }

		);

		menu.Show(tree, location);

	}



	private void ShowEntryMenu(FavoriteEntryTreeItem entry, Point location) {

		var items = new List<MenuItem> {

			new ButtonMenuItem((_, _) => FavoriteSelected?.Invoke(this, entry.ArchivePath)) {

				Text = entry.IsArchiveDirectory ? "Go to folder" : "Go to file"

			},

			new ButtonMenuItem((_, _) => Clipboard.Instance.Text = entry.ArchivePath) { Text = "Copy path" },

			new ButtonMenuItem((_, _) => FavoriteRemoveRequested?.Invoke(this, entry.ArchivePath)) { Text = "Remove from favorites" }

		};

		var moveMenu = BuildMoveToMenu(entry.ArchivePath, entry.GroupId);

		if (moveMenu.Items.Count > 0)

			items.Add(moveMenu);

		new ContextMenu(items).Show(tree, location);

	}



	private ButtonMenuItem BuildMoveToMenu(string path, string? currentGroupId) {

		var menu = new ButtonMenuItem { Text = "Move to" };

		if (currentGroupId is not null)

			menu.Items.Add(new ButtonMenuItem((_, _) => MoveEntry(path, null)) { Text = "(root)" });

		foreach (var (id, label) in EnumerateGroupMenuPaths(FavoritesStore.LoadData(), null, "")) {

			if (id == currentGroupId)

				continue;

			var groupId = id;

			menu.Items.Add(new ButtonMenuItem((_, _) => MoveEntry(path, groupId)) { Text = label });

		}

		return menu;

	}



	private static IEnumerable<(string Id, string Label)> EnumerateGroupMenuPaths(FavoritesData data, string? parentId, string prefix) {

		foreach (var group in data.Groups.Where(g => g.ParentId == parentId).OrderBy(g => g.Order)) {

			var label = string.IsNullOrEmpty(prefix) ? group.Name : $"{prefix}/{group.Name}";

			yield return (group.Id, label);

			foreach (var child in EnumerateGroupMenuPaths(data, group.Id, label))

				yield return child;

		}

	}



	private string? GetSelectedParentGroupId() => tree.SelectedItem switch {

		FavoriteGroupTreeItem g => g.Id,

		FavoriteEntryTreeItem e => e.GroupId,

		_ => null

	};



	private void CreateFolder(string? parentId) {

		var owner = ParentWindow ?? Application.Instance.MainForm;

		var dlg = new PromptDialog("New folder", "Folder name:", "New folder");

		if (dlg.ShowModal(owner) != DialogResult.Ok)

			return;

		var name = dlg.Value.Trim();

		if (string.IsNullOrEmpty(name))

			name = "New folder";

		FavoritesStore.CreateGroup(name, parentId);

		RefreshList();

	}



	private void RenameGroup(FavoriteGroupTreeItem group) {

		var owner = ParentWindow ?? Application.Instance.MainForm;

		var dlg = new PromptDialog("Rename folder", "Folder name:", group.Text);

		if (dlg.ShowModal(owner) != DialogResult.Ok)

			return;

		var name = dlg.Value.Trim();

		if (string.IsNullOrEmpty(name))

			return;

		FavoritesStore.RenameGroup(group.Id, name);

		RefreshList();

	}



	private void DeleteGroup(FavoriteGroupTreeItem group) {

		FavoritesStore.DeleteGroup(group.Id);

		RefreshList();

	}

}


