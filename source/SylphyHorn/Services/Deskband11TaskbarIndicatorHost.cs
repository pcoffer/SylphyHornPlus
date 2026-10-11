#if !NETFRAMEWORK
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using Deskband11Lib.Core;
using Deskband11Lib.Wpf;
using MetroRadiance.Platform;
using SylphyHorn.Serialization;
using SylphyHorn.UI;

namespace SylphyHorn.Services
{
	// Hosts the indicator inside the Windows 11 taskbar through Deskband11Lib, which
	// reparents a WPF window under Shell_TrayWnd and keeps it clear of the taskbar buttons.
	[SupportedOSPlatform("windows10.0.22000.0")]
	internal sealed class Deskband11TaskbarIndicatorHost : ITaskbarIndicatorHost
	{
		// Width limit at the default font size; larger fonts get proportionally more room.
		private const double _maxWidth = 320;
		private static readonly TimeSpan _recreationDelay = TimeSpan.FromSeconds(1);

		private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
		private readonly TaskbarSettings _settings;
		private readonly IDisposable _settingsSubscription;
		private TaskbarIndicatorWindow _window;
		private TaskbarContentHost _host;
		private TaskbarContentHostOptions _options;
		private string _text = string.Empty;
		private bool _recreating;
		private bool _disposed;

		internal Deskband11TaskbarIndicatorHost(TaskbarSettings settings)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
			WindowsTheme.SystemTheme.Changed += this.OnSystemThemeChanged;
			this._settingsSubscription = SettingsService.ObserveTaskbarIndicator(settings, this.RequestApply);
			if (settings.Enabled.Value) this.Create();
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
				AnimateLayoutChanges = false,
				// Each refresh scans the taskbar with UI Automation; the default 500 ms costs about 5% of a core.
				// Text changes refresh the layout themselves, so this only delays following the tray area.
				LayoutRefreshInterval = TimeSpan.FromSeconds(2),
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
			this._recreating = true;
			this.Release();

			await Task.Delay(_recreationDelay);
			this._recreating = false;
			if (this._disposed || this._host != null || !this._settings.Enabled.Value) return;
			try
			{
				this.Create();
			}
			catch (Exception ex)
			{
				LoggingService.Instance.Register(ex);
			}
		}

		// Brings the taskbar in line with the settings and the current text: removes the indicator
		// when it is turned off, creates it when it is turned on, and otherwise restyles it.
		private void Apply()
		{
			if (this._disposed) return;
			if (!this._settings.Enabled.Value)
			{
				if (this._window != null) this.Release();
				return;
			}
			if (this._window == null)
			{
				// A pending recreation creates the window itself once the taskbar has settled.
				if (!this._recreating) this.Create();
				return;
			}

			var fontSize = TaskbarIndicatorAppearance.ClampFontSize(this._settings.FontSize.Value);
			var fontFamily = new FontFamily(TaskbarIndicatorAppearance.FontFamilyOrDefault(this._settings.FontFamily.Value));
			this._options.Placement = ToPlacement(TaskbarIndicatorAppearance.PlacementOrDefault(this._settings.Placement.Value));

			var width = this._window.Update(this._text, this.ResolveForeground(), fontFamily, fontSize);
			this._options.PreferredWidth = Math.Min(_maxWidth * Math.Max(1.0, fontSize / (double)TaskbarSettings.FontSizeDefaultValue), width);
			if (this._host.IsAttached) this._host.RefreshLayout();
		}

		private Brush ResolveForeground()
		{
			if (!TaskbarIndicatorAppearance.TryParseColor(this._settings.TextColor.Value, out var color))
			{
				return WindowsTheme.SystemTheme.Current == Theme.Light ? Brushes.Black : Brushes.White;
			}
			var brush = new SolidColorBrush(color);
			brush.Freeze();
			return brush;
		}

		private static TaskbarContentPlacement ToPlacement(TaskbarIndicatorPlacement placement)
		{
			switch (placement)
			{
				case TaskbarIndicatorPlacement.Auto: return TaskbarContentPlacement.Auto;
				case TaskbarIndicatorPlacement.LeftEdge: return TaskbarContentPlacement.LeftEdge;
				case TaskbarIndicatorPlacement.BeforeNotificationArea: return TaskbarContentPlacement.BeforeNotificationArea;
				default: return TaskbarContentPlacement.BeforeStartButton;
			}
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

		private void OnSystemThemeChanged(object sender, Theme e) => this.RequestApply();

		private void RequestApply()
		{
			if (this._disposed || this._dispatcher.HasShutdownStarted) return;
			this._dispatcher.BeginInvoke(new Action(this.Apply));
		}

		public void Dispose()
		{
			if (this._disposed) return;
			this._disposed = true;
			WindowsTheme.SystemTheme.Changed -= this.OnSystemThemeChanged;
			this._settingsSubscription.Dispose();
			this.Release();
		}
	}
}
#endif
