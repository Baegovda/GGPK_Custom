#if Windows
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;

using Eto.Wpf.Forms;

using EtoControl = Eto.Forms.Control;
using EtoForm = Eto.Forms.Form;
using EtoButton = Eto.Forms.Button;
using EtoTextBox = Eto.Forms.TextBox;
using EtoTextArea = Eto.Forms.TextArea;
using EtoDropDown = Eto.Forms.DropDown;
using EtoSlider = Eto.Forms.Slider;
using EtoSplitter = Eto.Forms.Splitter;
using EtoTreeView = Eto.Forms.TreeView;
using EtoGridView = Eto.Forms.GridView;

using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfControl = System.Windows.Controls.Control;

namespace VisualGGPK3;

internal static class WpfDarkTheme {
	private static readonly WpfBrush WindowBg = Freeze(Solid(0x14, 0x14, 0x18));
	private static readonly WpfBrush Surface = Freeze(Solid(0x1e, 0x1e, 0x26));
	private static readonly WpfBrush SurfaceRaised = Freeze(Solid(0x26, 0x26, 0x30));
	private static readonly WpfBrush InputBg = Freeze(Solid(0x2a, 0x2a, 0x34));
	private static readonly WpfBrush Border = Freeze(Solid(0x3a, 0x3a, 0x48));
	private static readonly WpfBrush TextPrimary = Freeze(Solid(0xec, 0xec, 0xf0));
	private static readonly WpfBrush TextSecondary = Freeze(Solid(0xa8, 0xa8, 0xb8));
	private static readonly WpfBrush Accent = Freeze(Solid(0x5b, 0x8d, 0xef));
	private static readonly WpfBrush AccentHover = Freeze(Solid(0x6f, 0x9d, 0xf5));
	private static readonly WpfBrush AccentPressed = Freeze(Solid(0x4a, 0x7a, 0xd4));
	private static readonly WpfBrush SelectionPrimary = Freeze(Solid(0x35, 0x58, 0x92));
	private static readonly WpfBrush SelectionSecondary = Freeze(Solid(0x2a, 0x42, 0x6a));
	private static readonly HashSet<IntPtr> HookedWindows = [];

	public static WpfBrush PrimarySelectionBrush => SelectionPrimary;
	public static WpfBrush SecondarySelectionBrush => SelectionSecondary;
	public static WpfBrush PrimaryTextBrush => TextPrimary;

	public static void Initialize() {
		if (System.Windows.Application.Current is null)
			return;
		var resources = System.Windows.Application.Current.Resources;
		resources[SystemColors.WindowBrushKey] = WindowBg;
		resources[SystemColors.WindowTextBrushKey] = TextPrimary;
		resources[SystemColors.ControlBrushKey] = Surface;
		resources[SystemColors.ControlTextBrushKey] = TextPrimary;
		resources[SystemColors.HighlightBrushKey] = SelectionPrimary;
		resources[SystemColors.HighlightTextBrushKey] = TextPrimary;
		resources[SystemColors.InactiveSelectionHighlightBrushKey] = SelectionSecondary;
		resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = TextPrimary;

		// Eto creates the native WPF Window after the Form ctor; hook every form here.
		Eto.Style.Add<FormHandler>(null, handler => {
			if (handler.Control is not Window window)
				return;
			SetBackground(window, WindowBg);
			HookNativeTitleBar(window);
		});
	}

	private static void SetBackground(FrameworkElement element, WpfBrush brush) {
		if (element is WpfControl control)
			control.Background = brush;
	}

	public static void ApplyForm(EtoForm form) {
		if (TryWpfWindow(form, out var window)) {
			SetBackground(window, WindowBg);
			HookNativeTitleBar(window);
			return;
		}
		form.Load += (_, _) => {
			if (TryWpfWindow(form, out window)) {
				SetBackground(window, WindowBg);
				HookNativeTitleBar(window);
			}
		};
	}

	public static void ApplyTitleBar(EtoControl control) {
		if (!TryWpf(control, out var element))
			return;
		var window = element as Window ?? Window.GetWindow(element);
		if (window is not null)
			HookNativeTitleBar(window);
	}

	public static void ApplyPanel(EtoControl control, bool raised) {
		if (TryWpf(control, out var wpf))
			SetBackground(wpf, raised ? SurfaceRaised : Surface);
	}

	public static void ApplyTreeHost(EtoControl control) {
		if (TryWpf(control, out var wpf)) {
			SetBackground(wpf, Surface);
			if (wpf is WpfControl c)
				c.Foreground = TextPrimary;
		}
	}

