using System;
using NUnit.Framework;

namespace DungeonTeam.UI.WorldMap.Tests.EditMode
{
    public sealed class WorldMapViewModelTests
    {
        [Test]
        public void StartContext_CopiesVariableLocationSnapshot()
        {
            var source = new[] { Location("location.one"), Location("location.two") };
            var context = new WorldMapStartContext(source, Texts());

            source[0] = Location("location.changed");

            Assert.That(context.Locations.Count, Is.EqualTo(2));
            Assert.That(context.Locations[0].LocationId, Is.EqualTo("location.one"));
            Assert.That(context.Locations[1].LocationId, Is.EqualTo("location.two"));
        }

        [Test]
        public void Select_AvailableLocation_PublishesStableIdOnce()
        {
            var selectedCount = 0;
            string selectedId = null;
            using var viewModel = CreateViewModel(
                new[] { Location("location.one"), Location("location.two") },
                id =>
                {
                    selectedCount++;
                    selectedId = id;
                });

            viewModel.Items[1].Select();
            viewModel.Items[0].Select();

            Assert.That(selectedCount, Is.EqualTo(1));
            Assert.That(selectedId, Is.EqualTo("location.two"));
            Assert.That(viewModel.IsInteractionBlocked, Is.True);
        }

        [Test]
        public void Select_UnavailableLocation_DoesNotPublishOrBlock()
        {
            var selectedCount = 0;
            using var viewModel = CreateViewModel(
                new[] { Location("location.disabled", isAvailable: false) },
                _ => selectedCount++);

            viewModel.Items[0].Select();

            Assert.That(selectedCount, Is.Zero);
            Assert.That(viewModel.IsInteractionBlocked, Is.False);
        }

        [Test]
        public void BackAndSelection_AreMutuallyOneShotUntilRecovery()
        {
            var backCount = 0;
            var selectionCount = 0;
            using var viewModel = CreateViewModel(
                new[] { Location("location.one") },
                _ => selectionCount++,
                () => backCount++);

            viewModel.RequestBack();
            viewModel.Items[0].Select();
            Assert.That(backCount, Is.EqualTo(1));
            Assert.That(selectionCount, Is.Zero);

            Assert.That(viewModel.RestoreInteraction(), Is.True);
            viewModel.Items[0].Select();
            Assert.That(selectionCount, Is.EqualTo(1));
        }

        [Test]
        public void EmptyContext_IsSupported()
        {
            using var viewModel = CreateViewModel(Array.Empty<WorldLocationSnapshot>(), _ => { });

            Assert.That(viewModel.Items, Is.Empty);
        }

        private static WorldMapViewModel CreateViewModel(
            WorldLocationSnapshot[] locations,
            Action<string> selected,
            Action back = null)
        {
            return new WorldMapViewModel(
                new WorldMapStartContext(locations, Texts()),
                selected,
                back ?? (() => { }));
        }

        private static WorldLocationSnapshot Location(string id, bool isAvailable = true)
        {
            return new WorldLocationSnapshot(
                id,
                Text($"{id}.title"),
                Text($"{id}.description"),
                isAvailable,
                isAvailable ? null : Text($"{id}.disabled"),
                WorldLocationDestinationKind.GuildHall,
                null);
        }

        private static WorldMapUiTextSnapshot Texts()
        {
            return new WorldMapUiTextSnapshot(Text("map.title"), Text("map.back"), Text("map.empty"));
        }

        private static WorldMapTextSnapshot Text(string id)
        {
            return new WorldMapTextSnapshot(id, id);
        }
    }
}
