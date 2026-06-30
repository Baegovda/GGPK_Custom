using System;
using System.Threading;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class TreeFilterBar : Panel {
	private readonly TextBox pathBox = new() { PlaceholderText = "Path or filename…" };
	private readonly TextBox excludeBox = new() { PlaceholderText = "Exclude words (comma or space)…" };
	private readonly DropDown typeDropDown = new() { Width = 110 };
	private readonly Button resetButton = new() { Text = "Reset", Enabled = false };
	private readonly Button exportPngButton = new() { Text = "Export PNGs", Enabled = false };
	private readonly Label hintLabel = new() { TextColor = Colors.Gray };
	private CancellationTokenSource? debounce;
	private bool suppressEvents;

	public event EventHandler? FiltersChanged;
	public event EventHandler? ExportFilteredPngsRequested;

	public TreeFilterBar() {
		typeDropDown.Items.Add(new ListItem { Text = "All types", Key = "" });
		typeDropDown.Items.Add(new ListItem { Text = "Images", Key = "Images" });
		typeDropDown.Items.Add(new ListItem { Text = "Text", Key = "Text" });
		typeDropDown.Items.Add(new ListItem { Text = "Data", Key = "Data" });
		typeDropDown.Items.Add(new ListItem { Text = "Audio", Key = "Audio" });
		typeDropDown.Items.Add(new ListItem { Text = "Video", Key = "Video" });

		RestoreSavedFilters();

		pathBox.TextChanged += (_, _) => _ = DebouncedApplyAsync();
		excludeBox.TextChanged += (_, _) => _ = DebouncedApplyAsync();
		typeDropDown.SelectedKeyChanged += (_, _) => ApplyFilters();
		resetButton.Click += (_, _) => Reset();
		exportPngButton.Click += (_, _) => ExportFilteredPngsRequested?.Invoke(this, EventArgs.Empty);

		var layout = new DynamicLayout { Padding = new Padding(5, 0, 5, 5), Spacing = new Size(6, 4) };
		layout.BeginHorizontal();
		layout.Add(new Label { Text = "Show", VerticalAlignment = VerticalAlignment.Center });
		layout.Add(pathBox, xscale: true);
		layout.Add(new Label { Text = "·", VerticalAlignment = VerticalAlignment.Center });
		layout.Add(typeDropDown);
		layout.Add(exportPngButton);
		layout.Add(resetButton);
		layout.EndHorizontal();
		layout.BeginHorizontal();
		layout.Add(new Label { Text = "Hide", VerticalAlignment = VerticalAlignment.Center });
		layout.Add(excludeBox, xscale: true);
		layout.EndHorizontal();
		layout.AddRow(hintLabel);
		Content = layout;
		UpdateHint();
	}

	public string SelectedTypeFilterKey => typeDropDown.SelectedKey ?? "";

	public string SelectedExcludeFilterText => excludeBox.Text.Trim();

	public void ClearPath(bool notify = true) {
		suppressEvents = true;
		debounce?.Cancel();
		pathBox.Text = "";
		suppressEvents = false;
		FileSearchFilter.Clear();
		UpdateHint();
		if (notify)
			FiltersChanged?.Invoke(this, EventArgs.Empty);
	}

	public void Reset(bool notify = true) {
		suppressEvents = true;
		debounce?.Cancel();
		pathBox.Text = "";
		excludeBox.Text = "";
		typeDropDown.SelectedKey = "";
		suppressEvents = false;
		FileSearchFilter.Clear();
		FileExcludeFilter.Clear();
		FileFormatFilter.Clear();
		UpdateHint();
		PersistFilters();
		if (notify)
			FiltersChanged?.Invoke(this, EventArgs.Empty);
	}

	private async Task DebouncedApplyAsync() {
		debounce?.Cancel();
		debounce = new CancellationTokenSource();
		var token = debounce.Token;
		try {
			await Task.Delay(300, token);
			if (!token.IsCancellationRequested)
				ApplyFilters();
		} catch (TaskCanceledException) {
		}
	}

	private void ApplyFilters() {
		if (suppressEvents)
			return;
		FileSearchFilter.Set(pathBox.Text);
		FileExcludeFilter.Set(excludeBox.Text);
		var typeKey = typeDropDown.SelectedKey ?? "";
		if (string.IsNullOrEmpty(typeKey))
			FileFormatFilter.Clear();
		else
			FileFormatFilter.SetPreset(typeKey);
		UpdateHint();
		PersistFilters();
		FiltersChanged?.Invoke(this, EventArgs.Empty);
	}

	private void UpdateHint() {
		var hasPath = FileSearchFilter.IsActive;
		var hasExclude = FileExcludeFilter.IsActive;
		var hasType = FileFormatFilter.IsActive;
		resetButton.Enabled = hasPath || hasExclude || hasType;
		exportPngButton.Enabled = hasPath || hasExclude || hasType;
		if (!hasPath && !hasExclude && !hasType) {
			hintLabel.Text = "Trees show all files. Narrow by path text, excluded words, and/or file type.";
			return;
		}
		var parts = new System.Collections.Generic.List<string>();
		if (hasPath)
			parts.Add($"path contains \"{FileSearchFilter.Text}\"");
		if (hasExclude)
			parts.Add($"excludes [{string.Join(", ", FileExcludeFilter.Terms)}]");
		if (hasType)
			parts.Add($"type: {GetSelectedTypeLabel()}");
		hintLabel.Text = "Active filter — " + string.Join(", ", parts);
	}

	private string GetSelectedTypeLabel() {
		var key = typeDropDown.SelectedKey ?? "";
		foreach (ListItem item in typeDropDown.Items) {
			if (item.Key == key)
				return item.Text;
		}
		return key;
	}

	private void RestoreSavedFilters() {
		var saved = LayoutSettingsStore.Load();
		suppressEvents = true;
		typeDropDown.SelectedKey = IsValidTypeKey(saved.FilterType) ? saved.FilterType : "";
		excludeBox.Text = saved.FilterExclude;
		suppressEvents = false;
		SyncFilters();
		UpdateHint();
	}

	private void SyncFilters() {
		var typeKey = typeDropDown.SelectedKey ?? "";
		if (string.IsNullOrEmpty(typeKey))
			FileFormatFilter.Clear();
		else
			FileFormatFilter.SetPreset(typeKey);
		FileExcludeFilter.Set(excludeBox.Text);
	}

	private static bool IsValidTypeKey(string key) {
		return key is "Images" or "Text" or "Data" or "Audio" or "Video";
	}

	private void PersistFilters() {
		var layout = LayoutSettingsStore.Load();
		LayoutSettingsStore.Save(new LayoutSettingsStore.Layout(
			layout.MainSplitter,
			layout.InnerSplitter,
			layout.InfoAutoHide,
			typeDropDown.SelectedKey ?? "",
			excludeBox.Text.Trim()));
	}
}
