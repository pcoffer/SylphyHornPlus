using System.Drawing;
using System.Threading.Tasks;
using SylphyHorn.Serialization;
using SylphyHorn.UI;
using Xunit;

namespace SylphyHorn.Tests
{
	public sealed class SettingsWindowPlacementStoreTests
	{
		private static readonly Rectangle Primary = new Rectangle(0, 0, 1920, 1040);
		private static readonly Rectangle Secondary = new Rectangle(1920, 200, 1280, 984);
		private static readonly Rectangle OnSecondary = Rectangle.FromLTRB(2000, 300, 2760, 1060);

		[Fact]
		public async Task NothingIsRestoredBeforeAPlacementWasSaved()
		{
			var store = new SettingsWindowPlacementStore(new WindowPlacementSettings(await CreateProviderAsync()), () => new[] { Primary });

			Assert.Null(store.Bounds);
		}

		[Fact]
		public async Task SavedPlacementIsRestoredByANewStore()
		{
			var settings = new WindowPlacementSettings(await CreateProviderAsync());
			var store = new SettingsWindowPlacementStore(settings, () => new[] { Primary, Secondary });
			store.SetPlacement(OnSecondary, maximized: true);
			store.Save();

			var restored = new SettingsWindowPlacementStore(settings, () => new[] { Primary, Secondary });

			Assert.Equal(OnSecondary, restored.Bounds);
			Assert.True(restored.IsMaximized);
		}

		[Fact]
		public async Task PlacementOnAMonitorThatIsGoneIsNotRestored()
		{
			var settings = new WindowPlacementSettings(await CreateProviderAsync());
			var store = new SettingsWindowPlacementStore(settings, () => new[] { Primary, Secondary });
			store.SetPlacement(OnSecondary, maximized: false);
			store.Save();

			Assert.Null(new SettingsWindowPlacementStore(settings, () => new[] { Primary }).Bounds);

			// The saved values are kept, so the placement comes back with the monitor.
			Assert.Equal(OnSecondary, new SettingsWindowPlacementStore(settings, () => new[] { Primary, Secondary }).Bounds);
		}

		[Theory]
		[InlineData(100, 100, true)]      // fully on the primary monitor
		[InlineData(1700, 100, true)]     // spans both monitors
		[InlineData(-700, 100, false)]    // only 60 px of the title bar is on screen
		[InlineData(-600, 100, true)]     // 160 px of the title bar is on screen
		[InlineData(100, -20, false)]     // title bar is above the screen
		[InlineData(100, 1030, false)]    // title bar is below the work area
		[InlineData(5000, 100, false)]    // beyond every monitor
		[InlineData(2000, 0, false)]      // above the lower, secondary monitor
		public void WindowIsReachableWhenItsTitleBarCanBeGrabbed(int left, int top, bool expected)
			=> Assert.Equal(expected, SettingsWindowPlacementStore.IsReachable(new Rectangle(left, top, 760, 760), new[] { Primary, Secondary }));

		private static async Task<MemoryDictionaryProvider> CreateProviderAsync()
		{
			var provider = new MemoryDictionaryProvider();
			await provider.InitializeAsync();
			return provider;
		}
	}
}
