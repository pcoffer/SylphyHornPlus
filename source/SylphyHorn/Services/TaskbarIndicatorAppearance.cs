using System;
using System.Windows.Media;
using SylphyHorn.Serialization;

namespace SylphyHorn.Services
{
	internal static class TaskbarIndicatorAppearance
	{
		// False means "automatic": the caller follows the Windows theme.
		internal static bool TryParseColor(string value, out Color color)
		{
			color = default(Color);
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();

			// Only the full #RRGGBB form: the shorthand forms WPF also accepts (#RGB, #ARGB) would
			// turn a half-typed value into a color.
			if (value[0] == '#' && value.Length != 7) return false;
			try
			{
				if (ColorConverter.ConvertFromString(value) is Color parsed)
				{
					color = parsed;
					return true;
				}
			}
			catch (FormatException)
			{
			}
			return false;
		}

		internal static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

		internal static int ClampFontSize(int value)
			=> Math.Max(TaskbarSettings.FontSizeMinValue, Math.Min(TaskbarSettings.FontSizeMaxValue, value));

		internal static string FontFamilyOrDefault(string value)
			=> string.IsNullOrWhiteSpace(value) ? TaskbarSettings.FontFamilyDefaultValue : value;

		internal static TaskbarIndicatorPlacement PlacementOrDefault(uint value)
			=> Enum.IsDefined(typeof(TaskbarIndicatorPlacement), value)
				? (TaskbarIndicatorPlacement)value
				: (TaskbarIndicatorPlacement)TaskbarSettings.PlacementDefaultValue;
	}
}
