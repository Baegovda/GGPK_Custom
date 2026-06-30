using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class TreeFilterBar : Panel {
	private static readonly (string Key, string Label)[] TypeFilters = [
		("", "All"),
		("Images", "Images"),
		("UvSequence", "UV Seq"),
		("Text", "Text"),
		("Data", "Data"),
		("Audio", "Audio"),
		("Video", "Video")
	];

	private readonly TextBox pathBox = new() { PlaceholderText = "Path or filename…" };
	private readonly TextBox excludeBox = new() { PlaceholderText = "Exclude words (comma or space)…" };
	private readonly Button resetButton = new() { Text = "Reset", Enabled = false, Width = 72 };
	private readonly Button exportPngButton = new() { Text = "Export PNGs", Enabled = false, Width = 102 };
	private readonly Label hintLabel = new();
	private readonly List<(string Key, Button Button)> typeChips = [];
	private string selectedTypeKey = "";
	private CancellationTokenSource? debounce;
	private bool suppressEvents;
	private string lastPersistedType = "\0";
	private string lastPersistedExclude = "\0";

	public event EventHandler? FiltersChanged;
	public event EventHandler? ExportFilteredPngsRequested;

	public TreeFilterBar() {
		foreach (var (key, label) in TypeFilters) {
			var chip = new Button {
				Text = label,
				Width = key == "UvSequence" ? 72 : 64,
				ToolTip = key.Length == 0 ? "Show all file types" :
					key == "UvSequence" ? "Show only UV sprite sequences (NxM in file or path name)" :
					$"Show only {label.ToLowerInvariant()} files"
			};
			var filterKey = key;
			chip.Click += (_, _) => SelectTypeFilter(filterKey);
			typeChips.Add((key, chip));
		}

		RestoreSavedFilters();

		pathBox.TextChanged += (_, _) => _ = DebouncedApplyAsync();
		excludeBox.TextChanged += (_, _) => _ = DebouncedApplyAsync();
		resetButton.Click += (_, _) => Reset();
		exportPngButton.Click += (_, _) => ExportFilteredPngsRequested?.Invoke(this, EventArgs.Empty);

		AppTheme.StyleTextInput(pathBox);
		AppTheme.StyleTextInput(excludeBox);
		AppTheme.StyleButton(exportPngButton);
		AppTheme.StyleButton(resetButton);
		AppTheme.StyleHintLabel(hintLabel);
		UpdateTypeChipStyles();

		var showLabel = AppTheme.CreateFieldLabel("Show");
		var hideLabel = AppTheme.CreateFieldLabel("Hide");
		var typeLabel = AppTheme.CreateFieldLabel("Type");

		var chipRowTop = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 4,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		var chipRowBottom = new StackLayout {
			Orientation = Orientation.Horizontal,
			Spacing = 4,
			VerticalContentAlignment = VerticalAlignment.Center
		};
		for (var i = 0; i < typeChips.Count; i++) {
			var target = i < 4 ? chipRowTop : chipRowBottom;
			target.Items.Add(typeChips[i].Button);
		}
		var chipColumn = new StackLayout {
			Orientation = Orientation.Vertical,
			Spacing = 4,
			Items = { chipRowTop, chipRowBottom }
		};

		var layout = new DynamicLayout {
			Padding = new Padding(10, 8),
			Spacing = new Size(8, 6)
		};
		layout.BeginHorizontal();
		layout.Add(showLabel, yscale: false);
		layout.Add(pathBox, xscale: true, yscale: false);
		layout.Add(exportPngButton, yscale: false);
		layout.Add(resetButton, yscale: false);
		layout.EndHorizontal();
		layout.BeginHorizontal();
		layout.Add(hideLabel, yscale: false);
		layout.Add(excludeBox, xscale: true, yscale: false);
		layout.EndHorizontal();
		layout.BeginHorizontal();
		layout.Add(typeLabel, yscale: false);
		layout.Add(chipColumn, xscale: true, yscale: false);
		layout.EndHorizontal();
		layout.AddRow(hintLabel);
		Content = layout;
		AppTheme.ApplyPanel(this, raised: true);
		UpdateHint();
	}

	public string SelectedTypeFilterKey => selectedTypeKey;

	public string SelectedExcludeFilterText => excludeBox.Text.Trim();

	public void ClearPath(bool notify = true) {
		suppressEvents = true;
		debounce?.Cancel();
		pathBox.Text = "";
		suppressEvents = false;
		TreeViewFilter.ClearRevealPath();
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
		selectedTypeKey = "";
		suppressEvents = false;
		UpdateTypeChipStyles();
		TreeViewFilter.ClearRevealPath();
		FileSearchFilter.Clear();
		FileExcludeFilter.Clear();
		FileFormatFilter.Clear();
		UpdateHint();
		PersistFilters();
		if (notify)
			FiltersChanged?.Invoke(this, EventArgs.Empty);
	}

	private void SelectTypeFilter(string key) {
		if (suppressEvents || selectedTypeKey == key)
			return;
		selectedTypeKey = key;
		UpdateTypeChipStyles();
		ApplyFilters();
	}

	private void UpdateTypeChipStyles() {
		foreach (var (key, chip) in typeChips)
			AppTheme.StyleButton(chip, key == selectedTypeKey ? ThemeButtonVariant.Primary : ThemeButtonVariant.Ghost);
	}

	private async Task DebouncedApplyAsync() {
		debounce?.Cancel();
		debounce = new CancellationTokenSource();
		var token = debounce.Token;
		try {
			await Task.Delay(100, token);
			if (!token.IsCancellationRequested)
				ApplyFilters();
		} catch (TaskCanceledException) {
		}
	}

	private void ApplyFilters() {
		if (suppressEvents)
			return;
		TreeViewFilter.ClearRevealPath();
		FileSearchFilter.Set(pathBox.Text);
		FileExcludeFilter.Set(excludeBox.Text);
		if (string.IsNullOrEmpty(selectedTypeKey))
			FileFormatFilter.Clear();
		else
			FileFormatFilter.SetPreset(selectedTypeKey);
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
			hintLabel.Text = "Trees show all files. Narrow by path, excluded words, and/or file type.";
			return;
		}
		var parts = new List<string>();
		if (hasPath)
			parts.Add($"path contains \"{FileSearchFilter.Text}\"");
		if (hasExclude)
			parts.Add($"excludes [{string.Join(", ", FileExcludeFilter.Terms)}]");
		if (hasType)
			parts.Add($"type: {GetSelectedTypeLabel()}");
		hintLabel.Text = "Active filter — " + string.Join(", ", parts);
	}

	private string GetSelectedTypeLabel() {
		foreach (var (key, button) in typeChips) {
			if (key == selectedTypeKey)
				return button.Text;
		}
		return selectedTypeKey;
	}

	private void RestoreSavedFilters() {
		var saved = LayoutSettingsStore.Load();
		suppressEvents = true;
		selectedTypeKey = IsValidTypeKey(saved.FilterType) ? saved.FilterType : "";
		excludeBox.Text = saved.FilterExclude;
		lastPersistedType = selectedTypeKey;
		lastPersistedExclude = saved.FilterExclude.Trim();
		suppressEvents = false;
		UpdateTypeChipStyles();
		SyncFilters();
		UpdateHint();
	}

	private void SyncFilters() {
		if (string.IsNullOrEmpty(selectedTypeKey))
			FileFormatFilter.Clear();
		else
			FileFormatFilter.SetPreset(selectedTypeKey);
		FileExcludeFilter.Set(excludeBox.Text);
	}

	private static bool IsValidTypeKey(string key) {
		return key is "Images" or "UvSequence" or "Text" or "Data" or "Audio" or "Video";
	}

	private void PersistFilters() {
		var exclude = excludeBox.Text.Trim();
		if (selectedTypeKey == lastPersistedType && exclude == lastPersistedExclude)
			return;
		lastPersistedType = selectedTypeKey;
		lastPersistedExclude = exclude;
		var layout = LayoutSettingsStore.Load();
		LayoutSettingsStore.Save(new LayoutSettingsStore.Layout(
			layout.MainSplitter,
			layout.InnerSplitter,
			layout.InfoAutoHide,
			selectedTypeKey,
			exclude));
	}
}
