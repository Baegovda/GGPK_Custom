using System;
using System.Collections.Generic;
using System.IO;

using LibGGPK3;
using LibGGPK3.Records;

using VisualGGPK3.TreeItems;

using Index = LibBundle3.Index;

namespace VisualGGPK3;

internal readonly record struct FilteredImageEntry(string Path, Func<ReadOnlyMemory<byte>> Read);

internal sealed class FilteredImagePngExportResult {
	public int Exported { get; init; }
	public int Failed { get; init; }
	public int Total => Exported + Failed;
}

internal static class FilteredImagePngExporter {
	public static List<FilteredImageEntry> Collect(GGPK? ggpk, Index? bundleIndex, BundleDirectoryTreeItem? bundleRoot) {
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var list = new List<FilteredImageEntry>();

		if (ggpk is not null) {
			foreach (var (record, path) in TreeNode.RecurseFiles(ggpk.Root)) {
				if (!seen.Add(path))
					continue;
				if (!TreeViewFilter.MatchesPath(path))
					continue;
				if (!ImageBitmapDecoder.IsExportableImagePath(path))
					continue;
				var fr = record;
				list.Add(new FilteredImageEntry(path, () => fr.Read()));
			}
		}

		if (bundleIndex is not null && bundleRoot is not null) {
			foreach (var node in Index.Recursefiles(bundleRoot)) {
				var path = node.Record.Path;
				if (!seen.Add(path))
					continue;
				if (!TreeViewFilter.MatchesPath(path))
					continue;
				if (!ImageBitmapDecoder.IsExportableImagePath(path))
					continue;
				var record = node.Record;
				list.Add(new FilteredImageEntry(path, () => record.Read()));
			}
		}

		return list;
	}

	public static FilteredImagePngExportResult Export(string baseDirectory, IReadOnlyList<FilteredImageEntry> entries, Index? bundleIndex) {
		var exported = 0;
		var failed = 0;
		foreach (var entry in entries) {
			try {
				var rel = entry.Path.Replace('/', Path.DirectorySeparatorChar);
				var dest = Path.Combine(baseDirectory, Path.ChangeExtension(rel, ".png"));
				Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
				using var bitmap = ImageBitmapDecoder.Decode(entry.Read().Span, Path.GetFileName(entry.Path), bundleIndex);
				bitmap.Save(dest, Eto.Drawing.ImageFormat.Png);
				exported++;
			} catch {
				failed++;
			}
		}
		return new FilteredImagePngExportResult { Exported = exported, Failed = failed };
	}
}
