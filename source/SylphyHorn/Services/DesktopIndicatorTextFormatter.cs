namespace SylphyHorn.Services
{
	internal static class DesktopIndicatorTextFormatter
	{
		internal static string Create(int number, string name)
			=> string.IsNullOrWhiteSpace(name) ? "Desktop " + number : $"{number} · {name}";
	}
}
