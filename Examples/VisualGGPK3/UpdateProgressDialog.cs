using System;

using Eto.Drawing;
using Eto.Forms;

namespace VisualGGPK3;

internal sealed class UpdateProgressDialog : Dialog<DialogResult> {
	private readonly Label statusLabel = new() {
		Text = "Checking for updates…",
		TextColor = AppTheme.TextPrimary,
		Wrap = WrapMode.Word
	};
	private readonly ProgressBar progressBar = new() { Height = 18 };
	private readonly Button cancelButton = new() { Text = "Cancel" };

	public event Action? CancelRequested;

	public UpdateProgressDialog() {
		Title = "VisualGGPK3 Update";
		MinimumSize = new Size(420, 140);
		ClientSize = new Size(420, 140);
		Resizable = false;
		BackgroundColor = AppTheme.Surface;
		AppTheme.StyleButton(cancelButton);
		cancelButton.Click += (_, _) => {
			CancelRequested?.Invoke();
			Result = DialogResult.Cancel;
			Close();
		};
		AbortButton = cancelButton;
		Content = new TableLayout {
			Padding = 16,
			Spacing = new Size(8, 10),
			Rows = {
				new TableRow(statusLabel),
				new TableRow(progressBar) { ScaleHeight = false },
				new TableRow(null, cancelButton) { ScaleHeight = false }
			}
		};
		AppTheme.ApplyTitleBar(this);
	}

	public void Report(UpdateProgress progress) {
		Application.Instance.AsyncInvoke(() => {
			statusLabel.Text = progress.Message;
			if (progress.Fraction is double fraction)
				progressBar.Indeterminate = false;
			else
				progressBar.Indeterminate = true;
			if (progress.Fraction is double value)
				progressBar.Value = (int)Math.Round(Math.Clamp(value, 0, 1) * 100);
		});
	}

	public void SetComplete(string message) {
		Application.Instance.AsyncInvoke(() => {
			statusLabel.Text = message;
			progressBar.Indeterminate = false;
			progressBar.Value = 100;
			cancelButton.Text = "Close";
		});
	}

	public void SetError(string message) {
		Application.Instance.AsyncInvoke(() => {
			statusLabel.Text = message;
			progressBar.Indeterminate = false;
			progressBar.Value = 0;
			cancelButton.Text = "Close";
		});
	}
}
