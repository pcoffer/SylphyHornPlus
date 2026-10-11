using System;
using MetroTrilithon.Serialization;

namespace SylphyHorn.Serialization
{
	// Where the settings window was when it was last closed, in the pixel coordinates
	// Windows reports for a window placement. An empty rectangle means "never saved".
	public sealed class WindowPlacementSettings : SettingsHost
	{
		private readonly ISerializationProvider _provider;

		public WindowPlacementSettings(ISerializationProvider provider)
		{
			this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}

		public SerializableProperty<int> SettingsWindowLeft => this.Cache(key => new SerializableProperty<int>(key, this._provider));

		public SerializableProperty<int> SettingsWindowTop => this.Cache(key => new SerializableProperty<int>(key, this._provider));

		public SerializableProperty<int> SettingsWindowRight => this.Cache(key => new SerializableProperty<int>(key, this._provider));

		public SerializableProperty<int> SettingsWindowBottom => this.Cache(key => new SerializableProperty<int>(key, this._provider));

		public SerializableProperty<bool> SettingsWindowMaximized => this.Cache(key => new SerializableProperty<bool>(key, this._provider));
	}
}