	public static void StyleTextBox(EtoTextBox textBox, bool readOnly) {
		if (!TryWpf(textBox, out var wpf) || wpf is not System.Windows.Controls.TextBox tb)
			return;
		tb.Background = readOnly ? SurfaceRaised : InputBg;
		tb.Foreground = readOnly ? TextSecondary : TextPrimary;
		tb.BorderBrush = Border;
		tb.CaretBrush = TextPrimary;
		tb.Padding = new Thickness(8, 4, 8, 4);
		tb.MinHeight = AppTheme.ControlHeight;
		tb.MaxHeight = AppTheme.ControlHeight;
		tb.VerticalContentAlignment = VerticalAlignment.Center;
	}

	public static void StyleTextArea(EtoTextArea area) {
		if (!TryWpf(area, out var wpf) || wpf is not System.Windows.Controls.TextBox tb)
			return;
		tb.Background = Surface;
		tb.Foreground = TextPrimary;
		tb.BorderBrush = Border;
		tb.CaretBrush = TextPrimary;
	}

	public static void StyleButton(EtoButton button, ThemeButtonVariant variant) {
		if (!TryWpf(button, out var wpf) || wpf is not System.Windows.Controls.Button btn)
			return;
		btn.BorderThickness = new Thickness(1);
		btn.Padding = new Thickness(10, 2, 10, 2);
		btn.BorderBrush = Border;
		btn.MinHeight = AppTheme.ControlHeight;
		btn.MaxHeight = AppTheme.ControlHeight;
		btn.VerticalContentAlignment = VerticalAlignment.Center;
		switch (variant) {
			case ThemeButtonVariant.Primary:
				btn.Background = Accent;
				btn.Foreground = Brushes.White;
				btn.BorderBrush = AccentPressed;
				break;
			case ThemeButtonVariant.Ghost:
				btn.Background = Brushes.Transparent;
				btn.Foreground = TextSecondary;
				btn.BorderBrush = Brushes.Transparent;
				break;
			default:
				btn.Background = SurfaceRaised;
				btn.Foreground = TextPrimary;
				break;
		}
	}

	public static void StyleDropDown(EtoDropDown dropDown) {
		if (!TryWpf(dropDown, out var wpf) || wpf is not System.Windows.Controls.ComboBox combo)
			return;
		combo.Background = InputBg;
		combo.Foreground = TextPrimary;
		combo.BorderBrush = Border;
		combo.Padding = new Thickness(6, 4, 6, 4);
	}

	public static void StyleSlider(EtoSlider slider) {
		if (!TryWpf(slider, out var wpf) || wpf is not System.Windows.Controls.Slider s)
			return;
		s.Height = 22;
		s.MinHeight = 22;
		s.Background = Brushes.Transparent;
		s.BorderThickness = new Thickness(0);
		s.Foreground = Accent;
		s.Template = CreateSliderTemplate();
	}

	private static ControlTemplate CreateSliderTemplate() {
		const string xaml = """
			<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
			                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			                 TargetType='{x:Type Slider}'>
			  <Grid MinHeight='22' VerticalAlignment='Center'>
			    <Border Height='4' CornerRadius='2' Background='#363642' VerticalAlignment='Center'/>
			    <Track x:Name='PART_Track'>
			      <Track.DecreaseRepeatButton>
			        <RepeatButton Command='Slider.DecreaseLarge'>
			          <RepeatButton.Template>
			            <ControlTemplate TargetType='RepeatButton'>
			              <Border Height='4' Background='#5b8def' CornerRadius='2,0,0,2' VerticalAlignment='Center'/>
			            </ControlTemplate>
			          </RepeatButton.Template>
			        </RepeatButton>
			      </Track.DecreaseRepeatButton>
			      <Track.IncreaseRepeatButton>
			        <RepeatButton Command='Slider.IncreaseLarge' Opacity='0' Focusable='False'/>
			      </Track.IncreaseRepeatButton>
			      <Track.Thumb>
			        <Thumb Width='12' Height='12' VerticalAlignment='Center'>
			          <Thumb.Template>
			            <ControlTemplate TargetType='Thumb'>
			              <Ellipse Fill='#ececf0' Width='12' Height='12'/>
			            </ControlTemplate>
			          </Thumb.Template>
			        </Thumb>
			      </Track.Thumb>
			    </Track>
			  </Grid>
			</ControlTemplate>
			""";
		return (ControlTemplate)XamlReader.Parse(xaml);
	}

	public static void StyleSplitter(EtoSplitter splitter) {
		if (TryWpf(splitter, out var wpf))
			SetBackground(wpf, WindowBg);
	}

	public static void StyleTreeView(EtoTreeView tree) {
		if (!TryWpf(tree, out var wpf) || wpf is not System.Windows.Controls.TreeView tv)
			return;
		tv.Background = Surface;
		tv.Foreground = TextPrimary;
		tv.BorderBrush = Border;
		tv.BorderThickness = new Thickness(1);
		tv.SnapsToDevicePixels = true;
		tv.UseLayoutRounding = true;
		tv.SetValue(RenderOptions.ClearTypeHintProperty, ClearTypeHint.Enabled);
		tv.ItemContainerStyle = CreateTreeViewItemStyle();
	}

