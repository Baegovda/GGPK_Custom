using System;

using Eto.Forms;

using LibVLCSharp.Shared;

namespace VisualGGPK3;
public static class Program {
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	public static void Main(string[] args) {
		DiagnosticLog.Initialize();
#if Windows
		Core.Initialize();
#endif
#if Mac
		Eto.Style.Add<Eto.Mac.Forms.ApplicationHandler>(null, handler => handler.AllowClosingMainForm = true);
#endif
		var app = new Application();
#if Windows
		WpfDarkTheme.Initialize();
#endif
		DiagnosticLog.AttachApplication(app);
		var form = new MainWindow(args.Length != 0 ? args[0] : null);
		app.UnhandledException += (_, e) => {
			MessageBox.Show(app.MainForm, e.ExceptionObject.ToString(), "Error", MessageBoxType.Error);
		};
		app.Run(app.MainForm = form);
		DiagnosticLog.LogSessionEnd("main_returned");
	}
}