using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Eto.Drawing;
using Eto.Forms;

using LibBundledGGPK3;
using LibGGPK3;

using SystemExtensions;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;
public sealed class MainWindow : Form {
	private GGPK? Ggpk;
	internal LibBundle3.Index? Index;
#pragma warning disable CS0618
	private readonly TreeView GGPKTree = new();
	private readonly TreeView BundleTree = new();
#pragma warning restore CS0618
	private readonly TextArea TextPanel = new() { ReadOnly = true, Text = "This program hasn't been completed yet" };
	private readonly MediaPreviewPanel PreviewPanel = new();
	private readonly GridView DatPanel = new();
	private readonly TextBox PathBox = new() { ReadOnly = true, PlaceholderText = "No file opened" };
	private readonly TreeFilterBar FilterBar = new();
	private readonly FavoritesPanel FavoritesPanel = new();
	private readonly Splitter MainLayout;
	private readonly Splitter RightLayout;
	private readonly Splitter TreesLayout;
	private int preferredMainSplitter;
	private int preferredInnerSplitter;
	private int preferredFavoritesSplitter;

	private string? imageName;
	private ITreeItem? clickedItem;
	private bool firstSelected;
	private readonly Button updateButton;
	private Version? pendingUpdateVersion;

	public MainWindow(string? path = null) {
#if Mac
		static void closed(object? sender, EventArgs e) => Application.Instance.Quit();
		Closed += closed;
		Application.Instance.Terminating += (s, e) => Closed -= closed;
#endif
		var version = Assembly.GetExecutingAssembly().GetName().Version!;
		Title = $"VisualGGPK3 (v{version.ToString(version.Revision == 0 ? 3 : 4)})";

		if (Screen is null) {
			Size = new(640, 480);
		} else {
			var bounds = Screen.Bounds;
			if (bounds.Width <= 1280 || bounds.Height <= 720)
				Size = new(960, 540);
			else
				Size = new(1280, 720);
		}
#if Windows
#pragma warning disable CS0618 // Obsolete
		static void WindowsFix(TreeView tree) {
			var etree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)tree.Handler).Control; // EtoTreeView
#pragma warning restore CS0618
			// Virtualizing
			etree.SetValue(System.Windows.Controls.VirtualizingStackPanel.IsVirtualizingProperty, true);
			etree.SetValue(System.Windows.Controls.VirtualizingStackPanel.VirtualizationModeProperty, System.Windows.Controls.VirtualizationMode.Standard);
			// Fix expand binding
			var setter = (System.Windows.Setter)etree.ItemContainerStyle.Setters[0];
			((System.Windows.Data.Binding)setter.Value).Mode = System.Windows.Data.BindingMode.TwoWay; // From OneTime
		}
		WindowsFix(GGPKTree);
		WindowsFix(BundleTree);
		WpfDarkTheme.StyleTreeView(GGPKTree);
		WpfDarkTheme.StyleTreeView(BundleTree);
		WindowsHookTreeKeys(GGPKTree);
		WindowsHookTreeKeys(BundleTree);
		// Virtualizing
		var gtext = ((Eto.Wpf.Forms.Controls.GridViewHandler)DatPanel.Handler).Control; // EtoDataGrid
		gtext.SetValue(System.Windows.Controls.VirtualizingStackPanel.IsVirtualizingProperty, true);
		gtext.SetValue(System.Windows.Controls.VirtualizingStackPanel.VirtualizationModeProperty, System.Windows.Controls.VirtualizationMode.Recycling);
		WpfDarkTheme.StyleGridView(DatPanel);
#endif
		TreeMultiSelection.Enable(GGPKTree);
		TreeMultiSelection.Enable(BundleTree);
		GGPKTree.SelectionChanged += OnSelectionChanged;
		BundleTree.SelectionChanged += OnSelectionChanged;

		var layout = LayoutSettingsStore.Load();
		preferredMainSplitter = layout.MainSplitter;
		preferredInnerSplitter = layout.InnerSplitter;
		preferredFavoritesSplitter = layout.FavoritesSplitter;

		RightLayout = new Splitter {
			Panel1 = BundleTree,
			Panel1MinimumSize = 10,
			Panel2 = TextPanel,
			Panel2MinimumSize = 10,
			SplitterWidth = 4,
			Position = preferredInnerSplitter
		};
		RightLayout.PositionChangeCompleted += (_, _) => {
			if (RightLayout.Position > 0)
				preferredInnerSplitter = RightLayout.Position;
		};

		MainLayout = new Splitter() {
			Panel1 = GGPKTree,
			Panel1MinimumSize = 10,
			Panel2 = RightLayout,
			Panel2MinimumSize = 20,
			SplitterWidth = 4,
			Position = preferredMainSplitter
		};
		MainLayout.PositionChangeCompleted += (_, _) => {
			if (MainLayout.Position > 0)
				preferredMainSplitter = MainLayout.Position;
		};

		TreesLayout = new Splitter {
			Panel1 = FavoritesPanel,
			Panel1MinimumSize = 120,
			Panel2 = MainLayout,
			Panel2MinimumSize = 200,
			SplitterWidth = 4,
			Position = preferredFavoritesSplitter
		};
		TreesLayout.PositionChangeCompleted += (_, _) => {
			if (TreesLayout.Position > 0)
				preferredFavoritesSplitter = TreesLayout.Position;
		};

		AppTheme.ApplyForm(this);
#if Windows
		Load += (_, _) => AppTheme.ApplyForm(this);
