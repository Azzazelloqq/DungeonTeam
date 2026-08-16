using System;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.UI.WorldMap;
using NUnit.Framework;

namespace Code.ApplicationRoot.Tests.EditMode
{
    public sealed class ApplicationFlowPolicyTests
    {
        [Test]
        public void TransitionGate_AllowsOneTransitionAndCommitsState()
        {
            var gate = new ApplicationTransitionGate(PlayerFlowState.GuildHall);

            Assert.That(gate.TryBegin(PlayerFlowState.GuildHall, out var lease), Is.True);
            Assert.That(gate.TryBegin(PlayerFlowState.GuildHall, out _), Is.False);

            lease.Complete(PlayerFlowState.WorldMap);

            Assert.That(gate.State, Is.EqualTo(PlayerFlowState.WorldMap));
            Assert.That(gate.TryBegin(PlayerFlowState.WorldMap, out var next), Is.True);
            next.Dispose();
        }

        [Test]
        public void TransitionGate_DisposedDuringTransition_StaysDisposedAndRejectsRequests()
        {
            var gate = new ApplicationTransitionGate(PlayerFlowState.WorldMap);
            Assert.That(gate.TryBegin(PlayerFlowState.WorldMap, out var lease), Is.True);

            gate.Dispose();
            lease.Dispose();

            Assert.That(gate.State, Is.EqualTo(PlayerFlowState.Disposed));
            Assert.That(gate.TryBegin(PlayerFlowState.Disposed, out _), Is.False);
        }

        [Test]
        public void TransitionGate_FailedRecoveryAfterOutgoingOwnerWasDestroyed_TransitionsToFaulted()
        {
            var gate = new ApplicationTransitionGate(PlayerFlowState.DungeonRun);

            Assert.That(gate.TryBegin(PlayerFlowState.DungeonRun, out var lease), Is.True);
            lease.Complete(PlayerFlowState.Faulted);

            Assert.That(gate.State, Is.EqualTo(PlayerFlowState.Faulted));
            Assert.That(gate.TryBegin(PlayerFlowState.DungeonRun, out _), Is.False);
        }

        [Test]
        public void DestinationResolver_ValidContract_CreatesConfiguredRequest()
        {
            var session = new GuildSessionState();
            session.SelectContract("contract.one");
            var resolver = CreateResolver(
                Location("location.dungeon", WorldLocationDestinationKind.DungeonRun, "preset.one"),
                Offer("contract.one", "location.dungeon", isAvailable: true),
                session);

            var destination = resolver.Resolve("location.dungeon");

            Assert.That(destination.IsGuildHall, Is.False);
            Assert.That(destination.IsUnavailable, Is.False);
            Assert.That(destination.Request.Dungeon.DungeonId, Is.EqualTo("dungeon.one"));
            Assert.That(destination.Request.Dungeon.Seed, Is.EqualTo(17));
        }

        [Test]
        public void DestinationResolver_MissingSelection_RejectsDungeon()
        {
            var resolver = CreateResolver(
                Location("location.dungeon", WorldLocationDestinationKind.DungeonRun, "preset.one"),
                Offer("contract.one", "location.dungeon", isAvailable: true),
                new GuildSessionState());

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve("location.dungeon"));
        }

        [Test]
        public void DestinationResolver_MismatchedContract_RejectsDungeon()
        {
            var session = new GuildSessionState();
            session.SelectContract("contract.one");
            var resolver = CreateResolver(
                Location("location.dungeon", WorldLocationDestinationKind.DungeonRun, "preset.one"),
                Offer("contract.one", "location.other", isAvailable: true),
                session);

            Assert.Throws<InvalidOperationException>(() => resolver.Resolve("location.dungeon"));
        }

        [Test]
        public void DestinationResolver_UnavailableLocation_ReturnsNoTransition()
        {
            var resolver = CreateResolver(
                Location(
                    "location.disabled",
                    WorldLocationDestinationKind.GuildHall,
                    destinationId: null,
                    isAvailable: false),
                Offer("contract.one", "location.other", isAvailable: true),
                new GuildSessionState());

            Assert.That(resolver.Resolve("location.disabled").IsUnavailable, Is.True);
        }

        private static WorldMapDestinationResolver CreateResolver(
            WorldLocationSnapshot location,
            NoticeBoardOfferSnapshot offer,
            GuildSessionState session)
        {
            var locations = new WorldMapCatalog(
                new[] { location },
                new WorldMapUiTextSnapshot(MapText("map.title"), MapText("map.back"), MapText("map.empty")));
            var contracts = new ContractCatalog(new[] { offer });
            var presets = new DungeonRunLaunchPresetCatalog(
                new[]
                {
                    new DungeonRunLaunchPreset(
                        "preset.one",
                        "Preset",
                        "dungeon.one",
                        "scenario.one",
                        "difficulty.one",
                        17)
                },
                "preset.one");
            var team = new DungeonRunTeamSelection(
                new DungeonRunActorSelection("actor.leader", 1, "loadout.default"),
                Array.Empty<DungeonRunActorSelection>());
            return new WorldMapDestinationResolver(locations, contracts, session, presets, team);
        }

        private static WorldLocationSnapshot Location(
            string id,
            WorldLocationDestinationKind kind,
            string destinationId,
            bool isAvailable = true)
        {
            return new WorldLocationSnapshot(
                id,
                MapText($"{id}.title"),
                MapText($"{id}.description"),
                isAvailable,
                isAvailable ? null : MapText($"{id}.disabled"),
                kind,
                destinationId);
        }

        private static NoticeBoardOfferSnapshot Offer(string id, string locationId, bool isAvailable)
        {
            return new NoticeBoardOfferSnapshot(
                id,
                GuildText($"{id}.title"),
                GuildText($"{id}.summary"),
                locationId,
                isAvailable,
                isAvailable ? null : GuildText($"{id}.disabled"));
        }

        private static WorldMapTextSnapshot MapText(string id) => new(id, id);
        private static GuildTextSnapshot GuildText(string id) => new(id, id);
    }
}
