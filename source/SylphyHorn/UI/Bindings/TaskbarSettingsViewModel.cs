using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using SylphyHorn.Properties;
using SylphyHorn.Serialization;
using SylphyHorn.Services;

namespace SylphyHorn.UI.Bindings
{
	public sealed class TaskbarSettingsText
	{
		public string this[string key] => Resources.ResourceManager.GetString("Taskbar_" + key, Resources.Culture) ?? key;
	}

	// Edits Settings.Taskbar directly; the taskbar indicator observes those properties and updates live.
	// The settings window saves the file when it closes.
	public sealed class TaskbarSettingsViewModel : ObservableObject
	{
		private static readonly int[] _fontSizes = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 20, 22, 24, 26, 28, 32 };

		private readonly TaskbarSettings _settings;
		private readonly Func<Color> _automaticColor;
		private string _colorText;

		public TaskbarSettingsText Text { get; } = new TaskbarSettingsText();

		public IReadOnlyCollection<DisplayItem<string>> FontFamilies { get; }

		public IReadOnlyCollection<int> FontSizes { get; } = _fontSizes;

		public IReadOnlyCollection<DisplayItem<uint>> Placements { get; }

		public bool IsUnavailable { get; }

		internal TaskbarSettingsViewModel(TaskbarSettings settings, IEnumerable<string> installedFontFamilies, Func<Color> automaticColor, bool isAvailable)
		{
			this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
			this._automaticColor = automaticColor ?? throw new ArgumentNullException(nameof(automaticColor));
			this.IsUnavailable = !isAvailable;
			this.FontFamilies = new[] { new DisplayItem<string> { Display = this.Text["DefaultFont"], } }
				.Concat((installedFontFamilies ?? Enumerable.Empty<string>())
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
					.Select(name => new DisplayItem<string> { Display = name, Value = name, }))
				.ToArray();
			this.Placements = new[]
			{
				new DisplayItem<uint> { Display = this.Text["PlacementBeforeStart"], Value = (uint)TaskbarIndicatorPlacement.BeforeStartButton, },
				new DisplayItem<uint> { Display = this.Text["PlacementBeforeTray"], Value = (uint)TaskbarIndicatorPlacement.BeforeNotificationArea, },
				new DisplayItem<uint> { Display = this.Text["PlacementLeftEdge"], Value = (uint)TaskbarIndicatorPlacement.LeftEdge, },
				new DisplayItem<uint> { Display = this.Text["PlacementAuto"], Value = (uint)TaskbarIndicatorPlacement.Auto, },
			};
			this._colorText = this.StoredColorOrNull();
		}

		public bool IsEnabled
		{
			get => this._settings.Enabled.Value;
			set
			{
				if (this._settings.Enabled.Value == value) return;
				this._settings.Enabled.Value = value;
				this.OnPropertyChanged();
			}
		}

		public string FontFamily
		{
			get => string.IsNullOrWhiteSpace(this._settings.FontFamily.Value) ? null : this._settings.FontFamily.Value;
			set
			{
				var param = string.IsNullOrWhiteSpace(value) ? null : value;
				if (this._settings.FontFamily.Value == param) return;
				this._settings.FontFamily.Value = param;
				this.OnPropertyChanged();
			}
		}

		public int FontSize
		{
			get => TaskbarIndicatorAppearance.ClampFontSize(this._settings.FontSize.Value);
			set
			{
				var param = TaskbarIndicatorAppearance.ClampFontSize(value);
				if (this._settings.FontSize.Value == param) return;
				this._settings.FontSize.Value = param;
				this.OnPropertyChanged();
			}
		}

		public bool IsAutomaticColor
		{
			get => this.StoredColorOrNull() == null;
			set
			{
				if (value == this.IsAutomaticColor) return;
				if (value)
				{
					this._settings.TextColor.Value = null;
				}
				else
				{
					// Start from the last custom color, or from what "automatic" currently shows.
					if (!TaskbarIndicatorAppearance.TryParseColor(this._colorText, out var color)) color = this._automaticColor();
					this._colorText = TaskbarIndicatorAppearance.ToHex(color);
					this._settings.TextColor.Value = this._colorText;
				}
				this.NotifyColorChanged();
			}
		}

		// Free text while typing; only a value that parses as a color is stored.
		public string TextColor
		{
			get => this._colorText ?? TaskbarIndicatorAppearance.ToHex(this._automaticColor());
			set
			{
				if (this._colorText == value) return;
				this._colorText = value;
				if (!this.IsAutomaticColor && TaskbarIndicatorAppearance.TryParseColor(value, out var color))
				{
					this._settings.TextColor.Value = TaskbarIndicatorAppearance.ToHex(color);
				}
				this.OnPropertyChanged();
				this.OnPropertyChanged(nameof(this.TextColorPreview));
			}
		}

		public Brush TextColorPreview
		{
			get
			{
				if (!TaskbarIndicatorAppearance.TryParseColor(this.TextColor, out var color)) return Brushes.Transparent;
				var brush = new SolidColorBrush(color);
				brush.Freeze();
				return brush;
			}
		}

		public uint Placement
		{
			get => (uint)TaskbarIndicatorAppearance.PlacementOrDefault(this._settings.Placement.Value);
			set
			{
				if (this._settings.Placement.Value == value) return;
				this._settings.Placement.Value = value;
				this.OnPropertyChanged();
			}
		}

		// Settings were imported or reset behind this view model.
		internal void Refresh()
		{
			this._colorText = this.StoredColorOrNull();
			this.OnPropertyChanged(string.Empty);
		}

		private string StoredColorOrNull()
			=> TaskbarIndicatorAppearance.TryParseColor(this._settings.TextColor.Value, out var color) ? TaskbarIndicatorAppearance.ToHex(color) : null;

		private void NotifyColorChanged()
		{
			this.OnPropertyChanged(nameof(this.IsAutomaticColor));
			this.OnPropertyChanged(nameof(this.TextColor));
			this.OnPropertyChanged(nameof(this.TextColorPreview));
		}
	}
}