#endif
		AppTheme.StyleTextArea(TextPanel);
		AppTheme.StyleSplitter(MainLayout);
		AppTheme.StyleSplitter(RightLayout);
		AppTheme.StyleSplitter(TreesLayout);
		AppTheme.ApplyTreeHost(GGPKTree);
		AppTheme.ApplyTreeHost(BundleTree);
		FavoritesPanel.ApplyTheme();

		var loading = new TreeItemCollection() {
			new TreeItem() { Text = "Loading . . ." }
		};
		GGPKTree.DataStore = loading;
		BundleTree.DataStore = loading;

		var openButton = new Button { Text = "Open", Width = AppTheme.ToolbarButtonWidth };
		openButton.Click += OnOpenClicked;
		AppTheme.StyleButton(openButton, ThemeButtonVariant.Primary);
		updateButton = new Button { Text = "Update", Width = AppTheme.ToolbarButtonWidth };
		updateButton.Click += OnUpdateClicked;
		AppTheme.StyleButton(updateButton, ThemeButtonVariant.Secondary);
		AppTheme.StyleTextInput(PathBox, readOnly: true);
		FilterBar.FiltersChanged += (_, _) => {
			DiagnosticLog.User("filter_changed", new Dictionary<string, object?> {
				["show"] = FileSearchFilter.Text,
				["hide"] = FileExcludeFilter.Text,
				["type"] = FilterBar.SelectedTypeFilterKey
			});
			DiagnosticLog.Measure("filter", "apply_trees", ApplyTreeFilters);
		};
		FilterBar.ExportFilteredPngsRequested += (_, _) => _ = ExportFilteredImagesAsPngAsync();
		FavoritesPanel.FavoriteSelected += (_, path) => NavigateToFavorite(path);
		FavoritesPanel.FavoriteRemoveRequested += (_, path) => RemoveFavorite(path);

		var topBar = new DynamicLayout {
			Padding = new Padding(10, 8, 10, 6),
			Spacing = new Size(0, 8)
		};
		topBar.BeginHorizontal();
		topBar.Add(openButton, yscale: false);
		topBar.Add(updateButton, yscale: false);
		topBar.Add(PathBox, xscale: true, yscale: false);
		topBar.EndHorizontal();
		topBar.Add(FilterBar, xscale: true, yscale: false);
		AppTheme.ApplyPanel(FilterBar, raised: true);

		var root = new DynamicLayout {
			Spacing = Size.Empty,
			BackgroundColor = AppTheme.WindowBg
		};
		root.Add(topBar, xscale: true, yscale: false);
		root.Add(TreesLayout, xscale: true, yscale: true);
		Content = root;

		GGPKTree.MouseUp += (s, e) => {
			if (e.Buttons == MouseButtons.Alternate && GGPKTree.GetNodeAt(e.Location) is ITreeItem item)
				ShowTreeContextMenu(GGPKTree, item);
		};
		BundleTree.MouseUp += (s, e) => {
			if (e.Buttons == MouseButtons.Alternate && BundleTree.GetNodeAt(e.Location) is ITreeItem item)
				ShowTreeContextMenu(BundleTree, item);
		};
#if !Windows
		GGPKTree.MouseDown += OnTreeMouseDown;
		BundleTree.MouseDown += OnTreeMouseDown;
