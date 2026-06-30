using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal enum ThemeButtonVariant {
	Primary,
	Secondary,
	Ghost
}

internal static class AppTheme {
	public const int ControlHeight = 30;
	public const int ToolbarButtonWidth = 88;

	public static readonly Color WindowBg = Color.FromRgb(0x141418);
	public static readonly Color Surface = Color.FromRgb(0x1e1e26);
	public static readonly Color SurfaceRaised = Color.FromRgb(0x262630);
	public static readonly Color InputBg = Color.FromRgb(0x2a2a34);
	public static readonly Color Border = Color.FromRgb(0x3a3a48);
	public static readonly Color BorderFocus = Color.FromRgb(0x5b8def);

	public static readonly Color TextPrimary = Color.FromRgb(0xececf0);
	public static readonly Color TextSecondary = Color.FromRgb(0xa8a8b8);
	public static readonly Color TextMuted = Color.FromRgb(0x727282);

	public static readonly Color Accent = Color.FromRgb(0x5b8def);
	public static readonly Color AccentHover = Color.FromRgb(0x6f9df5);
	public static readonly Color AccentPressed = Color.FromRgb(0x4a7ad4);

	public static readonly Color SelectionPrimary = Color.FromRgb(0x355892);
	public static readonly Color SelectionSecondary = Color.FromRgb(0x2a426a);

	public static readonly Color DangerMuted = Color.FromRgb(0x8a4a4a);

	public static void ApplyForm(Form form) {
		form.BackgroundColor = WindowBg;
#if Windows
		WpfDarkTheme.ApplyForm(form);
#endif
	}

	public static void ApplyTitleBar(Control control) {
#if Windows
		WpfDarkTheme.ApplyTitleBar(control);
#endif
	}

	public static void ApplyPanel(Control control, bool raised = false) {
		control.BackgroundColor = raised ? SurfaceRaised : Surface;
#if Windows
		WpfDarkTheme.ApplyPanel(control, raised);
#endif
	}

	public static void ApplyTreeHost(Control control) {
		control.BackgroundColor = Surface;
#if Windows
		WpfDarkTheme.ApplyTreeHost(control);
#endif
	}

	public static Label CreateFieldLabel(string text) {
		var label = new Label {
			Text = text,
			TextColor = TextSecondary,
			VerticalAlignment = VerticalAlignment.Center,
			Width = 44,
			TextAlignment = TextAlignment.Right
		};
		return label;
	}

	public static void StyleTextInput(TextBox textBox, bool readOnly = false) {
		textBox.BackgroundColor = readOnly ? SurfaceRaised : InputBg;
		textBox.TextColor = readOnly ? TextSecondary : TextPrimary;
		textBox.Height = ControlHeight;
#if Windows
		WpfDarkTheme.StyleTextBox(textBox, readOnly);
#endif
	}

	public static void StyleButton(Button button, ThemeButtonVariant variant = ThemeButtonVariant.Secondary) {
		button.Height = ControlHeight;
		switch (variant) {
			case ThemeButtonVariant.Primary:
				button.BackgroundColor = Accent;
				button.TextColor = Colors.White;
				break;
			case ThemeButtonVariant.Ghost:
				button.BackgroundColor = Colors.Transparent;
				button.TextColor = TextSecondary;
				break;
			default:
				button.BackgroundColor = SurfaceRaised;
				button.TextColor = TextPrimary;
				break;
		}
#if Windows
		WpfDarkTheme.StyleButton(button, variant);
#endif
	}

	public static void StyleDropDown(DropDown dropDown) {
		dropDown.BackgroundColor = InputBg;
		dropDown.TextColor = TextPrimary;
		dropDown.Height = ControlHeight;
#if Windows
		WpfDarkTheme.StyleDropDown(dropDown);
#endif
	}

	public static void StyleHintLabel(Label label) {
		label.TextColor = TextMuted;
	}

	public static void StyleCaptionLabel(Label label) {
		label.TextColor = TextSecondary;
		label.Font = new Font(SystemFonts.Default().Family, SystemFonts.Default().Size - 1);
	}

	public static void StyleTimeLabel(Label label, bool muted = false) {
		label.TextColor = muted ? TextMuted : TextPrimary;
		label.Font = Fonts.Monospace(SystemFonts.Default().Size - 1);
		label.VerticalAlignment = VerticalAlignment.Center;
	}

	public static void StyleSlider(Slider slider) {
#if Windows
		WpfDarkTheme.StyleSlider(slider);
#endif
	}

	public static void StyleHeaderLabel(Label label) {
		label.TextColor = TextPrimary;
	}

	public static void StyleTextArea(TextArea area) {
		area.BackgroundColor = Surface;
		area.TextColor = TextPrimary;
#if Windows
		WpfDarkTheme.StyleTextArea(area);
#endif
	}

	public static void StyleSplitter(Splitter splitter) {
		splitter.BackgroundColor = WindowBg;
#if Windows
		WpfDarkTheme.StyleSplitter(splitter);
#endif
	}
}
