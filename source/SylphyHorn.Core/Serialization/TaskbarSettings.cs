using System;
using MetroTrilithon.Serialization;

namespace SylphyHorn.Serialization
{
	public enum TaskbarIndicatorPlacement : uint
	{
		Auto = 0,
		LeftEdge = 1,
		BeforeStartButton = 2,
		BeforeNotificationArea = 3,
	}

	public sealed class TaskbarSettings : SettingsHost
	{
		private readonly ISerializationProvider _provider;

		public TaskbarSettings(ISerializationProvider provider)
		{
			this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public SerializableProperty<bool> Enabled => this.Cache(key => new SerializableProperty<bool>(key, this._provider, EnabledDefaultValue));

		// Null or empty uses FontFamilyDefaultValue.
		public SerializableProperty<string> FontFamily => this.Cache(key => new SerializableProperty<string>(key, this._provider));

		public SerializableProperty<int> FontSize => this.Cache(key => new SerializableProperty<int>(key, this._provider, FontSizeDefaultValue));

		// Null or empty follows the Windows theme; otherwise a color such as "#FFCC00".
		public SerializableProperty<string> TextColor => this.Cache(key => new SerializableProperty<string>(key, this._provider));

		public SerializableProperty<uint> Placement => this.Cache(key => new SerializableProperty<uint>(key, this._provider, PlacementDefaultValue));

		#region default values

		public static bool EnabledDefaultValue { get; } = true;

		public static string FontFamilyDefaultValue { get; } = "Segoe UI Variable Text, Segoe UI";

		public static int FontSizeDefaultValue { get; } = 12 /* px */;

		public static int FontSizeMinValue { get; } = 8 /* px */;

		public static int FontSizeMaxValue { get; } = 32 /* px */;

		public static uint PlacementDefaultValue { get; } = (uint)TaskbarIndicatorPlacement.BeforeStartButton;

		#endregion
	}
}
