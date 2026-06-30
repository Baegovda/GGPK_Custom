using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VisualGGPK3;

internal static class Bink2Native {
	private const string DllName = "bink2w64";
	private const uint BinkSurface32Ra = 6;
	private static bool resolverInstalled;

	[StructLayout(LayoutKind.Sequential)]
	public struct BinkInfo {
		public uint Width;
		public uint Height;
		public uint Frames;
		public uint FrameNum;
		public uint LastFrameNum;
		public uint FrameRate;
		public uint FrameRateDiv;
	}

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
	private static extern IntPtr BinkOpen(string name, uint flags);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern void BinkClose(IntPtr bink);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern int BinkDoFrame(IntPtr bink);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern void BinkNextFrame(IntPtr bink);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern int BinkWait(IntPtr bink);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern int BinkCopyToBuffer(IntPtr bink, IntPtr dest, int destPitch, uint destHeight, uint destX, uint destY, uint flags);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern int BinkPause(IntPtr bink, int pause);

	[DllImport(DllName, CallingConvention = CallingConvention.StdCall)]
	private static extern void BinkGoto(IntPtr bink, uint frame, int flags);

	public static bool IsAvailable {
		get {
			EnsureResolver();
			return Bink2Locator.TryGetDllPath() is not null;
		}
	}

	public static string MissingDllMessage =>
		"BK2 playback needs bink2w64.dll from your Path of Exile install (Steam, Daum/Kakao, or standalone). " +
		"Use “Locate bink2w64.dll…” below, or copy the DLL next to VisualGGPK3.exe.";

	public static IntPtr Open(string path) {
		EnsureResolver();
		var handle = BinkOpen(path, 0);
		return handle == IntPtr.Zero ? IntPtr.Zero : handle;
	}

	public static BinkInfo ReadInfo(IntPtr bink) => Marshal.PtrToStructure<BinkInfo>(bink);

	public static void Close(IntPtr bink) {
		if (bink != IntPtr.Zero)
			BinkClose(bink);
	}

	public static bool IsFrameReady(IntPtr bink) => BinkWait(bink) == 0;

	public static bool DecodeFrame(IntPtr bink) => BinkDoFrame(bink) != 0;

	public static void AdvanceFrame(IntPtr bink) => BinkNextFrame(bink);

	public static void SetPaused(IntPtr bink, bool paused) => BinkPause(bink, paused ? 1 : 0);

	public static void GotoFrame(IntPtr bink, uint frame) => BinkGoto(bink, Math.Max(1, frame), 0);

	public static bool CopyFrameToBuffer(IntPtr bink, IntPtr dest, int width, int height) {
		var pitch = width * 4;
		return BinkCopyToBuffer(bink, dest, pitch, (uint)height, 0, 0, BinkSurface32Ra) != 0;
	}

	private static void EnsureResolver() {
		if (resolverInstalled)
			return;
		NativeLibrary.SetDllImportResolver(typeof(Bink2Native).Assembly, ResolveDll);
		resolverInstalled = true;
	}

	private static IntPtr ResolveDll(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
		if (!libraryName.Equals(DllName, StringComparison.OrdinalIgnoreCase))
			return IntPtr.Zero;
		var path = Bink2Locator.TryGetDllPath();
		return path is null ? IntPtr.Zero : NativeLibrary.Load(path);
	}
}
