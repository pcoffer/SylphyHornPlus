using System.Windows;
using System.Windows.Controls;
using SylphyHorn.Services;
using SylphyHorn.UI.Bindings;

namespace SylphyHorn.UI
{
	public partial class TaskbarSettingsView : UserControl
	{
		public TaskbarSettingsView()
		{
			this.InitializeComponent();
		}

		private void ChooseColor(object sender, RoutedEventArgs args)
		{
			if (!(this.DataContext is TaskbarSettingsViewModel viewModel)) return;

			using (var dialog = new System.Windows.Forms.ColorDialog { FullOpen = true, })
			{
				if (TaskbarIndicatorAppearance.TryParseColor(viewModel.TextColor, out var current))
				{
					dialog.Color = System.Drawing.Color.FromArgb(current.R, current.G, current.B);
				}
				if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

				var chosen = dialog.Color;
				viewModel.TextColor = TaskbarIndicatorAppearance.ToHex(System.Windows.Media.Color.FromRgb(chosen.R, chosen.G, chosen.B));
			}
		}
	}
}
