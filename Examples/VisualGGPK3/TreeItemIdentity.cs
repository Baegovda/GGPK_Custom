using Eto.Forms;

using VisualGGPK3.TreeItems;

namespace VisualGGPK3;

internal static class TreeItemIdentity {
	public static string? GetKey(ITreeItem item) => item switch {
		FileTreeItem file => "f:" + file.GetPath(),
		DirectoryTreeItem dir => "d:" + dir.GetPath(),
		_ => null
	};

	public static bool Same(ITreeItem? a, ITreeItem? b) {
		if (ReferenceEquals(a, b))
			return true;
		if (a is null || b is null)
			return false;
		var keyA = GetKey(a);
		return keyA is not null && keyA == GetKey(b);
	}
}
