using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using MetroRadiance.Interop.Win32;

namespace SylphyHorn.UI
{
	partial class TaskbarIndicatorWindow
	{
		public TaskbarIndicatorWindow()
		{
			this.InitializeComponent();
		}

		internal FrameworkElement ContentRoot => this.Root;

		// Returns the width the text needs, in device-independent pixels.
		internal double Update(string text, Brush foreground, FontFamily fontFamily, double fontSize)
		{
			this.Label.Text = text;
			this.Label.Foreground = foreground;
			this.Label.FontFamily = fontFamily;
			this.Label.FontSize = fontSize;
			this.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
			return Math.Ceiling(this.Label.DesiredSize.Width);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			// The handle is created before the window is shown, so there is no PresentationSource yet.
			var handle = new WindowInteropHelper(this).Handle;

			// Hardware rendering of this window, once it is hosted inside the taskbar, sends WPF's render
			// thread into an endless swap chain rebuild that leaks memory. A line of text needs no GPU.
			var source = HwndSource.FromHwnd(handle);
			if (source?.CompositionTarget != null) source.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

			// The indicator is display-only: it must not take focus or swallow taskbar clicks.
			var style = User32.GetWindowLongEx(handle);
			style |= WindowExStyles.WS_EX_TOOLWINDOW | WindowExStyles.WS_EX_NOACTIVATE | WindowExStyles.WS_EX_TRANSPARENT;
			User32.SetWindowLongEx(handle, style);
		}
	}
}