#endif
		var menu2 = new ContextMenu(new ButtonMenuItem(OnSaveAsPngClicked) { Text = "Save as png" });
		PreviewPanel.ImageView.MouseUp += (s, e) => {
			if (e.Buttons == MouseButtons.Alternate)
				menu2.Show(PreviewPanel.ImageView, e.Location);
		};

		GGPKTree.DragEnter += OnDragEnter;
		GGPKTree.DragDrop += OnDragDrop;
		GGPKTree.AllowDrop = true;
		BundleTree.DragEnter += OnDragEnter;
		BundleTree.DragDrop += OnDragDrop;
		BundleTree.AllowDrop = true;

		GGPKTree.KeyDown += OnTreeKeyDown;
		BundleTree.KeyDown += OnTreeKeyDown;
		PreviewPanel.KeyDown += OnPlayKeyDown;

		LoadComplete += OnLoadComplete;
		Closed += (_, _) => {
			PreviewPanel.Clear();
			LayoutSettingsStore.Save(new LayoutSettingsStore.Layout(
				preferredMainSplitter,
				preferredInnerSplitter,
				PreviewPanel.InfoAutoHideEnabled,
				FilterBar.SelectedTypeFilterKey,
				FilterBar.SelectedExcludeFilterText,
				preferredFavoritesSplitter));
			DiagnosticLog.LogSessionEnd("window_closed");
		};

		_ = CheckForUpdatesInBackgroundAsync(version);

		async void OnLoadComplete(object? sender, EventArgs e) {
			LoadComplete -= OnLoadComplete;
			DiagnosticLog.StartUiWatchdog();
			DiagnosticLog.User("app_ready", new Dictionary<string, object?> {
				["log_path"] = DiagnosticLog.LogFilePath
			});
			await Task.Yield();
			if (path is null || !File.Exists(path)) {
				var recent = RecentFileStore.Load();
				if (recent is not null && File.Exists(recent))
					path = recent;
			}
			if (path is null || !File.Exists(path)) {
				using var ofd = CreateOpenFileDialog();
				if (ofd.ShowDialog(this) != DialogResult.Ok) {
					Close();
					return;
				}
				path = ofd.FileName;
			}
			await LoadFileAsync(path);
		}
	}

	private async Task CheckForUpdatesInBackgroundAsync(Version currentVersion) {
		try {
			var info = await AppUpdater.CheckAsync().ConfigureAwait(false);
			if (info is not UpdateInfo resolved)
				return;

			if (resolved.Minimum is Version minimum && currentVersion < minimum) {
				await Application.Instance.InvokeAsync(() => {
					if (MessageBox.Show(this,
							$"Critical update required (minimum v{AppUpdater.FormatVersion(minimum)}).\nInstall the latest version now?",
							MessageBoxButtons.YesNo,
							MessageBoxType.Warning) == DialogResult.Yes)
						_ = RunUpdateAsync(resolved, prompt: false);
					else {
						Close();
						Application.Instance.Quit();
					}
				});
				return;
			}

			if (AppUpdater.IsNewer(resolved.Latest)) {
				pendingUpdateVersion = resolved.Latest;
				await Application.Instance.InvokeAsync(RefreshUpdateButton);
			}
		} catch (Exception ex) {
			Debug.WriteLine(ex.GetNameAndMessage());
		}
	}

	private void RefreshUpdateButton() {
		if (pendingUpdateVersion is Version latest) {
			updateButton.Text = $"Update v{AppUpdater.FormatVersion(latest)}";
			AppTheme.StyleButton(updateButton, ThemeButtonVariant.Primary);
		} else {
			updateButton.Text = "Update";
			AppTheme.StyleButton(updateButton, ThemeButtonVariant.Secondary);
		}
	}

	private async void OnUpdateClicked(object? sender, EventArgs e) {
		updateButton.Enabled = false;
		try {
			UpdateInfo info;
			try {
				if (await AppUpdater.CheckAsync().ConfigureAwait(true) is not UpdateInfo resolved) {
					MessageBox.Show(this, "Could not check for updates.", MessageBoxButtons.OK, MessageBoxType.Error);
					return;
				}
				info = resolved;
			} catch (Exception ex) {
				MessageBox.Show(this, $"Could not check for updates.\n{ex.Message}", MessageBoxButtons.OK, MessageBoxType.Error);
				return;
			}

			if (!AppUpdater.IsNewer(info.Latest)) {
				MessageBox.Show(this, $"You already have the latest version (v{AppUpdater.FormatVersion(AppUpdater.CurrentVersion)}).", MessageBoxButtons.OK, MessageBoxType.Information);
				pendingUpdateVersion = null;
				RefreshUpdateButton();
				return;
			}

			pendingUpdateVersion = info.Latest;
			RefreshUpdateButton();
			await RunUpdateAsync(info, prompt: true);
		} finally {
			updateButton.Enabled = true;
		}
	}

	private Task RunUpdateAsync(UpdateInfo info, bool prompt) {
		if (prompt) {
			if (string.IsNullOrEmpty(info.DownloadUrl)) {
				if (MessageBox.Show(this,
						$"Version v{AppUpdater.FormatVersion(info.Latest)} is available, but no install package was published yet.\nOpen the releases page in your browser?",
						MessageBoxButtons.YesNo,
						MessageBoxType.Question) != DialogResult.Yes)
					return Task.CompletedTask;
			} else if (MessageBox.Show(this,
					$"Download and install VisualGGPK3 v{AppUpdater.FormatVersion(info.Latest)}?\nThe app will close and restart automatically.",
					MessageBoxButtons.OKCancel,
					MessageBoxType.Question) != DialogResult.Ok)
				return Task.CompletedTask;
		}

		if (string.IsNullOrEmpty(info.DownloadUrl)) {
			Process.Start(new ProcessStartInfo(AppUpdater.ReleasesUrl) { UseShellExecute = true });
			return Task.CompletedTask;
		}

		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			MessageBox.Show(this, "Automatic install is only supported on Windows. Opening the releases page.", MessageBoxButtons.OK, MessageBoxType.Information);
			Process.Start(new ProcessStartInfo(AppUpdater.ReleasesUrl) { UseShellExecute = true });
			return Task.CompletedTask;
		}

		using var progressDialog = new UpdateProgressDialog();
		using var cts = new CancellationTokenSource();
		progressDialog.CancelRequested += () => cts.Cancel();
		var success = false;

		progressDialog.Shown += async (_, _) => {
			try {
				await AppUpdater.DownloadAndApplyAsync(
					info,
					new Progress<UpdateProgress>(progressDialog.Report),
					cts.Token).ConfigureAwait(true);
				success = true;
				progressDialog.SetComplete("Update ready. Restarting…");
				await Task.Delay(600).ConfigureAwait(true);
				progressDialog.Result = DialogResult.Ok;
				progressDialog.Close();
			} catch (OperationCanceledException) {
				progressDialog.SetError("Update cancelled.");
			} catch (Exception ex) {
				progressDialog.SetError(ex.Message);
			}
		};

		progressDialog.ShowModal(this);
		if (success) {
			Close();
			Application.Instance.Quit();
		}
		return Task.CompletedTask;
	}

	private static OpenFileDialog CreateOpenFileDialog() => new() {
		FileName = "Content.ggpk",
		Filters = {
			new("GGPK/Index File", ".ggpk", ".bin"),
			allFilesFilters
		}
	};

	private async void OnOpenClicked(object? sender, EventArgs e) {
		using var ofd = CreateOpenFileDialog();
		if (ofd.ShowDialog(this) != DialogResult.Ok)
			return;
		await LoadFileAsync(ofd.FileName);
	}

	private async Task LoadFileAsync(string path) {
		try {
			await DiagnosticLog.MeasureAsync("archive", "load", async () => {
			if (!File.Exists(path))
				throw new FileNotFoundException(path);

			DiagnosticLog.User("open_archive", new Dictionary<string, object?> { ["path"] = path });

			Application.Instance.Invoke(() => BeginArchiveLoadUi(path));

			ArchiveLoadResult loadResult;
			if (path.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
				loadResult = await Task.Run(() => LoadBinArchive(path));
			else
				loadResult = await Task.Run(() => LoadGgpkArchive(path));

			BundleDirectoryTreeItem? bundleRoot = null;
			if (loadResult.Index is not null && loadResult.Failed != loadResult.Index.Files.Count) {
				var index = loadResult.Index;
				bundleRoot = await Task.Run(() => (BundleDirectoryTreeItem)index.BuildTree(
					BundleDirectoryTreeItem.GetFuncCreateInstance(BundleTree),
					BundleFileTreeItem.CreateInstance,
					true));
			}

			var bundles = bundleRoot;
			Application.Instance.Invoke(() => FinishArchiveLoadUi(path, loadResult, bundles));
			}, new Dictionary<string, object?> { ["path"] = path });
		} catch (Exception ex) {
			Application.Instance.Invoke(() => FinishArchiveLoadUi(path, CreateArchiveLoadError(path, ex), null));
		}
	}

	private void BeginArchiveLoadUi(string path) {
		PathBox.Text = Path.GetFullPath(path);
		PreviewPanel.Clear();
		FilterBar.ClearPath(notify: false);

		if (Ggpk is not null) {
			Ggpk.Dispose();
			Ggpk = null;
			Index = null;
		} else if (Index is not null) {
			Index.Dispose();
			Index = null;
		}

		GGPKTree.Visible = true;
		GGPKTree.Enabled = true;
		MainLayout.Panel1MinimumSize = 10;
		if (MainLayout.Position == 0)
			MainLayout.Position = preferredMainSplitter;

		var loading = new TreeItemCollection() {
			new TreeItem() { Text = "Loading . . ." }
		};
		GGPKTree.DataStore = loading;
		BundleTree.DataStore = loading;
		ShowLoadStatus("Loading…");
	}

	private sealed class ArchiveLoadResult {
		public int Failed;
		public GGPK? Ggpk;
		public LibBundle3.Index? Index;
		public bool BinOnly;
		public string? DialogTitle;
		public string? DialogMessage;
		public MessageBoxType DialogType;
	}

	private static ArchiveLoadResult LoadBinArchive(string path) {
		try {
			var index = new LibBundle3.Index(path, false);
			return new ArchiveLoadResult {
				Failed = index.ParsePaths(),
				Index = index,
				BinOnly = true
			};
		} catch (Exception ex) {
			return CreateArchiveLoadError(path, ex);
		}
	}

	private ArchiveLoadResult LoadGgpkArchive(string path) {
		if (PoeGameDetector.IsClientRunning() && PoeGameDetector.IsGameArchivePath(path)) {
			try {
				using var probe = GGPK.OpenFileStream(path);
			} catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
				return CreateArchiveLoadError(path, ex);
			}
		}

		try {
			var ggpk = new BundledGGPK(path, false);
			return new ArchiveLoadResult {
				Failed = ggpk.Index.ParsePaths(),
				Ggpk = ggpk,
				Index = ggpk.Index
			};
		} catch (Exception ex) when (ex is DllNotFoundException { Message: var dllMsg }
			&& dllMsg.Contains("oo2core", StringComparison.OrdinalIgnoreCase)) {
			try {
				var ggpk = new GGPK(path);
				return new ArchiveLoadResult {
					Failed = 0,
					Ggpk = ggpk,
					DialogTitle = "oo2core 없음",
					DialogMessage =
						"Bundled GGPK (PoE2 등)을 읽으려면 oo2core.dll 이 필요합니다.\r\n\r\n" +
						"1. 저장소 루트에서 setup.ps1 실행\r\n" +
						"2. Run-VisualGGPK3.cmd 로 실행 (bin\\Debug\\)\r\n\r\n" +
						$"상세: {dllMsg}",
					DialogType = MessageBoxType.Error
				};
			} catch (Exception fallback) {
				return CreateArchiveLoadError(path, fallback);
			}
		} catch (Exception ex) {
			return CreateArchiveLoadError(path, ex);
		}
	}

	private static ArchiveLoadResult CreateArchiveLoadError(string path, Exception ex) {
		if (ex is FileNotFoundException or DirectoryNotFoundException) {
			return new ArchiveLoadResult {
				DialogTitle = "Warning",
				DialogMessage = ex.GetNameAndMessage(),
				DialogType = MessageBoxType.Warning
			};
		}
		if (ex is IOException or UnauthorizedAccessException) {
			var gameLikely = PoeGameDetector.IsClientRunning() && PoeGameDetector.IsGameArchivePath(path);
			return new ArchiveLoadResult {
				DialogTitle = gameLikely ? "게임 실행 중 — GGPK를 열 수 없음" : "파일을 열 수 없음",
				DialogMessage = gameLikely
					? PoeGameDetector.BuildGameLockWarning(path)
					: "다른 프로그램이 파일을 사용 중입니다.\n\n" + Path.GetFullPath(path),
				DialogType = MessageBoxType.Warning
			};
		}
		return new ArchiveLoadResult {
			DialogTitle = "Error",
			DialogMessage = ex.Message,
			DialogType = MessageBoxType.Error
		};
	}

	private void FinishArchiveLoadUi(string path, ArchiveLoadResult loadResult, BundleDirectoryTreeItem? bundles) {
		if (loadResult.DialogMessage is not null)
			MessageBox.Show(this, loadResult.DialogMessage, loadResult.DialogTitle ?? "Error", loadResult.DialogType);

		if (loadResult.Ggpk is null && loadResult.Index is null) {
			var empty = new TreeItemCollection {
				new TreeItem() { Text = "No file opened" }
			};
			GGPKTree.DataStore = empty;
			BundleTree.DataStore = empty;
			ShowLoadStatus(loadResult.DialogMessage ?? "Failed to open archive.");
			return;
		}

		Ggpk = loadResult.Ggpk;
		Index = loadResult.Index;

		if (loadResult.BinOnly) {
			GGPKTree.DataStore = null;
			GGPKTree.Visible = false;
			GGPKTree.Enabled = false;
			MainLayout.Panel1MinimumSize = 0;
			MainLayout.Position = 0;
		} else {
			GGPKTree.DataStore = new GGPKDirectoryTreeItem(Ggpk!.Root, null, GGPKTree) {
				Expanded = true
			};
		}

		if (bundles is not null)
			bundles.Expanded = true;
		BundleTree.DataStore = bundles;

		string? extraNote = null;
		if (loadResult.BinOnly)
			extraNote = "GGPK tree hidden (opened _.index.bin only).";
		else if (loadResult.Failed != 0 && Index is not null)
			extraNote = $"Warning: {loadResult.Failed} directory path(s) could not be matched. See details below.";
		ShowLoadStatus(LoadStatusReport.Build(PathBox.Text, Index, loadResult.Failed, extraNote));
		ApplyTreeFilters();
		RecentFileStore.Save(path);
	}

	private void ShowLoadStatus(string text) {
		PreviewPanel.ShowStatus(text);
		RightLayout.Panel2 = PreviewPanel;
	}

	private void ShowTreeContextMenu(TreeView tree, ITreeItem item) {
		clickedItem = item;
		var items = new List<MenuItem> {
			new ButtonMenuItem(OnExtractClicked) { Text = "Extract" },
			new ButtonMenuItem(OnReplaceClicked) { Text = "Replace" },
			new ButtonMenuItem(OnCopyPathClicked) { Text = "Copy Path" },
			new ButtonMenuItem(OnExportDdsClicked) { Text = "Export .dds to .png" }
		};
		if (item is FileTreeItem fileItem) {
			AddFavoriteMenuItem(items, fileItem.GetPath());
		} else if (item is DirectoryTreeItem dirItem) {
			AddFavoriteMenuItem(items, dirItem.GetPath());
		}
		new ContextMenu(items).Show(tree);
	}

	private void AddFavoriteMenuItem(List<MenuItem> items, string path) {
		items.Add(new ButtonMenuItem(OnToggleFavoriteClicked) {
			Text = FavoritesStore.Contains(path) ? "Remove from favorites" : "Add to favorites"
		});
	}

	private void OnToggleFavoriteClicked(object? sender, EventArgs _) {
		if (clickedItem is DirectoryTreeItem dirItem) {
			ToggleFavoritePath(dirItem.GetPath());
			return;
		}
		if (clickedItem is not FileTreeItem)
			return;
		var tree = TreeForItem(clickedItem);
		var files = GetContextFileItems(tree).ToList();
		if (files.Count == 0)
			return;
		foreach (var fileItem in files)
			ToggleFavoritePath(fileItem.GetPath(), refresh: false);
		FavoritesPanel.RefreshList();
	}

	private void ToggleFavoritePath(string path, bool refresh = true) {
		if (FavoritesStore.Contains(path))
			FavoritesStore.Remove(path);
		else
			FavoritesStore.Add(path, FavoritesPanel.GetAddTargetGroupId());
		if (refresh)
			FavoritesPanel.RefreshList();
	}

	private void RemoveFavorite(string path) {
		FavoritesStore.Remove(path);
		FavoritesPanel.RefreshList();
	}

	private void NavigateToFavorite(string path) {
		if (Index is null && Ggpk is null) {
			MessageBox.Show(this, "Open a GGPK or index file first.", "Favorites", MessageBoxType.Information);
			return;
		}
		TreeViewFilter.SetRevealPath(path);
		ApplyTreeFilters();
		Application.Instance.AsyncInvoke(() => SelectFavoriteInTree(path));
	}

	private void SelectFavoriteInTree(string path) {
		var ggpkRoot = GGPKTree.DataStore as GGPKDirectoryTreeItem;
		var bundleRoot = BundleTree.DataStore as BundleDirectoryTreeItem;
		var item = DiagnosticLog.Measure("favorite", "navigate", () =>
			FavoriteFileLocator.Find(path, Index, Ggpk, ggpkRoot, bundleRoot));
		if (item is null) {
			TreeViewFilter.ClearRevealPath();
			ApplyTreeFilters();
			MessageBox.Show(this, $"Path not found in the current archive:\r\n{path}", "Favorites", MessageBoxType.Warning);
			return;
		}
		var tree = TreeForItem(item);
		FavoriteFileLocator.ExpandTo(item);
		tree.SelectedItem = item;
		TreeMultiSelection.Get(tree)?.SelectSingle(item);
		OnSelectionChanged(tree, EventArgs.Empty);
	}

