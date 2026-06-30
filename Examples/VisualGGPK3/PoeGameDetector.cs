using System;
using System.Diagnostics;
using System.IO;

namespace VisualGGPK3;

internal static class PoeGameDetector {
	private static readonly string[] ClientProcessNames = [
		"PathOfExileSteam",
		"PathOfExile",
		"PathOfExileEGS",
		"PathOfExile_KG",
		"PathOfExile_x64"
	];

	public static bool IsClientRunning() {
		foreach (var name in ClientProcessNames) {
			if (Process.GetProcessesByName(name).Length > 0)
				return true;
		}
		return false;
	}

	public static bool IsGameArchivePath(string path) {
		if (string.IsNullOrWhiteSpace(path))
			return false;
		var fileName = Path.GetFileName(path);
		if (fileName.Equals("Content.ggpk", StringComparison.OrdinalIgnoreCase))
			return true;
		var full = Path.GetFullPath(path);
		return full.Contains("Path of Exile", StringComparison.OrdinalIgnoreCase)
			|| full.Contains("Path of Exile2", StringComparison.OrdinalIgnoreCase);
	}

	public static string BuildGameLockWarning(string path) =>
		"Path of Exile 2(또는 Path of Exile)가 실행 중일 때는 설치 폴더의 Content.ggpk가 게임에 의해 잠겨 열 수 없습니다.\n\n" +
		"해결 방법:\n" +
		"• 게임을 완전히 종료한 뒤 다시 열기\n" +
		"• 또는 Content.ggpk를 다른 폴더로 복사한 뒤 복사본 열기\n\n" +
		$"파일:\n{Path.GetFullPath(path)}";
}
