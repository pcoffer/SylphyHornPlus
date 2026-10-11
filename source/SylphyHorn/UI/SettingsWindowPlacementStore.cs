using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using MetroRadiance.Interop.Win32;
using MetroRadiance.UI.Controls;
using SylphyHorn.Serialization;

namespace SylphyHorn.UI
{
	// MetroWindow saves its placement on close and restores it on open through IWindowSettings.
	// This keeps the settings window's placement in the application's own settings file, and
	// withholds a saved placement that no current monitor can show so the window opens in the
	// default position instead.
	internal sealed class SettingsWindowPlacementStore : IWindowSettings
	{
		private const int _captionHeight = 32;
		private const int _minimumReachableWidth = 100;

		private readonly WindowPlacementSettings _settings;
		private readonly Func<IEnumerable<Rectangle>> _workAreas;

		public WINDOWPLACEMENT? Placement { get; set; }

		internal SettingsWindowPlacementStore(WindowPlacementSettings settings)
			: this(settings, () => System.Windows.Forms.Screen.AllScreens.Select(screen => screen.WorkingArea))
		{
		}

		internal SettingsWindowPlacementStore(WindowPlacementSettings settings, Func<IEnumerable<Rectangle>> workAreas)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
			this._workAreas = workAreas ?? throw new ArgumentNullException(nameof(workAreas));
			this.Reload();
		}

		public void Reload()
		{
			var bounds = Rectangle.FromLTRB(
				this._settings.SettingsWindowLeft.Value,
				this._settings.SettingsWindowTop.Value,
				this._settings.SettingsWindowRight.Value,
				this._settings.SettingsWindowBottom.Value);
			if (bounds.Width <= 0 || bounds.Height <= 0 || !IsReachable(bounds, this._workAreas()))
			{
				this.Placement = null;
				return;
			}

			this.SetPlacement(bounds, this._settings.SettingsWindowMaximized.Value);
		}

		internal Rectangle? Bounds
		{
			get
			{
				if (!this.Placement.HasValue) return null;
				var position = this.Placement.Value.normalPosition;
				return Rectangle.FromLTRB(position.Left, position.Top, position.Right, position.Bottom);
			}
		}

		internal bool IsMaximized => this.Placement?.showCmd == ShowWindowFlags.SW_SHOWMAXIMIZED;

		internal void SetPlacement(Rectangle bounds, bool maximized)
		{
			this.Placement = new WINDOWPLACEMENT
			{
				length = Marshal.SizeOf(typeof(WINDOWPLACEMENT)),
				showCmd = maximized ? ShowWindowFlags.SW_SHOWMAXIMIZED : ShowWindowFlags.SW_SHOWNORMAL,
				minPosition = new POINT { X = -1, Y = -1, },
				maxPosition = new POINT { X = -1, Y = -1, },
				normalPosition = new RECT { Left = bounds.Left, Top = bounds.Top, Right = bounds.Right, Bottom = bounds.Bottom, },
			};
		}

		public void Save()
		{
			if (!this.Placement.HasValue) return;

			var placement = this.Placement.Value;
			this._settings.SettingsWindowLeft.Value = placement.normalPosition.Left;
			this._settings.SettingsWindowTop.Value = placement.normalPosition.Top;
			this._settings.SettingsWindowRight.Value = placement.normalPosition.Right;
			this._settings.SettingsWindowBottom.Value = placement.normalPosition.Bottom;
			this._settings.SettingsWindowMaximized.Value = placement.showCmd == ShowWindowFlags.SW_SHOWMAXIMIZED;
		}

		// A window is reachable when enough of its title bar is on a monitor to grab and drag it.
		internal static bool IsReachable(Rectangle bounds, IEnumerable<Rectangle> workAreas)
		{
			var caption = new Rectangle(bounds.Left, bounds.Top, bounds.Width, Math.Min(bounds.Height, _captionHeight));
			foreach (var area in workAreas)
			{
				var visible = Rectangle.Intersect(caption, area);
				if (visible.Width >= Math.Min(_minimumReachableWidth, caption.Width) && visible.Height * 2 >= caption.Height) return true;
			}
			return false;
		}
	}
}
