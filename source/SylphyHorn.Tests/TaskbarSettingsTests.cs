using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using SylphyHorn.Serialization;
using SylphyHorn.Services;
using SylphyHorn.UI.Bindings;
using Xunit;

namespace SylphyHorn.Tests
{
	public sealed class TaskbarSettingsTests
	{
		[Fact]
		public async Task SettingsKeysAndDefaultsRemainStable()
		{
			var settings = new TaskbarSettings(await CreateProviderAsync());

			Assert.Equal("TaskbarSettings.FontFamily", settings.FontFamily.Key);
			Assert.Equal("TaskbarSettings.FontSize", settings.FontSize.Key);
			Assert.Equal("TaskbarSettings.TextColor", settings.TextColor.Key);
			Assert.Equal("TaskbarSettings.Placement", settings.Placement.Key);
			Assert.Null(settings.FontFamily.Value);
			Assert.Equal(12, settings.FontSize.Value);
			Assert.Null(settings.TextColor.Value);
			Assert.Equal((uint)TaskbarIndicatorPlacement.BeforeStartButton, settings.Placement.Value);
		}

		[Theory]
		[InlineData("#FFCC00", true, "#FFCC00")]
		[InlineData(" #ffcc00 ", true, "#FFCC00")]
		[InlineData("Red", true, "#FF0000")]
		[InlineData(null, false, null)]
		[InlineData("", false, null)]
		[InlineData("#12", false, null)]
		[InlineData("not a color", false, null)]
		public void ColorTextParsesOrMeansAutomatic(string value, bool expected, string hex)
		{
			Assert.Equal(expected, TaskbarIndicatorAppearance.TryParseColor(value, out var color));
			if (expected) Assert.Equal(hex, TaskbarIndicatorAppearance.ToHex(color));
		}

		[Theory]
		[InlineData(0, 8)]
		[InlineData(12, 12)]
		[InlineData(22, 22)]
		[InlineData(500, 32)]
		public void FontSizeIsKeptInsideTheTaskbar(int value, int expected)
			=> Assert.Equal(expected, TaskbarIndicatorAppearance.ClampFontSize(value));

		[Fact]
		public void UnknownStoredValuesFallBackToDefaults()
		{
			Assert.Equal(TaskbarSettings.FontFamilyDefaultValue, TaskbarIndicatorAppearance.FontFamilyOrDefault(" "));
			Assert.Equal("Consolas", TaskbarIndicatorAppearance.FontFamilyOrDefault("Consolas"));
			Assert.Equal(TaskbarIndicatorPlacement.BeforeStartButton, TaskbarIndicatorAppearance.PlacementOrDefault(99));
			Assert.Equal(TaskbarIndicatorPlacement.LeftEdge, TaskbarIndicatorAppearance.PlacementOrDefault(1));
		}

		[Fact]
		public async Task ViewModelWritesFontSizeAndPlacementThrough()
		{
			var settings = new TaskbarSettings(await CreateProviderAsync());
			var viewModel = CreateViewModel(settings);

			Assert.Null(viewModel.FontFamilies.First().Value);
			Assert.Equal(new[] { "Arial", "Consolas" }, viewModel.FontFamilies.Skip(1).Select(item => item.Value));

			viewModel.FontFamily = "Consolas";
			viewModel.FontSize = 22;
			viewModel.Placement = (uint)TaskbarIndicatorPlacement.BeforeNotificationArea;
			Assert.Equal("Consolas", settings.FontFamily.Value);
			Assert.Equal(22, settings.FontSize.Value);
			Assert.Equal((uint)TaskbarIndicatorPlacement.BeforeNotificationArea, settings.Placement.Value);

			viewModel.FontFamily = null;
			Assert.Null(settings.FontFamily.Value);
		}

		[Fact]
		public async Task ViewModelKeepsAutomaticColorUntilACustomColorIsValid()
		{
			var settings = new TaskbarSettings(await CreateProviderAsync());
			var viewModel = CreateViewModel(settings);
			Assert.True(viewModel.IsAutomaticColor);

			// Leaving automatic starts from the color automatic was showing.
			viewModel.IsAutomaticColor = false;
			Assert.Equal("#FFFFFF", settings.TextColor.Value);

			// Text that is not a color yet is not stored.
			viewModel.TextColor = "#FFC";
			viewModel.TextColor = "#FFCC0";
			Assert.Equal("#FFFFFF", settings.TextColor.Value);
			viewModel.TextColor = "#FFCC00";
			Assert.Equal("#FFCC00", settings.TextColor.Value);

			// Automatic clears the stored value but the custom color is remembered for this session.
			viewModel.IsAutomaticColor = true;
			Assert.Null(settings.TextColor.Value);
			viewModel.IsAutomaticColor = false;
			Assert.Equal("#FFCC00", settings.TextColor.Value);
		}

		private static TaskbarSettingsViewModel CreateViewModel(TaskbarSettings settings)
			=> new TaskbarSettingsViewModel(settings, new[] { "Consolas", "Arial", "consolas" }, () => Colors.White, true);

		private static async Task<MemoryDictionaryProvider> CreateProviderAsync()
		{
			var provider = new MemoryDictionaryProvider();
			await provider.InitializeAsync();
			return provider;
		}
	}
}
