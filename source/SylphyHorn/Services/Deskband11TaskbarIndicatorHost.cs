#if !NETFRAMEWORK
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Deskband11Lib.Core;
using Deskband11Lib.Wpf;
using MetroRadiance.Platform;
using SylphyHorn.UI;

namespace SylphyHorn.Services
{
	// Hosts the indicator inside the Windows 11 taskbar through Deskband11Lib, which
	// reparents a WPF window under Shell_TrayWnd and keeps it clear of the taskbar buttons.
	[SupportedOSPlatform("windows10.0.22000.0")]
	internal sealed class Deskband11TaskbarIndicatorHost : ITaskbarIndicatorHost
	{
		private const double _maxWidth = 320;
		private static readonly TimeSpan _recreationDelay = TimeSpan.FromSeconds(1);

		private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		private TaskbarIndicatorWindow _window;
		private TaskbarContentHost _host;
		private TaskbarContentHostOptions _options;
		private string _text = string.Empty;
		private bool _disposed;

		internal Deskband11TaskbarIndicatorHost()
		{
			WindowsTheme.SystemTheme.Changed += this.OnSystemThemeChanged;
			this.Create();
		}

		public void SetText(string text)
		{
			if (this._disposed) return;
			this._text = text ?? string.Empty;
			this.Apply();
		}

		private async void Create()
		{
			var window = new TaskbarIndicatorWindow();
			var options = new TaskbarContentHostOptions
			{
				Placement = TaskbarContentPlacement.BeforeNotificationArea,
				AnimateLayoutChanges = false,
			};
			var host = new TaskbarContentHost(window, window.ContentRoot, options);
			host.TaskbarWindowRecreationRequired += this.OnTaskbarWindowRecreationRequired;
			this._window = window;
			this._host = host;
			this._options = options;
			this.Apply();

			try
			{
				await host.AttachWhenLayoutReadyAsync();
				if (ReferenceEquals(this._host, host)) window.Show();
			}
			catch (Exception ex)
			{
				// A host released while it was still waiting for the taskbar fails by design.
				if (!ReferenceEquals(this._host, host)) return;
				LoggingService.Instance.Register(ex);
				this.Release();
			}
		}

		// Explorer restarted, the taskbar's monitor went away, or the taskbar DPI changed.
		// The hosted window cannot be reused in any of these cases.
		private async void OnTaskbarWindowRecreationRequired(object sender, EventArgs e)
		{
			if (!ReferenceEquals(sender, this._host)) return;
			this.Release();

			await Task.Delay(_recreationDelay);
			if (this._disposed || this._host != null) return;
			try
			{
				this.Create();
			}
			catch (Exception ex)
			{
				LoggingService.Instance.Register(ex);
			}
		}

		private void Apply()
		{
			if (this._window == null) return;
			var foreground = WindowsTheme.SystemTheme.Current == Theme.Light ? Brushes.Black : Brushes.White;
			this._options.PreferredWidth = Math.Min(_maxWidth, this._window.Update(this._text, foreground));
			if (this._host.IsAttached) this._host.RefreshLayout();
		}

		private void Release()
		{
			var window = this._window;
			var host = this._host;
			this._window = null;
			this._host = null;
			this._options = null;
			host?.Dispose();
			if (window.IsWindowAlive()) window.Close();
		}

		private void OnSystemThemeChanged(object sender, Theme e)
		{
			if (this._dispatcher.HasShutdownStarted) return;
			this._dispatcher.BeginInvoke(new Action(this.Apply));
		}

		public void Dispose()
		{
			if (this._disposed) return;
			this._disposed = true;
			WindowsTheme.SystemTheme.Changed -= this.OnSystemThemeChanged;
			this.Release();
		}
	}
}
#endif
