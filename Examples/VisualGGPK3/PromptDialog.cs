using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class PromptDialog : Dialog<DialogResult> {
	public string Value => textBox.Text ?? "";

	public PromptDialog(string title, string prompt, string initialValue = "") {
		Title = title;
		MinimumSize = new Size(320, 120);
		var textBox = new TextBox { Text = initialValue };
		this.textBox = textBox;
		var promptLabel = new Label { Text = prompt };
		AppTheme.StyleHeaderLabel(promptLabel);
		var ok = new Button { Text = "OK" };
		AppTheme.StyleButton(ok, ThemeButtonVariant.Primary);
		ok.Click += (_, _) => {
			Result = DialogResult.Ok;
			Close();
		};
		var cancel = new Button { Text = "Cancel" };
		AppTheme.StyleButton(cancel);
		cancel.Click += (_, _) => {
			Result = DialogResult.Cancel;
			Close();
		};
		AppTheme.StyleTextInput(textBox);
		BackgroundColor = AppTheme.Surface;
		DefaultButton = ok;
		AbortButton = cancel;
		Content = new TableLayout {
			Padding = 10,
			Spacing = new Size(6, 6),
			Rows = {
				new TableRow(promptLabel),
				new TableRow(textBox),
				new TableRow(null, ok, cancel) { ScaleHeight = false }
			}
		};
		textBox.KeyDown += (_, e) => {
			if (e.Key == Keys.Enter) {
				e.Handled = true;
				Result = DialogResult.Ok;
				Close();
			}
		};
		Shown += (_, _) => textBox.Focus();
		AppTheme.ApplyTitleBar(this);
	}

	private readonly TextBox textBox;
}