	private static Style CreateTreeViewItemStyle() {
		var style = new Style(typeof(TreeViewItem));
		style.Setters.Add(new Setter(TreeViewItem.ForegroundProperty, TextPrimary));
		style.Setters.Add(new Setter(TreeViewItem.BackgroundProperty, Brushes.Transparent));
		style.Setters.Add(new Setter(TreeViewItem.BorderThicknessProperty, new Thickness(0)));
		style.Setters.Add(new Setter(TreeViewItem.PaddingProperty, new Thickness(2, 1, 4, 1)));
		style.Setters.Add(new Setter(TreeViewItem.HorizontalContentAlignmentProperty, HorizontalAlignment.Left));
		style.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
			new Binding("Expanded") { Mode = BindingMode.TwoWay }));
		style.Setters.Add(new Setter(TreeViewItem.TemplateProperty, CreateTreeViewItemTemplate()));
		var selectedTrigger = new Trigger { Property = TreeViewItem.IsSelectedProperty, Value = true };
		selectedTrigger.Setters.Add(new Setter(TreeViewItem.BackgroundProperty, SelectionPrimary));
		selectedTrigger.Setters.Add(new Setter(TreeViewItem.ForegroundProperty, TextPrimary));
		style.Triggers.Add(selectedTrigger);
		var inactiveSelected = new MultiTrigger();
		inactiveSelected.Conditions.Add(new Condition(TreeViewItem.IsSelectedProperty, true));
		inactiveSelected.Conditions.Add(new Condition(TreeViewItem.IsSelectionActiveProperty, false));
		inactiveSelected.Setters.Add(new Setter(TreeViewItem.BackgroundProperty, SelectionSecondary));
		inactiveSelected.Setters.Add(new Setter(TreeViewItem.ForegroundProperty, TextPrimary));
		style.Triggers.Add(inactiveSelected);
		return style;
	}

	private static ControlTemplate CreateTreeViewItemTemplate() {
		const string xaml = """
			<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
			                 xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
			                 TargetType='{x:Type TreeViewItem}'>
			  <Grid SnapsToDevicePixels='True'>
			    <Grid.RowDefinitions>
			      <RowDefinition Height='Auto'/>
			      <RowDefinition/>
			    </Grid.RowDefinitions>
			    <StackPanel Grid.Row='0' Orientation='Horizontal' HorizontalAlignment='Left'>
			      <ToggleButton x:Name='Expander'
			                    Width='16' Height='16' Margin='0,0,2,0' Padding='0'
			                    ClickMode='Press' Focusable='False'
			                    Background='Transparent' BorderThickness='0'
			                    IsChecked='{Binding IsExpanded, RelativeSource={RelativeSource TemplatedParent}}'>
			        <ToggleButton.Style>
			          <Style TargetType='ToggleButton'>
			            <Setter Property='Visibility' Value='Collapsed'/>
			            <Style.Triggers>
			              <DataTrigger Binding='{Binding HasItems, RelativeSource={RelativeSource AncestorType=TreeViewItem}}' Value='True'>
			                <Setter Property='Visibility' Value='Visible'/>
			              </DataTrigger>
			            </Style.Triggers>
			          </Style>
			        </ToggleButton.Style>
			        <Path x:Name='Chevron' Data='M 2 1 L 6 5 L 2 9' Stroke='#a8a8b8' StrokeThickness='1.4'
			              HorizontalAlignment='Center' VerticalAlignment='Center'/>
			      </ToggleButton>
			      <Border x:Name='LeafIndent' Width='18' Visibility='Collapsed'
			              Background='Transparent' IsHitTestVisible='False'/>
			      <Border x:Name='ItemBg'
			              Background='{TemplateBinding Background}'
			              BorderBrush='{TemplateBinding BorderBrush}'
			              BorderThickness='{TemplateBinding BorderThickness}'
			              CornerRadius='3' Padding='1,1,4,1' SnapsToDevicePixels='True'>
			        <ContentPresenter x:Name='PART_Header'
			                          ContentSource='Header' VerticalAlignment='Center'
			                          TextElement.Foreground='{TemplateBinding Foreground}'/>
			      </Border>
			    </StackPanel>
			    <ItemsPresenter x:Name='ItemsHost' Grid.Row='1' Grid.Column='0' Grid.ColumnSpan='2'
			                    Margin='16,0,0,0'/>
			  </Grid>
			  <ControlTemplate.Triggers>
			    <Trigger Property='IsExpanded' Value='False'>
			      <Setter TargetName='ItemsHost' Property='Visibility' Value='Collapsed'/>
			    </Trigger>
			    <Trigger Property='HasItems' Value='False'>
			      <Setter TargetName='LeafIndent' Property='Visibility' Value='Visible'/>
			    </Trigger>
			  </ControlTemplate.Triggers>
			</ControlTemplate>
			""";
		return (ControlTemplate)XamlReader.Parse(xaml);
	}

	public static void StyleGridView(EtoGridView grid) {
		if (!TryWpf(grid, out var wpf) || wpf is not System.Windows.Controls.DataGrid dg)
			return;
		dg.Background = Surface;
		dg.Foreground = TextPrimary;
		dg.BorderBrush = Border;
		dg.RowBackground = Surface;
		dg.AlternatingRowBackground = SurfaceRaised;
		dg.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
		dg.HorizontalGridLinesBrush = Border;
		dg.HeadersVisibility = DataGridHeadersVisibility.Column;
		var headerStyle = new System.Windows.Style(typeof(System.Windows.Controls.Primitives.DataGridColumnHeader));
		headerStyle.Setters.Add(new Setter(WpfControl.BackgroundProperty, SurfaceRaised));
		headerStyle.Setters.Add(new Setter(WpfControl.ForegroundProperty, TextSecondary));
		headerStyle.Setters.Add(new Setter(WpfControl.BorderBrushProperty, Border));
		headerStyle.Setters.Add(new Setter(WpfControl.PaddingProperty, new Thickness(6, 4, 6, 4)));
		dg.ColumnHeaderStyle = headerStyle;
	}

	private static bool TryWpf(EtoControl control, out FrameworkElement element) {
		element = null!;
		var prop = control.Handler?.GetType().GetProperty("Control");
		if (prop?.GetValue(control.Handler) is FrameworkElement fe) {
			element = fe;
			return true;
		}
		return false;
	}

	private static bool TryWpfWindow(EtoForm form, out Window window) {
		window = null!;
		if (form.Handler is FormHandler { Control: Window wpfWindow }) {
			window = wpfWindow;
			return true;
		}
		var prop = form.Handler?.GetType().GetProperty("Control");
		if (prop?.GetValue(form.Handler) is Window reflected) {
			window = reflected;
			return true;
		}
		return false;
	}

	private static void HookNativeTitleBar(Window window) {
		void Apply(object? _, EventArgs __) => ApplyNativeTitleBar(window);
		window.SourceInitialized -= Apply;
		window.Loaded -= Apply;
		window.SourceInitialized += Apply;
		window.Loaded += Apply;
		if (window.IsLoaded)
			ApplyNativeTitleBar(window);
	}

	private static void ApplyNativeTitleBar(Window window) {
		var hwnd = new WindowInteropHelper(window).Handle;
		if (hwnd == IntPtr.Zero)
			return;
		if (!HookedWindows.Add(hwnd))
			return;

		int useDark = 1;
		_ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDark, sizeof(int));
		_ = DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkModeLegacy, ref useDark, sizeof(int));
		uint caption = ColorRef(0x14, 0x14, 0x18);
		uint text = ColorRef(0xec, 0xec, 0xf0);
		uint border = ColorRef(0x3a, 0x3a, 0x48);
		_ = DwmSetWindowAttribute(hwnd, DwmwaCaptionColor, ref caption, sizeof(uint));
		_ = DwmSetWindowAttribute(hwnd, DwmwaTextColor, ref text, sizeof(uint));
		_ = DwmSetWindowAttribute(hwnd, DwmwaBorderColor, ref border, sizeof(uint));
		ForceRedrawFrame(hwnd);
	}

	private static void ForceRedrawFrame(IntPtr hwnd) =>
		SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNozorder | SwpFramechanged);

	private static uint ColorRef(byte r, byte g, byte b) => (uint)(b << 16 | g << 8 | r);

	private const int DwmwaUseImmersiveDarkMode = 20;
	private const int DwmwaUseImmersiveDarkModeLegacy = 19;
	private const int DwmwaBorderColor = 34;
	private const int DwmwaCaptionColor = 35;
	private const int DwmwaTextColor = 36;

	private const uint SwpNomove = 0x0002;
	private const uint SwpNosize = 0x0001;
	private const uint SwpNozorder = 0x0004;
	private const uint SwpFramechanged = 0x0020;

	[DllImport("user32.dll", SetLastError = true)]
	private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("dwmapi.dll", PreserveSig = true)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

	[DllImport("dwmapi.dll", PreserveSig = true)]
	private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref uint pvAttribute, int cbAttribute);

	private static SolidColorBrush Solid(byte r, byte g, byte b) => new(WpfColor.FromRgb(r, g, b));

	private static WpfBrush Freeze(SolidColorBrush brush) {
		if (brush.CanFreeze)
			brush.Freeze();
		return brush;
	}
}
#endif
