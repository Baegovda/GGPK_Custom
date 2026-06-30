namespace LibBundle3;

/// <summary>
/// Why a path entry from the directory bundle could not be linked to a <see cref="Records.FileRecord"/>.
/// </summary>
public enum ParsePathFailureKind {
	/// <summary>
	/// <c>_Files.TryGetValue(NameHash(path))</c> returned false — the directory blob names a path
	/// whose hash is not present in the index file table.
	/// </summary>
	PathHashNotInFileTable,
}

/// <summary>
/// One directory-bundle path entry that failed during <see cref="Index.ParsePaths"/>.
/// </summary>
public sealed class ParsePathFailure {
	public required int DirectoryRecordIndex { get; init; }
	public required ulong DirectoryPathHash { get; init; }
	public required int DirectoryDataOffset { get; init; }
	public required int DirectoryDataSize { get; init; }
	public required string AttemptedPath { get; init; }
	public required ulong ComputedPathHash { get; init; }
	public required ParsePathFailureKind Kind { get; init; }
	/// <summary><see langword="true"/> when built from stacked path segments; <see langword="false"/> for a single segment.</summary>
	public required bool FromConcatenatedSegments { get; init; }
}

/// <summary>
/// A <see cref="Records.FileRecord"/> in the index file table that still has no <see cref="Records.FileRecord.Path"/>
/// after <see cref="Index.ParsePaths"/> completed.
/// </summary>
public sealed class UnresolvedFileRecord {
	public required ulong PathHash { get; init; }
	public required string BundlePath { get; init; }
	public required int BundleIndex { get; init; }
	public required int Offset { get; init; }
	public required int Size { get; init; }
}
