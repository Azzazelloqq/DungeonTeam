using System;
using DungeonTeam.Gameplay.Contracts.Domain;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.Rewards.Runtime;
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
        public void RewardSettlementMapper_MapsCurrentRewardsToGoldAndCrystalResource()
        {
            var result = new DungeonRunResult(
                "run-id",
                DungeonRunOutcome.Completed,
                "dungeon",
                1,
                0,
                new[]
                {
                    new RewardGrant("reward.silver", 4),
                    new RewardGrant("reward.gold", 6),
                    new RewardGrant("reward.crystal", 3)
                });
            var rewards = new RewardCatalog(new[]
            {
                new RewardDefinition("reward.gold", "Gold"),
                new RewardDefinition("reward.silver", "Silver"),
                new RewardDefinition("reward.crystal", "Crystal")
            });

            var request = new RewardSettlementMapper().Map(result, rewards);

            Assert.That(request.RunId, Is.EqualTo("run-id"));
            Assert.That(request.GoldAmount, Is.EqualTo(10));
            Assert.That(request.ResourceGrants, Has.Count.EqualTo(1));
            Assert.That(request.ResourceGrants[0].DefinitionId, Is.EqualTo("resource.monster-crystal"));
            Assert.That(request.ResourceGrants[0].Amount, Is.EqualTo(3));
        }

        [Test]
        public void RewardSettlementMapper_UnknownRewardRejectsBeforeProfileSave()
        {
            var result = new DungeonRunResult(
                "run-id",
                DungeonRunOutcome.Completed,
                "dungeon",
                1,
                0,
                new[] { new RewardGrant("reward.unknown", 2) });
            var rewards = new RewardCatalog(new[]
            {
                new RewardDefinition("reward.gold", "Gold"),
                new RewardDefinition("reward.unknown", "Unknown")
            });
            var repository = new RecordingProfileRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            var saveCount = repository.SaveCount;

            Assert.Throws<InvalidOperationException>(() => new RewardSettlementMapper().Map(result, rewards));
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
            Assert.That(session.State.Gold, Is.EqualTo(0));
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
            Assert.That(destination.Request.ContractId, Is.EqualTo("contract.one"));
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

        [Test]
        public void ProfileTeamMapper_PreservesLatestLeaderAndCompanionOrderForDifferentCompositions()
        {
            var first = new PlayerProfileState(
                0, null,
                new[]
                {
                    new HeroProfileState("a", 1, "a.loadout"),
                    new HeroProfileState("b", 2, "b.loadout")
                },
                "b",
                new[] { "a" });
            var second = new PlayerProfileState(
                0, null,
                new[]
                {
                    new HeroProfileState("a", 1, "a.loadout"),
                    new HeroProfileState("b", 2, "b.loadout"),
                    new HeroProfileState("c", 3, "c.loadout")
                },
                "c",
                new[] { "b", "a" });

            var firstSelection = PlayerProfileComposition.MapToTeamSelection(first);
            var secondSelection = PlayerProfileComposition.MapToTeamSelection(second);

            Assert.That(firstSelection.LeaderActorId, Is.EqualTo("b"));
            Assert.That(firstSelection.CompanionActorIds, Is.EqualTo(new[] { "a" }));
            Assert.That(secondSelection.LeaderActorId, Is.EqualTo("c"));
            Assert.That(secondSelection.CompanionActorIds, Is.EqualTo(new[] { "b", "a" }));
        }

        [Test]
        public void ProfileEditHandler_TeamSizeRejection_DoesNotSaveOrReplaceState()
        {
            var repository = new RecordingProfileRepository();
            var session = CreateProfileSession(repository);
            var initialSaveCount = repository.SaveCount;
            var text = CreateProfileText();
            var handler = CreateProfileEditHandler(session, text, _ => { });

            var result = handler.Handle(new GuildProfileEditRequest(
                GuildProfileEditKind.RemoveCompanion,
                "b"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejection, Is.SameAs(text.RejectedTeamSize));
            Assert.That(repository.SaveCount, Is.EqualTo(initialSaveCount));
            Assert.That(session.State.CompanionActorIds, Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void ProfileEditHandler_AcceptedChange_SavesOnceAndReturnsPreparedSnapshot()
        {
            var repository = new RecordingProfileRepository();
            var session = CreateProfileSession(repository);
            var initialSaveCount = repository.SaveCount;
            var text = CreateProfileText();
            var expectedSnapshot = CreateGuildProfileSnapshot(text);
            var handler = CreateProfileEditHandler(session, text, _ => { }, expectedSnapshot);

            var result = handler.Handle(new GuildProfileEditRequest(
                GuildProfileEditKind.AddCompanion,
                "c"));

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Profile, Is.SameAs(expectedSnapshot));
            Assert.That(repository.SaveCount, Is.EqualTo(initialSaveCount + 1));
            Assert.That(session.State.CompanionActorIds, Is.EqualTo(new[] { "b", "c" }));
        }

        [Test]
        public void ProfileEditHandler_SaveThrows_ReportsPersistenceAndKeepsPreviousState()
        {
            var repository = new RecordingProfileRepository();
            var session = CreateProfileSession(repository);
            repository.ThrowOnSave = true;
            var reportedFailures = 0;
            var text = CreateProfileText();
            var handler = CreateProfileEditHandler(
                session,
                text,
                _ => reportedFailures++);

            var result = handler.Handle(new GuildProfileEditRequest(
                GuildProfileEditKind.AddCompanion,
                "c"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejection, Is.SameAs(text.RejectedPersistence));
            Assert.That(reportedFailures, Is.EqualTo(1));
            Assert.That(session.State.CompanionActorIds, Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void ProfileEditHandler_UnsupportedLoadout_RejectsWithoutSaving()
        {
            var repository = new RecordingProfileRepository();
            var session = CreateProfileSession(repository);
            var initialSaveCount = repository.SaveCount;
            var text = CreateProfileText();
            var handler = CreateProfileEditHandler(session, text, _ => { });

            var result = handler.Handle(new GuildProfileEditRequest(
                GuildProfileEditKind.SetLoadout,
                "a",
                "unsupported.loadout"));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Rejection, Is.SameAs(text.RejectedInvalidLoadout));
            Assert.That(repository.SaveCount, Is.EqualTo(initialSaveCount));
            Assert.That(session.State.Heroes[0].LoadoutId, Is.EqualTo("a.loadout"));
        }

        private static WorldMapDestinationResolver CreateResolver(
            WorldLocationSnapshot location,
            ContractDefinition offer,
            GuildSessionState session)
        {
            var locations = new WorldMapCatalog(
                new[] { location },
                new WorldMapUiTextSnapshot(MapText("map.title"), MapText("map.back"), MapText("map.empty")));
            var contracts = new ContractCatalog(new[] { offer });
            var contractState = session.SelectedContractId == null
                ? new ContractState()
                : new ContractState(session.SelectedContractId, Array.Empty<string>());
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
            return new WorldMapDestinationResolver(locations, contracts, contractState, presets, team);
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

        private static ContractDefinition Offer(string id, string locationId, bool isAvailable)
        {
            return new ContractDefinition(
                id,
                new ContractTextSnapshot($"{id}.title", $"{id}.title"),
                new ContractTextSnapshot($"{id}.summary", $"{id}.summary"),
                locationId,
                isAvailable,
                isAvailable ? null : new ContractTextSnapshot($"{id}.disabled", $"{id}.disabled"));
        }

        private static WorldMapTextSnapshot MapText(string id) => new(id, id);
        private static GuildTextSnapshot GuildText(string id) => new(id, id);

        private static PlayerProfileSession CreateProfileSession(
            RecordingProfileRepository repository) => new(
            repository,
            new PlayerProfileSeed(
                new[]
                {
                    new HeroProfileState("a", 1, "a.loadout"),
                    new HeroProfileState("b", 1, "b.loadout"),
                    new HeroProfileState("c", 1, "c.loadout")
                },
                "a",
                new[] { "b" }));

        private static GuildProfileEditHandler CreateProfileEditHandler(
            PlayerProfileSession session,
            GuildProfileTextSnapshot text,
            Action<Exception> reportFailure,
            GuildProfileSnapshot snapshot = null)
        {
            var setup = new DungeonRunTeamSetup(
                new[]
                {
                    Member("a", "a.loadout"),
                    Member("b", "b.loadout"),
                    Member("c", "c.loadout")
                },
                2,
                3,
                new DungeonRunTeamSelection(
                    new DungeonRunActorSelection("a", 1, "a.loadout"),
                    new[] { new DungeonRunActorSelection("b", 1, "b.loadout") }));
            snapshot ??= CreateGuildProfileSnapshot(text);
            return new GuildProfileEditHandler(
                session,
                setup,
                text,
                _ => snapshot,
                reportFailure);
        }

        private static DungeonRunTeamMemberOption Member(string actorId, string loadoutId) =>
            new(actorId, actorId, new[] { 1 }, new[] { loadoutId });

        private static GuildProfileSnapshot CreateGuildProfileSnapshot(
            GuildProfileTextSnapshot text)
        {
            var leader = new GuildHeroSnapshot(
                "a",
                "A",
                GuildHeroRole.Leader,
                1,
                10,
                2f,
                Array.Empty<GuildHeroSkillSnapshot>(),
                "a.loadout",
                new[] { new GuildHeroLoadoutSnapshot("a.loadout", "A") });
            return new GuildProfileSnapshot(
                0,
                "-",
                leader,
                Array.Empty<GuildHeroSnapshot>(),
                new[] { leader },
                text);
        }

        private static GuildProfileTextSnapshot CreateProfileText() => new(
            ProfileText("header"), ProfileText("gold"), ProfileText("rank"),
            ProfileText("unassigned"), ProfileText("leader"), ProfileText("leader-explanation"),
            ProfileText("team"), ProfileText("roster"), ProfileText("available"),
            ProfileText("level"), ProfileText("health"), ProfileText("speed"),
            ProfileText("primary"), ProfileText("active"), ProfileText("close"),
            ProfileText("make-leader"), ProfileText("add"), ProfileText("remove"),
            ProfileText("loadout"), ProfileText("team-size"), ProfileText("invalid-actor"),
            ProfileText("invalid-loadout"), ProfileText("persistence"));

        private static GuildTextSnapshot ProfileText(string id) =>
            new($"profile.{id}", id);

        private sealed class RecordingProfileRepository : IPlayerProfileRepository
        {
            public int SaveCount { get; private set; }
            public bool ThrowOnSave { get; set; }

            public bool TryLoad(out PlayerProfileState state)
            {
                state = null;
                return false;
            }

            public void Save(PlayerProfileState state)
            {
                SaveCount++;
                if (ThrowOnSave)
                {
                    throw new InvalidOperationException("Save failed.");
                }
            }
        }
    }
}
