using System;
using System.IO;

namespace VisualGGPK3;

internal static class LayoutSettingsStore {
	private const int DefaultMainSplitter = 160;
	private const int DefaultInnerSplitter = 240;
	private const int DefaultFavoritesSplitter = 220;

	private static string SettingsPath => Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		"VisualGGPK3",
		"layout.txt");

	public readonly struct Layout(int mainSplitter, int innerSplitter, bool infoAutoHide = false, string filterType = "", string filterExclude = "", int favoritesSplitter = DefaultFavoritesSplitter) {
		public int MainSplitter { get; } = mainSplitter;
		public int InnerSplitter { get; } = innerSplitter;
		public bool InfoAutoHide { get; } = infoAutoHide;
		public string FilterType { get; } = filterType;
		public string FilterExclude { get; } = filterExclude;
		public int FavoritesSplitter { get; } = favoritesSplitter;
		public static Layout Default => new(DefaultMainSplitter, DefaultInnerSplitter, favoritesSplitter: DefaultFavoritesSplitter);
	}

	public static Layout Load() {
		try {
			if (!File.Exists(SettingsPath))
				return Layout.Default;
			var main = DefaultMainSplitter;
			var inner = DefaultInnerSplitter;
			var infoAutoHide = false;
			var filterType = "";
			var filterExclude = "";
			var favorites = DefaultFavoritesSplitter;
			foreach (var line in File.ReadAllLines(SettingsPath)) {
				var sep = line.IndexOf('=');
				if (sep <= 0)
					continue;
				var key = line[..sep].Trim();
				var value = line[(sep + 1)..].Trim();
				if (key.Equals("main", StringComparison.OrdinalIgnoreCase)) {
					if (int.TryParse(value, out var n) && n > 0)
						main = n;
				} else if (key.Equals("inner", StringComparison.OrdinalIgnoreCase)) {
					if (int.TryParse(value, out var n) && n > 0)
						inner = n;
				} else if (key.Equals("infoAutoHide", StringComparison.OrdinalIgnoreCase)) {
					infoAutoHide = value.Equals("1", StringComparison.OrdinalIgnoreCase)
						|| value.Equals("true", StringComparison.OrdinalIgnoreCase);
				} else if (key.Equals("filterType", StringComparison.OrdinalIgnoreCase)) {
					filterType = value;
				} else if (key.Equals("filterExclude", StringComparison.OrdinalIgnoreCase)) {
					filterExclude = value;
				} else if (key.Equals("favorites", StringComparison.OrdinalIgnoreCase)) {
					if (int.TryParse(value, out var n) && n > 0)
						favorites = n;
				}
			}
			return new Layout(main, inner, infoAutoHide, filterType, filterExclude, favorites);
		} catch {
			return Layout.Default;
		}
	}

	public static void Save(Layout layout) {
		try {
			var dir = Path.GetDirectoryName(SettingsPath)!;
			Directory.CreateDirectory(dir);
			File.WriteAllText(SettingsPath,
				$"main={layout.MainSplitter}{Environment.NewLine}inner={layout.InnerSplitter}{Environment.NewLine}favorites={layout.FavoritesSplitter}{Environment.NewLine}infoAutoHide={(layout.InfoAutoHide ? 1 : 0)}{Environment.NewLine}filterType={layout.FilterType}{Environment.NewLine}filterExclude={layout.FilterExclude}{Environment.NewLine}");
		} catch {
			// ignore persistence errors
		}
	}
}