#if !Windows
	private void OnTreeMouseDown(object? sender, MouseEventArgs e) {
		if (e.Buttons != MouseButtons.Primary || sender is not TreeView tree)
			return;
		var item = tree.GetNodeAt(e.Location);
		var ms = TreeMultiSelection.Get(tree);
		ms?.OnMouseDown(item, e.Modifiers.HasFlag(Keys.Control), e.Modifiers.HasFlag(Keys.Shift));
	}
#endif

	private IEnumerable<FileTreeItem> GetSelectedFileItems(TreeView tree) {
		var ms = TreeMultiSelection.Get(tree);
		if (ms is not null) {
			var files = ms.Selected.OfType<FileTreeItem>().ToList();
			if (files.Count > 0)
				return files;
		}
#pragma warning disable CS0618
		if (tree.SelectedItem is FileTreeItem single)
			return [single];
#pragma warning restore CS0618
		return [];
	}

	private IEnumerable<FileTreeItem> GetContextFileItems(TreeView tree) {
		var selected = GetSelectedFileItems(tree).ToList();
		if (clickedItem is FileTreeItem clicked && selected.Any(f => TreeItemIdentity.Same(f, clicked)))
			return selected;
		if (clickedItem is FileTreeItem one)
			return [one];
		return [];
	}

	private TreeView TreeForItem(ITreeItem item) =>
		item is GGPKFileTreeItem or GGPKDirectoryTreeItem ? GGPKTree : BundleTree;

	private void OnTreeKeyDown(object? sender, KeyEventArgs e) {
		OnPlayKeyDown(sender, e);
		if (e.Handled)
			return;
		OnNavigateKeyDown(sender, e);
	}

	private void OnNavigateKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key != Keys.Right)
			return;
		if (sender is not TreeView tree)
			return;
		if (TryHandleRightArrow(tree))
			e.Handled = true;
	}

	private bool TryHandleRightArrow(TreeView tree) {
#pragma warning disable CS0618
		if (tree.SelectedItem is not ITreeItem selected)
			return false;
#pragma warning restore CS0618
		if (tree.DataStore is not ITreeItem root)
			return false;

		if (selected is DirectoryTreeItem dir && !dir.Expanded && dir.Expandable) {
			dir.Expanded = true;
			return true;
		}

		var next = selected is FileTreeItem
			? TreeNavigation.GetNextFileItem(root, selected)
			: TreeNavigation.GetNextVisibleItem(root, selected);
		if (next is null)
			return false;
		SelectTreeItem(tree, next);
		return true;
	}

	private void SelectTreeItem(TreeView tree, ITreeItem item) {
		if (TreeMultiSelection.Get(tree) is { } ms)
			ms.SelectSingle(item);
#pragma warning disable CS0618
		tree.SelectedItem = item;
#pragma warning restore CS0618
		OnSelectionChanged(tree, EventArgs.Empty);
	}

	private bool TrySelectNextVisibleItem(TreeView tree) => TryHandleRightArrow(tree);

	private void OnPlayKeyDown(object? sender, KeyEventArgs e) {
		if (e.Key != Keys.Space)
			return;
		if (sender is TextBox or TextArea or DropDown)
			return;
		if (TryPlaySelectedAudio() || TryPlaySelectedVideo() || PreviewPanel.ToggleSpritePlayback())
			e.Handled = true;
	}

	private bool TryPlaySelectedAudio() {
		var file = GetSelectedFileItem();
		if (file is null || file.Format != FileTreeItem.DataFormat.Sound)
			return false;
		return PreviewPanel.ToggleAudioPlayback();
	}

	private bool TryPlaySelectedVideo() {
		var file = GetSelectedFileItem();
		if (file is null || file.Format != FileTreeItem.DataFormat.Video)
			return false;
		return PreviewPanel.ToggleVideoPlayback();
	}

	private FileTreeItem? GetSelectedFileItem() {
#pragma warning disable CS0618
		if (GGPKTree.SelectedItem is FileTreeItem ggpkFile)
			return ggpkFile;
		if (BundleTree.SelectedItem is FileTreeItem bundleFile)
			return bundleFile;
#pragma warning restore CS0618
		return null;
	}

	private void ApplyTreeFilters() {
		TreeRefresh.ApplyFilterChange(GGPKTree);
		TreeRefresh.ApplyFilterChange(BundleTree);
	}

	private void OnSelectionChanged(object? sender, EventArgs _) {
#pragma warning disable CS0618 // Obsolete
		var item = (sender as TreeView)?.SelectedItem;
#pragma warning restore CS0618
		if (item is null)
			return;

		if (item is FileTreeItem fileItem) {
			firstSelected = true;
			PreviewPanel.Clear();

			var panel = RightLayout;
			try {
				DiagnosticLog.Measure("preview", fileItem.Format.ToString(), () => {
				switch (fileItem.Format) {
					case FileTreeItem.DataFormat.Text:
						var span = fileItem.Read().Span;
#if Windows
						if (span.Length > 204800) {
							MessageBox.Show(this, "This text file is too large, only the first 100KB will be shown", "Warning", MessageBoxButtons.OK, MessageBoxType.Warning);
							span = span[..102400];
						}
#endif
						if (span.IsEmpty)
							TextPanel.Text = "";
						else if (MemoryMarshal.GetReference(span) == 0xFF)
							TextPanel.Text = new string(MemoryMarshal.Cast<byte, char>(span[2..]));
						else if (fileItem.Name.EndsWith(".amd", StringComparison.OrdinalIgnoreCase))
							TextPanel.Text = new string(MemoryMarshal.Cast<byte, char>(span));
						else
							unsafe {
								fixed (byte* p = span)
									TextPanel.Text = new string((sbyte*)p, 0, span.Length);
							}
						panel.Panel2 = TextPanel;
						break;
					case FileTreeItem.DataFormat.Image:
						imageName = Path.GetFileNameWithoutExtension(fileItem.Name);
						ReadOnlyMemory<byte> imageData;
						if (fileItem is GGPKFileTreeItem g)
							imageData = g.Record.Read();
						else
							imageData = fileItem.Read();
						var imageBitmap = new Bitmap(imageData.ToArray());
						ShowImagePreview(fileItem, imageBitmap, imageData.Length, displayFormat: Path.GetExtension(fileItem.Name).TrimStart('.'));
						break;
					case FileTreeItem.DataFormat.DdsImage:
						imageName = Path.GetFileNameWithoutExtension(fileItem.Name);

						ReadOnlySpan<byte> data;
						if (fileItem is GGPKFileTreeItem g2)
							data = g2.Record.Read();
						else
							data = fileItem.Read().Span;

						if (fileItem.Name.EndsWith(".header")) {
							data = ImageBitmapDecoder.ResolveDdsData(data, fileItem.Name, Index);
						}

						var (ddsBitmap, ddsInfo) = ImageBitmapDecoder.CreateDdsBitmap(data);
						ShowImagePreview(fileItem, ddsBitmap, data.Length, ddsInfo, "DDS");
						break;
					case FileTreeItem.DataFormat.Dat:
					// TODO: LibDat3
					// 	panel.Panel2 = DatPanel;
					// 	break;
					case FileTreeItem.DataFormat.Sound: {
							ReadOnlyMemory<byte> audioData;
							if (fileItem is GGPKFileTreeItem gw)
								audioData = gw.Record.Read();
							else
								audioData = fileItem.Read();
							PreviewPanel.ShowAudio(fileItem, audioData);
							panel.Panel2 = PreviewPanel;
						}
						break;
					case FileTreeItem.DataFormat.Video: {
							ReadOnlyMemory<byte> videoData;
							if (fileItem is GGPKFileTreeItem gv)
								videoData = gv.Record.Read();
							else
								videoData = fileItem.Read();
							PreviewPanel.ShowVideo(fileItem, videoData);
							panel.Panel2 = PreviewPanel;
						}
						break;
					default:
						TextPanel.Text = "";
						panel.Panel2 = TextPanel;
						// DatPanel.DataStore = null;
						break;
				}
				}, new Dictionary<string, object?> {
					["path"] = fileItem.GetPath(),
					["name"] = fileItem.Name
				});
			} catch (Exception ex) {
				DiagnosticLog.Error("preview", "preview_failed", ex, new Dictionary<string, object?> {
					["path"] = fileItem.GetPath()
				});
				TextPanel.Text = ex.ToString();
				panel.Panel2 = TextPanel;
			}
		}
	}

	private void OnExtractClicked(object? sender, EventArgs _) {
		if (clickedItem is FileTreeItem) {
			var tree = TreeForItem(clickedItem);
			var files = GetContextFileItems(tree).ToList();
			if (files.Count > 1) {
				using var dlg = new SelectFolderDialog();
				if (dlg.ShowDialog(this) != DialogResult.Ok)
					return;
				var baseDir = dlg.Directory;
				long totalBytes = 0;
				foreach (var fi in files) {
					var rel = fi.GetPath().Replace('/', Path.DirectorySeparatorChar);
					var dest = Path.Combine(baseDir, rel);
					Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
					var span = fi.Read().Span;
					using (var f = File.OpenHandle(dest, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.None, span.Length))
						RandomAccess.Write(f, span, 0);
					totalBytes += span.Length;
				}
				MessageBox.Show(this, $"Extracted {files.Count} files ({totalBytes:N0} bytes) to\r\n{baseDir}", "Done", MessageBoxType.Information);
				return;
			}
			var fiSingle = files[0];
			var ext = "*" + Path.GetExtension(fiSingle.Name);
			var sfd = new SaveFileDialog() {
				FileName = fiSingle.Name,
				Filters = {
					new(ext, ext),
					allFilesFilters
				}
			};
			if (sfd.ShowDialog(this) != DialogResult.Ok)
				return;
			Directory.CreateDirectory(Path.GetDirectoryName(sfd.FileName)!);
			var spanSingle = fiSingle.Read().Span;
			using (var f = File.OpenHandle(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.None, FileOptions.None, spanSingle.Length))
				RandomAccess.Write(f, spanSingle, 0);
			MessageBox.Show(this, $"Extracted {spanSingle.Length} bytes to\r\n{sfd.FileName}", "Done", MessageBoxType.Information);
		} else if (clickedItem is DirectoryTreeItem di) {
			var sfd = new SaveFileDialog() {
				CheckFileExists = false,
				FileName = di.Name + ".dir",
				Filters = { allFilesFilters }
			};
			if (sfd.ShowDialog(this) != DialogResult.Ok)
				return;
			var dir = Directory.CreateDirectory(Path.GetDirectoryName(sfd.FileName)!).FullName;
			MessageBox.Show(this, $"Extracted {di.Extract(dir)} files to\r\n{dir}", "Done", MessageBoxType.Information);
		}
	}

	private void OnReplaceClicked(object? sender, EventArgs _) {
		if (clickedItem is FileTreeItem) {
			var tree = TreeForItem(clickedItem);
			var files = GetContextFileItems(tree).ToList();
			if (files.Count > 1) {
				MessageBox.Show(this, "Replace one file at a time. Select a single file.", "Replace", MessageBoxType.Information);
				return;
			}
			var fi = files[0];
			var ext = "*" + Path.GetExtension(fi.Name);
			using var ofd = new OpenFileDialog() {
				FileName = fi.Name,
				Filters = {
					new(ext, ext),
					allFilesFilters
				}
			};
			if (ofd.ShowDialog(this) != DialogResult.Ok)
				return;
			var b = File.ReadAllBytes(ofd.FileName);
			fi.Write(b);
			MessageBox.Show(this, $"Replaced {b.Length} bytes from\r\n{ofd.FileName}", "Done", MessageBoxType.Information);
		} else if (clickedItem is DirectoryTreeItem di) {
			using var ofd = new OpenFileDialog() {
				CheckFileExists = false,
				FileName = "{OPEN IN A FOLDER}",
				Filters = { allFilesFilters }
			};
			if (ofd.ShowDialog(this) != DialogResult.Ok)
				return;
			var dir = Path.GetDirectoryName(ofd.FileName)!;
			MessageBox.Show(this, $"Replaced {di.Replace(dir)} files from\r\n{dir}", "Done", MessageBoxType.Information);
		}

		var bd2 = (GGPKTree.DataStore as GGPKDirectoryTreeItem)?.ChildItems.FirstOrDefault(t => t.Text == "Bundles2");
		if (bd2 is GGPKDirectoryTreeItem g) {
			g._ChildItems = null; // Update tree
			GGPKTree.RefreshItem(g);
		}
		OnSelectionChanged(BundleTree, EventArgs.Empty);
	}

	private void OnCopyPathClicked(object? sender, EventArgs _) {
		if (clickedItem is null)
			return;
		if (clickedItem is FileTreeItem) {
			var tree = TreeForItem(clickedItem);
			var files = GetContextFileItems(tree).ToList();
			if (files.Count > 1) {
				Clipboard.Instance.Text = string.Join(Environment.NewLine, files.Select(f => f.GetPath()));
				return;
			}
		}
		Clipboard.Instance.Text = clickedItem is DirectoryTreeItem di
			? di.GetPath()
			: ((FileTreeItem)clickedItem).GetPath();
	}

	private void OnSaveAsPngClicked(object? sender, EventArgs _) {
		var sfd = new SaveFileDialog {
			FileName = (imageName ?? "unnamed") + ".png",
			Filters = {
				new("Png File", "*.png"),
				allFilesFilters
			}
		};
		if (sfd.ShowDialog(this) != DialogResult.Ok)
			return;
		Directory.CreateDirectory(Path.GetDirectoryName(sfd.FileName)!);
		(PreviewPanel.ImageView.Image as Bitmap)!.Save(sfd.FileName, Eto.Drawing.ImageFormat.Png);
	}

	private void OnExportDdsClicked(object? sender, EventArgs _) {
		if (clickedItem is FileTreeItem) {
			var tree = TreeForItem(clickedItem);
			var files = GetContextFileItems(tree).Where(f => f.Format == FileTreeItem.DataFormat.DdsImage).ToList();
			if (files.Count == 0) {
				MessageBox.Show(this, "Selected file is not a dds image", "Error", MessageBoxType.Error);
				return;
			}
			if (files.Count > 1) {
				using var dlg = new SelectFolderDialog();
				if (dlg.ShowDialog(this) != DialogResult.Ok)
					return;
				var baseDir = dlg.Directory;
				int failed = 0;
				foreach (var fi in files) {
					var rel = Path.ChangeExtension(fi.GetPath().Replace('/', Path.DirectorySeparatorChar), ".png");
					var dest = Path.Combine(baseDir, rel);
					try {
						Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
						ImageBitmapDecoder.CreateDdsBitmap(fi.Read().Span).Bitmap.Save(dest, Eto.Drawing.ImageFormat.Png);
					} catch {
						Interlocked.Increment(ref failed);
					}
				}
				if (failed == 0)
					MessageBox.Show(this, $"Exported {files.Count} files to\r\n{baseDir}", "Done", MessageBoxType.Information);
				else
					MessageBox.Show(this, $"Exported {files.Count - failed} files to\r\n{baseDir}\r\n{failed} files failed!", "Done", MessageBoxType.Warning);
				return;
			}
			var fiSingle = files[0];
			var sfd = new SaveFileDialog() {
				FileName = Path.GetFileNameWithoutExtension(fiSingle.Name) + ".png",
				Filters = {
					new("*.png", "*.png"),
					allFilesFilters
				}
			};
			if (sfd.ShowDialog(this) != DialogResult.Ok)
				return;
			Directory.CreateDirectory(Path.GetDirectoryName(sfd.FileName)!);
			ImageBitmapDecoder.CreateDdsBitmap(fiSingle.Read().Span).Bitmap.Save(sfd.FileName, Eto.Drawing.ImageFormat.Png);
			MessageBox.Show(this, $"Saved {sfd.FileName}", "Done", MessageBoxType.Information);
		} else if (clickedItem is DirectoryTreeItem di) {
			var sfd = new SaveFileDialog() {
				CheckFileExists = false,
				FileName = di.Name + ".dir",
				Filters = { allFilesFilters }
			};
			if (sfd.ShowDialog(this) != DialogResult.Ok)
				return;
			var dir = Path.Combine(Path.GetDirectoryName(sfd.FileName)!, di.Name);
			int failed = 0;
			var count = di.Extract((path, data) => {
				var filename = Path.GetFileNameWithoutExtension(path);
				path = Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(dir, path))!).FullName;
				try {
					var bitmap = ImageBitmapDecoder.CreateDdsBitmap(data.Span).Bitmap;
					bitmap.Save(Path.Combine(path, filename + ".png"), Eto.Drawing.ImageFormat.Png);
				} catch {
					Interlocked.Increment(ref failed);
				}
			}, ".dds");
			if (failed == 0)
				MessageBox.Show(this, $"Exported {count} files to\r\n{dir}", "Done", MessageBoxType.Information);
			else
				MessageBox.Show(this, $"Exported {count} files to\r\n{dir}\r\n{failed} files failed!", "Done", MessageBoxType.Warning);
		}
	}

	private static readonly FileFilter allFilesFilters = new("All Files", "*");

	private void OnDragEnter(object? sender, DragEventArgs e) {
		if (Index is not null && e.Data.ContainsUris)
			e.Effects = DragEffects.Copy;
	}

	private void OnDragDrop(object? sender, DragEventArgs e) {
		if (Index is null || !e.Data.ContainsUris)
			return;

		try {
			var uris = e.Data.Uris;
			if (uris.Length != 1)
				goto err;
			var uri = uris[0];
			if (!uri.IsFile)
				goto err;
			var path = uri.LocalPath;
			if (!path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
				goto err;

			int count;
			using (var zip = ZipFile.OpenRead(path))
				count = sender == GGPKTree
					? GGPK.Replace((Ggpk ?? throw ThrowHelper.Create<InvalidOperationException>("GGPK replacing is not supported in Steam/Epic mode"))
						.Root, zip.Entries)
					: LibBundle3.Index.Replace(Index!, zip.Entries);
			MessageBox.Show(this, $"Replaced {count} files!", "Done", MessageBoxType.Information);
		} catch (Exception ex) {
			MessageBox.Show(this, ex.ToString(), "Error", MessageBoxType.Error);
		}

		return;
	err:
		MessageBox.Show(this, "Only a single zip file is allowed", "Error", MessageBoxType.Error);
		return;
	}

	private void ShowImagePreview(FileTreeItem fileItem, Bitmap bitmap, int fileSize, string? extraInfo = null, string? displayFormat = null) {
		var sb = new StringBuilder();
		sb.AppendLine($"File: {fileItem.Name}");
		sb.AppendLine($"Size: {FormatByteSize(fileSize)}");
		sb.AppendLine($"Dimensions: {bitmap.Width} x {bitmap.Height}");
		if (UvSequenceGrid.TryParse(fileItem.Name, fileItem.GetPath(), out var grid)) {
			sb.AppendLine($"UV sequence: {grid.Columns}x{grid.Rows} ({grid.FrameCount} frames)");
			sb.AppendLine($"Frame size: {bitmap.Width / grid.Columns} x {bitmap.Height / grid.Rows}");
		}
		if (!string.IsNullOrEmpty(displayFormat))
			sb.AppendLine($"Format: {displayFormat}");
		if (!string.IsNullOrEmpty(extraInfo))
			sb.Append(extraInfo.TrimEnd());
		PreviewPanel.ShowImage(bitmap, fileItem.Name, fileItem.GetPath(), sb.ToString().TrimEnd());
		RightLayout.Panel2 = PreviewPanel;
	}

	private async Task ExportFilteredImagesAsPngAsync() {
		if (!TreeViewFilter.IsActive) {
			MessageBox.Show(this,
				"Set a filter first (Show path, Hide words, and/or Images type) to choose which files to export.",
				"Export PNGs",
				MessageBoxType.Information);
			return;
		}
		if (Ggpk is null && Index is null) {
			MessageBox.Show(this, "Open a GGPK or index file first.", "Export PNGs", MessageBoxType.Information);
			return;
		}

		var bundleRoot = BundleTree.DataStore as BundleDirectoryTreeItem;
		var entries = FilteredImagePngExporter.Collect(Ggpk, Index, bundleRoot);
		if (entries.Count == 0) {
			MessageBox.Show(this,
				"No exportable images match the current filter.\r\n\r\nTip: set type to Images or narrow the path search.",
				"Export PNGs",
				MessageBoxType.Information);
			return;
		}

		using var dlg = new SelectFolderDialog { Title = "Export filtered images as PNG" };
		if (dlg.ShowDialog(this) != DialogResult.Ok)
			return;

		var baseDir = dlg.Directory;
		ShowLoadStatus($"Exporting {entries.Count:N0} image(s) to PNG…\r\n{baseDir}");
		DiagnosticLog.User("export_png_start", new Dictionary<string, object?> {
			["count"] = entries.Count,
			["dest"] = baseDir
		});

		var bundleIndex = Index;
		FilteredImagePngExportResult result;
		try {
			result = await DiagnosticLog.MeasureAsync("export", "png_batch", () =>
				Task.Run(() => FilteredImagePngExporter.Export(baseDir, entries, bundleIndex)),
				new Dictionary<string, object?> { ["count"] = entries.Count });
		} catch (Exception ex) {
			DiagnosticLog.Error("export", "png_batch_failed", ex);
			MessageBox.Show(this, ex.ToString(), "Export PNGs", MessageBoxType.Error);
			return;
		}

		DiagnosticLog.User("export_png_done", new Dictionary<string, object?> {
			["exported"] = result.Exported,
			["failed"] = result.Failed,
			["dest"] = baseDir
		});

		if (result.Failed == 0)
			MessageBox.Show(this,
				$"Exported {result.Exported:N0} PNG file(s) to\r\n{baseDir}",
				"Export PNGs",
				MessageBoxType.Information);
		else
			MessageBox.Show(this,
				$"Exported {result.Exported:N0} PNG file(s) to\r\n{baseDir}\r\n{result.Failed:N0} file(s) failed.",
				"Export PNGs",
				MessageBoxType.Warning);
	}

	private static string FormatByteSize(long bytes) => bytes switch {
		< 1024 => $"{bytes} bytes",
		< 1024 * 1024 => $"{bytes / 1024.0:F1} KB ({bytes:N0} bytes)",
		_ => $"{bytes / (1024.0 * 1024):F2} MB ({bytes:N0} bytes)"
	};

#if Windows
	private void WindowsHookTreeKeys(TreeView tree) {
#pragma warning disable CS0618
		var etree = ((Eto.Wpf.Forms.Controls.TreeViewHandler)tree.Handler).Control;
#pragma warning restore CS0618
		etree.PreviewKeyDown += (_, e) => {
			if (e.Key == System.Windows.Input.Key.Space) {
				if (TryPlaySelectedAudio() || TryPlaySelectedVideo() || PreviewPanel.ToggleSpritePlayback())
					e.Handled = true;
				return;
			}
			if (e.Key == System.Windows.Input.Key.Right) {
				if (TrySelectNextVisibleItem(tree))
					e.Handled = true;
			}
		};
	}
#endif
}