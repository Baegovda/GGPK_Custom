using System.IO;
using System.Reflection;

using Eto.Drawing;

namespace VisualGGPK3;

internal static class TreeItemIcons {
	public static readonly Bitmap File;

	static TreeItemIcons() {
		try {
			using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("VisualGGPK3.Resources.file.ico");
			File = new(s);
		} catch {
			File = null!;
		}
	}
}
