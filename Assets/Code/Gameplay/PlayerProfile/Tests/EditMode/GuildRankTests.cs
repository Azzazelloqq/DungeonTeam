using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.PlayerProfile.Application;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Infrastructure;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.PlayerProfile.Tests.EditMode
{
    public sealed class GuildRankTests
    {
        [Test]
        public void Catalog_UsesAuthoredOrderForImmediateNextAndComparison()
        {
            var catalog = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.custom", "Custom", 7),
                new GuildRankDefinition("rank.sss", "SSS", 11)
            });

            Assert.That(catalog.TryGetNext("rank.f", out var next), Is.True);
            Assert.That(next.RankId, Is.EqualTo("rank.custom"));
            Assert.That(catalog.Compare("rank.custom", "rank.sss"), Is.LessThan(0));
            Assert.That(catalog.TryGetNext("rank.sss", out _), Is.False);
        }

        [Test]
        public void Catalog_RejectsMissingBaseRankAndNonZeroBaseCost()
        {
            Assert.Throws<ArgumentException>(() => new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.custom", "Custom", 0)
            }));

            Assert.Throws<ArgumentException>(() => new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 1)
            }));
        }

        [Test]
        public void Session_PromotesOnlyImmediateNextRankAndDebitsConfiguredGold()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            var catalog = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.e", "E", 10),
                new GuildRankDefinition("rank.d", "D", 25)
            });
            session.Commit(session.State.WithGold(10));
            var saveCount = repository.SaveCount;

            var result = session.PromoteRank(catalog);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.NextRankId, Is.EqualTo("rank.e"));
            Assert.That(session.State.RankId, Is.EqualTo("rank.e"));
            Assert.That(session.State.Gold, Is.Zero);
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount + 1));
        }

        [Test]
        public void Session_InsufficientGoldAndTerminalRank_DoNotSave()
        {
            var repository = new RecordingRepository();
            var session = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            var catalog = new GuildRankCatalog(new[]
            {
                new GuildRankDefinition("rank.f", "F", 0),
                new GuildRankDefinition("rank.e", "E", 10)
            });
            var saveCount = repository.SaveCount;

            var insufficient = session.PromoteRank(catalog);

            Assert.That(insufficient.Rejection, Is.EqualTo(RankPromotionRejection.InsufficientGold));
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));

            repository.LoadedState = session.State.WithRank("rank.e");
            var terminalSession = new PlayerProfileSession(
                repository,
                new PlayerProfileSeed(
                    new[] { new HeroProfileState("leader", 1, "loadout") },
                    "leader",
                    Array.Empty<string>()));
            saveCount = repository.SaveCount;

            var terminal = terminalSession.PromoteRank(catalog);

            Assert.That(terminal.Rejection, Is.EqualTo(RankPromotionRejection.AlreadyTerminal));
            Assert.That(repository.SaveCount, Is.EqualTo(saveCount));
        }

        [Test]
        public void V3ToV4Migration_AssignsBaseRankWithoutChangingExistingFields()
        {
            var dto = new PlayerProfileSaveV1
            {
                Gold = 17,
                Heroes = new[] { new PlayerProfileHeroSaveV1 { ActorId = "leader", Level = 2, LoadoutId = "loadout" } },
                LeaderActorId = "leader",
                CompanionActorIds = Array.Empty<string>(),
                UniqueItems = Array.Empty<PlayerProfileItemInstanceSaveV2>(),
                Resources = Array.Empty<PlayerProfileResourceStackSaveV2>(),
                EquipmentByHero = new[] { new PlayerProfileHeroEquipmentSaveV2 { ActorId = "leader" } },
                PendingTerminalResult = new PlayerProfilePendingTerminalResultSaveV3
                {
                    RunId = "run",
                    GoldAmount = 2,
                    ResourceGrants = Array.Empty<PlayerProfileTerminalResourceGrantSaveV3>()
                },
                LastAppliedRunId = "previous-run"
            };

            new PlayerProfileV3ToV4Migrator().Migrate(dto);

            Assert.That(dto.RankId, Is.EqualTo(GuildRankCatalog.BaseRankId));
            Assert.That(dto.Gold, Is.EqualTo(17));
            Assert.That(dto.Heroes[0].ActorId, Is.EqualTo("leader"));
            Assert.That(dto.PendingTerminalResult.RunId, Is.EqualTo("run"));
            Assert.That(dto.LastAppliedRunId, Is.EqualTo("previous-run"));
        }

        private sealed class RecordingRepository : IPlayerProfileRepository
        {
            public PlayerProfileState LoadedState { get; set; }
            public int SaveCount { get; private set; }

            public bool TryLoad(out PlayerProfileState state)
            {
                state = LoadedState;
                return state != null;
            }

            public void Save(PlayerProfileState state)
            {
                SaveCount++;
                LoadedState = state;
            }
        }
    }
}
