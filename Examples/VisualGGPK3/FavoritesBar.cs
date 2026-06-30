using System;
using System.IO;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class FavoritesBar : Panel {
	private readonly DropDown dropDown = new() { Width = 200 };
	private bool suppressEvents;

	public event EventHandler<string>? FavoriteSelected;

	public FavoritesBar() {
		dropDown.SelectedKeyChanged += (_, _) => OnFavoriteSelected();
		RefreshList();

		Content = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 6,
			Items = {
				new Label { Text = "Favorites", VerticalAlignment = VerticalAlignment.Center },
				dropDown
			}
		};
	}

	public void RefreshList() {
		suppressEvents = true;
		dropDown.Items.Clear();
		dropDown.Items.Add(new ListItem { Text = "(select a favorite)", Key = "" });
		foreach (var path in FavoriteFilesStore.Load())
			dropDown.Items.Add(new ListItem { Text = FormatLabel(path), Key = path });
		dropDown.SelectedKey = "";
		suppressEvents = false;
	}

	private void OnFavoriteSelected() {
		if (suppressEvents)
			return;
		var key = dropDown.SelectedKey ?? "";
		if (string.IsNullOrEmpty(key))
			return;
		FavoriteSelected?.Invoke(this, key);
		suppressEvents = true;
		dropDown.SelectedKey = "";
		suppressEvents = false;
	}

	private static string FormatLabel(string path) {
		var normalized = path.Replace('\\', '/');
		var fileName = Path.GetFileName(normalized);
		var lastSlash = normalized.LastIndexOf('/');
		if (lastSlash <= 0)
			return string.IsNullOrEmpty(fileName) ? path : fileName;
		var parentPath = normalized[..lastSlash];
		var parentSlash = parentPath.LastIndexOf('/');
		var folder = parentSlash >= 0 ? parentPath[(parentSlash + 1)..] : parentPath;
		if (string.IsNullOrEmpty(folder))
			return fileName;
		return $"{fileName}  —  {folder}";
	}
}
