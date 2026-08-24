using System;
using System.IO;
using NUnit.Framework;
using DungeonTeam.Gameplay.Inventory.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Domain;
using DungeonTeam.Gameplay.PlayerProfile.Infrastructure;
using LocalSaveSystem;

namespace DungeonTeam.Gameplay.PlayerProfile.Tests.EditMode
{
    public sealed class SaveStorePlayerProfileRepositoryTests
    {
        private string _path;
        [SetUp] public void SetUp() { _path = Path.Combine(Path.GetTempPath(), "DungeonTeamProfileTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(_path); }
        [TearDown] public void TearDown() { if (Directory.Exists(_path)) Directory.Delete(_path, true); }
        [Test] public void Save_ThenReopenLoad_PreservesRosterAndCompanionOrder()
        {
            var state = new PlayerProfileState(9, null, new[] { new HeroProfileState("leader", 2, "a"), new HeroProfileState("b", 3, "b"), new HeroProfileState("c", 4, "c") }, "leader", new[] { "c", "b" });
            using (var store = CreateStore()) new SaveStorePlayerProfileRepository(store).Save(state);
            using (var store = CreateStore()) { var repo = new SaveStorePlayerProfileRepository(store); Assert.That(repo.TryLoad(out var loaded), Is.True); Assert.That(loaded.Gold, Is.EqualTo(9)); Assert.That(loaded.Heroes[2].ActorId, Is.EqualTo("c")); Assert.That(loaded.CompanionActorIds, Is.EqualTo(new[] { "c", "b" })); }
        }
        [Test] public void Load_MissingRecord_ReturnsFalse() { using var store = CreateStore(); Assert.That(new SaveStorePlayerProfileRepository(store).TryLoad(out _), Is.False); }
        [Test]
        public void Save_ThenReopenLoad_PreservesInventoryV2Fields()
        {
            var inventory = new InventoryState(
                new[] { new ItemInstanceState("blade-instance", "equipment.training-blade") },
                new[] { new ResourceStackState("resource.monster-crystal", 3) },
                new[] { new HeroEquipmentState("leader", "blade-instance") });
            var state = new PlayerProfileState(
                9,
                "rank.one",
                new[] { new HeroProfileState("leader", 2, "a") },
                "leader",
                Array.Empty<string>(),
                inventory);

            using (var store = CreateStore()) new SaveStorePlayerProfileRepository(store).Save(state);
            using (var store = CreateStore())
            {
                var repo = new SaveStorePlayerProfileRepository(store);
                Assert.That(repo.TryLoad(out var loaded), Is.True);
                Assert.That(loaded.Inventory.UniqueItems[0].InstanceId, Is.EqualTo("blade-instance"));
                Assert.That(loaded.Inventory.Resources[0].Quantity, Is.EqualTo(3));
                Assert.That(loaded.Inventory.EquipmentByHero[0].WeaponInstanceId, Is.EqualTo("blade-instance"));
            }
        }

        [Test]
        public void V1ToV2Migrator_PreservesProfileAndCreatesDeterministicStarterItemsOnce()
        {
            var dto = new PlayerProfileSaveV1
            {
                Gold = 7,
                RankId = "rank.one",
                Heroes = new[] { new PlayerProfileHeroSaveV1 { ActorId = "leader", Level = 2, LoadoutId = "a" } },
                LeaderActorId = "leader",
                CompanionActorIds = Array.Empty<string>()
            };
            var migrator = new PlayerProfileV1ToV2Migrator();

            migrator.Migrate(dto);
            migrator.Migrate(dto);

            Assert.That(dto.Gold, Is.EqualTo(7));
            Assert.That(dto.RankId, Is.EqualTo("rank.one"));
            Assert.That(dto.Heroes[0].LoadoutId, Is.EqualTo("a"));
            Assert.That(dto.UniqueItems, Has.Length.EqualTo(3));
            Assert.That(dto.Resources, Is.Empty);
            Assert.That(dto.EquipmentByHero, Has.Length.EqualTo(1));
            Assert.That(dto.EquipmentByHero[0].ActorId, Is.EqualTo("leader"));
        }

        [Test]
        public void V2ToV3Migrator_LeavesNewTerminalFieldsAbsent()
        {
            var dto = new PlayerProfileSaveV1
            {
                Gold = 12,
                Heroes = new[] { new PlayerProfileHeroSaveV1 { ActorId = "leader", Level = 1, LoadoutId = "loadout" } },
                LeaderActorId = "leader",
                CompanionActorIds = Array.Empty<string>(),
                UniqueItems = Array.Empty<PlayerProfileItemInstanceSaveV2>(),
                Resources = Array.Empty<PlayerProfileResourceStackSaveV2>(),
                EquipmentByHero = new[] { new PlayerProfileHeroEquipmentSaveV2 { ActorId = "leader" } }
            };
            var migrator = new PlayerProfileV2ToV3Migrator();

            dto.PendingTerminalResult = new PlayerProfilePendingTerminalResultSaveV3
            {
                RunId = "stale",
                GoldAmount = 1,
                ResourceGrants = Array.Empty<PlayerProfileTerminalResourceGrantSaveV3>()
            };
            dto.LastAppliedRunId = "stale";
            migrator.Migrate(dto);

            Assert.That(migrator.WasApplied, Is.True);
            Assert.That(dto.PendingTerminalResult, Is.Null);
            Assert.That(dto.LastAppliedRunId, Is.Null);
            Assert.That(dto.Gold, Is.EqualTo(12));
            Assert.That(dto.Heroes[0].ActorId, Is.EqualTo("leader"));
        }

        [Test]
        public void V4ToV5Migrator_AddsEmptyAppliedClaimIds()
        {
            var dto = new PlayerProfileSaveV1
            {
                Gold = 12,
                Heroes = new[] { new PlayerProfileHeroSaveV1 { ActorId = "leader", Level = 1, LoadoutId = "loadout" } },
                LeaderActorId = "leader",
                CompanionActorIds = Array.Empty<string>(),
                UniqueItems = Array.Empty<PlayerProfileItemInstanceSaveV2>(),
                Resources = Array.Empty<PlayerProfileResourceStackSaveV2>(),
                EquipmentByHero = new[] { new PlayerProfileHeroEquipmentSaveV2 { ActorId = "leader" } }
            };

            var migrator = new PlayerProfileV4ToV5Migrator();
            migrator.Migrate(dto);

            Assert.That(migrator.WasApplied, Is.True);
            Assert.That(dto.AppliedClaimIds, Is.Empty);
            Assert.That(dto.Gold, Is.EqualTo(12));
        }
        SaveStore CreateStore() => new(new SaveStoreOptions(_path) { UseTaggedFormat = true, UseAtomicWrite = true });
    }
}
