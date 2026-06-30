using System;
using System.Text;

using LibBundle3;

using BundleIndex = LibBundle3.Index;

namespace VisualGGPK3;

internal static class LoadStatusReport {
	public static string Build(string path, BundleIndex? index, int directoryParseFailures, string? extraNote = null) {
		var sb = new StringBuilder();
		sb.AppendLine("=== Load Status ===");
		sb.AppendLine($"File: {path}");
		if (index is null) {
			sb.AppendLine("Bundle index: not loaded");
			if (extraNote is not null)
				sb.AppendLine(extraNote);
			return sb.ToString().TrimEnd();
		}

		sb.AppendLine($"Files in index: {index.Files.Count:N0}");
		sb.AppendLine($"Bundles: {index.Bundles.Length:N0}");
		sb.AppendLine($"Directory records: {index.DirectoryRecordCount:N0}");
		sb.AppendLine($"Name hash: {index.NameHashAlgorithmDescription}");
		sb.AppendLine($"Paths resolved: {index.Files.Count - index.UnresolvedFiles.Count:N0} / {index.Files.Count:N0}");
		if (extraNote is not null)
			sb.AppendLine(extraNote);

		if (directoryParseFailures == 0 && index.ParsePathFailures.Count == 0 && index.UnresolvedFiles.Count == 0) {
			sb.AppendLine();
			sb.AppendLine("Path parsing: all entries matched successfully.");
			return sb.ToString().TrimEnd();
		}

		if (directoryParseFailures != 0 || index.ParsePathFailures.Count != 0) {
			sb.AppendLine();
			sb.AppendLine($"=== Directory Path Parse Failures ({index.ParsePathFailures.Count}) ===");
			sb.AppendLine("These paths appear in the Bundles2 directory blob inside _.index.bin, but their");
			sb.AppendLine("computed PathHash was not found in the index file table (_Files dictionary).");
			sb.AppendLine("They will not appear in the bundle tree.");
			sb.AppendLine();
			sb.AppendLine("Code: LibBundle3.Index.ParsePaths()");
			sb.AppendLine("  if (!_Files.TryGetValue(NameHash(pathBytes), out var file)) { ++failed; }");
			sb.AppendLine();

			for (var i = 0; i < index.ParsePathFailures.Count; ++i) {
				var f = index.ParsePathFailures[i];
				sb.AppendLine($"--- Failure #{i + 1} ---");
				sb.AppendLine($"Attempted path: {f.AttemptedPath}");
				sb.AppendLine($"Computed PathHash: 0x{f.ComputedPathHash:X16}");
				sb.AppendLine($"Kind: {f.Kind}");
				sb.AppendLine($"Built from: {(f.FromConcatenatedSegments ? "concatenated path segments" : "single path segment")}");
				sb.AppendLine($"DirectoryRecord[{f.DirectoryRecordIndex}]:");
				sb.AppendLine($"  PathHash = 0x{f.DirectoryPathHash:X16}");
				sb.AppendLine($"  Data offset in directory blob = {f.DirectoryDataOffset} (0x{f.DirectoryDataOffset:X})");
				sb.AppendLine($"  Data size = {f.DirectoryDataSize} bytes");
				sb.AppendLine("Explanation:");
				sb.AppendLine("  The directory listing references this file path, but no FileRecord with the");
				sb.AppendLine("  same PathHash exists in the index. Common causes: truncated/corrupt index,");
				sb.AppendLine("  version mismatch, or a file removed from the file table but left in the directory blob.");
				sb.AppendLine();
			}
		}

		if (index.UnresolvedFiles.Count != 0) {
			sb.AppendLine($"=== Unresolved File Records ({index.UnresolvedFiles.Count}) ===");
			sb.AppendLine("These FileRecord entries exist in the index file table but never received a Path");
			sb.AppendLine("during ParsePaths() (Path remains null). They are omitted from the bundle tree.");
			sb.AppendLine();
			sb.AppendLine("Code: LibBundle3.Index.BuildTree(..., ignoreNullPath: true) skips them.");
			sb.AppendLine();

			for (var i = 0; i < index.UnresolvedFiles.Count; ++i) {
				var u = index.UnresolvedFiles[i];
				sb.AppendLine($"--- Unresolved #{i + 1} ---");
				sb.AppendLine($"PathHash: 0x{u.PathHash:X16}");
				sb.AppendLine($"Bundle: {u.BundlePath}.bundle.bin (index {u.BundleIndex})");
				sb.AppendLine($"Offset in bundle: {u.Offset} (0x{u.Offset:X}), size: {u.Size:N0} bytes (0x{u.Size:X})");
				sb.AppendLine("Explanation:");
				sb.AppendLine("  The file table lists this entry, but no matching path string was resolved from");
				sb.AppendLine("  the directory blob. It may be orphaned data or a hash/path the directory omits.");
				sb.AppendLine();
			}
		}

		return sb.ToString().TrimEnd();
	}
}
