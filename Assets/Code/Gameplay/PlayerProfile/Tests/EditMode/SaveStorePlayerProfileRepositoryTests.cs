using System;
using System.IO;
using NUnit.Framework;
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
        SaveStore CreateStore() => new(new SaveStoreOptions(_path) { UseTaggedFormat = true, UseAtomicWrite = true });
    }
}
