using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Eto;
using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class FavoritesPanel : Panel {
	private readonly GridView grid = new();
	private readonly Label emptyHint = new() {
		Text = "Right-click a file in the tree\nand choose Add to favorites.",
		TextColor = Colors.Gray,
		Wrap = WrapMode.Word,
		VerticalAlignment = VerticalAlignment.Center,
		TextAlignment = TextAlignment.Center
	};
	private List<FavoriteListEntry> entries = [];
	private bool suppressEvents;

	public event EventHandler<string>? FavoriteSelected;
	public event EventHandler<string>? FavoriteRemoveRequested;

	private readonly Panel listHost = new();

	public FavoritesPanel() {
		grid.AllowMultipleSelection = false;
		grid.ShowHeader = true;
		grid.Columns.Add(new GridColumn {
			HeaderText = "",
			DataCell = new ImageViewCell { Binding = Binding.Property<FavoriteListEntry, Image>(e => e.Icon) },
			Width = 28,
			Editable = false,
			Sortable = false
		});
		grid.Columns.Add(new GridColumn {
			HeaderText = "Name",
			DataCell = new TextBoxCell { Binding = Binding.Property<FavoriteListEntry, string>(e => e.FileName) },
			Width = 120,
			Editable = false,
			Sortable = true
		});
		grid.Columns.Add(new GridColumn {
			HeaderText = "Folder",
			DataCell = new TextBoxCell { Binding = Binding.Property<FavoriteListEntry, string>(e => e.Folder) },
			Editable = false,
			Sortable = true,
			Expand = true
		});

		grid.SelectionChanged += (_, _) => OnSelectionChanged();
		grid.KeyDown += OnGridKeyDown;
		grid.MouseUp += OnGridMouseUp;

		var header = new Label {
			Text = "Favorites",
			Font = SystemFonts.Bold(),
			VerticalAlignment = VerticalAlignment.Center
		};

		Content = new TableLayout {
			Spacing = new Size(4, 4),
			Padding = new Padding(4),
			Rows = {
				new TableRow(header) { ScaleHeight = false },
				new TableRow(listHost) { ScaleHeight = true }
			}
		};

		listHost.Content = grid;
		RefreshList();
	}

	public void RefreshList() {
		suppressEvents = true;
		entries = FavoriteFilesStore.Load().Select(FavoriteListEntry.FromPath).ToList();
		grid.DataStore = entries;
		listHost.Content = entries.Count > 0 ? grid : emptyHint;
		suppressEvents = false;
	}

	private void OnSelectionChanged() {
		if (suppressEvents)
			return;
		if (grid.SelectedItem is not FavoriteListEntry entry)
			return;
		FavoriteSelected?.Invoke(this, entry.FilePath);
	}

	private void OnGridKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key != Keys.Delete)
			return;
		if (grid.SelectedItem is not FavoriteListEntry entry)
			return;
		e.Handled = true;
		FavoriteRemoveRequested?.Invoke(this, entry.FilePath);
	}

	private void OnGridMouseUp(object? sender, MouseEventArgs e) {
		if (e.Buttons != MouseButtons.Alternate)
			return;
		if (grid.SelectedItem is not FavoriteListEntry entry)
			return;

		var menu = new ContextMenu(
			new ButtonMenuItem((_, _) => FavoriteSelected?.Invoke(this, entry.FilePath)) { Text = "Go to file" },
			new ButtonMenuItem((_, _) => {
				Clipboard.Instance.Text = entry.FilePath;
			}) { Text = "Copy path" },
			new ButtonMenuItem((_, _) => FavoriteRemoveRequested?.Invoke(this, entry.FilePath)) { Text = "Remove from favorites" }
		);
		menu.Show(grid, e.Location);
	}

	private sealed class FavoriteListEntry {
		public required string FilePath { get; init; }
		public required string FileName { get; init; }
		public required string Folder { get; init; }
		public Image Icon => TreeItemIcons.File;

		public static FavoriteListEntry FromPath(string path) {
			var normalized = path.Replace('\\', '/');
			var fileName = System.IO.Path.GetFileName(normalized);
			var lastSlash = normalized.LastIndexOf('/');
			var folder = lastSlash > 0 ? normalized[..lastSlash] : "";
			return new FavoriteListEntry {
				FilePath = normalized,
				FileName = string.IsNullOrEmpty(fileName) ? normalized : fileName,
				Folder = folder
			};
		}
	}
}
