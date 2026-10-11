using System;
using System.Collections.Generic;
using SylphyHorn.Services;
using SylphyHorn.Services.DesktopTransitions;
using WindowsDesktop;
using Xunit;

namespace SylphyHorn.Tests
{
	public sealed class DesktopIndicatorTests
	{
		private static readonly Guid A = Guid.Parse("11111111-1111-1111-1111-111111111111");
		private static readonly Guid B = Guid.Parse("22222222-2222-2222-2222-222222222222");
		private static readonly Guid C = Guid.Parse("33333333-3333-3333-3333-333333333333");

		[Theory]
		[InlineData("Development", "3 · Development")]
		[InlineData(null, "Desktop 3")]
		[InlineData("", "Desktop 3")]
		[InlineData(" ", "Desktop 3")]
		public void TextShowsNumberAndNameAndFallsBackForUnnamedDesktops(string name, string expected)
			=> Assert.Equal(expected, DesktopIndicatorTextFormatter.Create(3, name));

		[Fact]
		public void TextFollowsTheCurrentDesktopOrderAndName()
		{
			var state = State(C, Record(A, "Mail"), Record(B, null), Record(C, "Development"));
			Assert.Equal("3 · Development", DesktopIndicatorService.CreateText(state));

			var reordered = State(C, Record(C, "Development"), Record(A, "Mail"), Record(B, null));
			Assert.Equal("1 · Development", DesktopIndicatorService.CreateText(reordered));

			var unnamed = State(B, Record(A, "Mail"), Record(B, null), Record(C, "Development"));
			Assert.Equal("Desktop 2", DesktopIndicatorService.CreateText(unnamed));
		}

		[Fact]
		public void TextIsUnavailableWithoutACurrentDesktop()
		{
			Assert.Null(DesktopIndicatorService.CreateText(null));
			Assert.Null(DesktopIndicatorService.CreateText(State(null, Record(A, "Mail"))));
		}

		private static DesktopRecord Record(Guid id, string name)
			=> new DesktopRecord(
				id,
				name == null ? DesktopPropertyState.Unknown(VirtualDesktopReadStatus.NotAttempted) : DesktopPropertyState.Provider(name),
				DesktopPropertyState.Unknown(VirtualDesktopReadStatus.NotAttempted),
				WallpaperPosition.Fill,
				DesktopRecordOrigin.SeededExistingRecord);

		private static DesktopRuntimeState State(Guid? current, params DesktopRecord[] records)
		{
			var order = new List<Guid>();
			var map = new Dictionary<Guid, DesktopRecord>();
			foreach (var record in records)
			{
				order.Add(record.Id);
				map[record.Id] = record;
			}
			return new DesktopRuntimeState(1, 1, 1, current, order, map);
		}
	}
}
