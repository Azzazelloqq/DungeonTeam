using System;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class GuildProfileViewModelTests
    {
        [Test]
        public void SelectHero_ByStableId_PublishesRequestedHero()
        {
            var viewModel = Create(out _);
            viewModel.Initialize();

            viewModel.SelectHeroCommand.Execute("companion");

            Assert.That(viewModel.SelectedHero.Value.ActorId, Is.EqualTo("companion"));
        }

        [Test]
        public void SelectHero_UnknownId_ThrowsWithoutChangingSelection()
        {
            var viewModel = Create(out _);
            viewModel.Initialize();

            Assert.Throws<ArgumentException>(() =>
                viewModel.SelectHeroCommand.Execute("missing"));
            Assert.That(viewModel.SelectedHero.Value.ActorId, Is.EqualTo("leader"));
        }

        [Test]
        public void Close_WhenVisible_InvokesOwner()
        {
            var viewModel = Create(out var closeCount);
            viewModel.Initialize();
            viewModel.Open();

            viewModel.CloseCommand.Execute(null);

            Assert.That(closeCount(), Is.EqualTo(1));
        }

        [Test]
        public void Edit_AcceptedResult_ReplacesSnapshotAndKeepsSelectedActorId()
        {
            var initial = CreateSnapshot();
            var updated = CreateSnapshot(leaderId: "companion");
            var viewModel = new GuildProfileViewModel(
                new GuildProfileModel(initial),
                () => { },
                _ => GuildProfileEditResult.Accept(updated));
            viewModel.Initialize();
            viewModel.SelectHeroCommand.Execute("companion");

            viewModel.SetLeaderCommand.Execute(null);

            Assert.That(viewModel.Profile.Leader.ActorId, Is.EqualTo("companion"));
            Assert.That(viewModel.SelectedHero.Value.ActorId, Is.EqualTo("companion"));
            Assert.That(viewModel.Rejection.Value, Is.Null);
        }

        [Test]
        public void Edit_RejectedResult_PreservesSnapshotAndPublishesReason()
        {
            var initial = CreateSnapshot();
            var reason = Text("profile.rejection");
            var viewModel = new GuildProfileViewModel(
                new GuildProfileModel(initial),
                () => { },
                _ => GuildProfileEditResult.Reject(reason));
            viewModel.Initialize();

            viewModel.AddCompanionCommand.Execute(null);

            Assert.That(viewModel.Profile, Is.SameAs(initial));
            Assert.That(viewModel.Rejection.Value, Is.SameAs(reason));
        }

        private static GuildProfileViewModel Create(out Func<int> closeCount)
        {
            var closed = 0;
            closeCount = () => closed;
            var snapshot = CreateSnapshot();
            var leader = snapshot.Leader;
            var companion = snapshot.Companions[0];
            return new GuildProfileViewModel(
                new GuildProfileModel(snapshot),
                () => closed++,
                _ => GuildProfileEditResult.Accept(snapshot));
        }

        private static GuildProfileSnapshot CreateSnapshot(string leaderId = "leader")
        {
            var leader = new GuildHeroSnapshot(
                "leader",
                "Leader",
                GuildHeroRole.Leader,
                1,
                10,
                2f,
                Array.Empty<GuildHeroSkillSnapshot>(),
                "leader.loadout",
                Loadouts("leader.loadout"));
            var companion = new GuildHeroSnapshot(
                "companion",
                "Companion",
                GuildHeroRole.Companion,
                2,
                11,
                3f,
                Array.Empty<GuildHeroSkillSnapshot>(),
                "companion.loadout",
                Loadouts("companion.loadout"));
            var snapshot = new GuildProfileSnapshot(
                0,
                "-",
                leader,
                new[] { companion },
                new[] { leader, companion },
                CreateProfileText());
            if (leaderId == "leader")
            {
                return snapshot;
            }

            var updatedLeader = new GuildHeroSnapshot(
                companion.ActorId, companion.DisplayName, GuildHeroRole.Leader,
                companion.Level, companion.MaximumHealth, companion.MovementSpeed,
                companion.Skills, companion.LoadoutId, companion.AllowedLoadouts);
            var updatedCompanion = new GuildHeroSnapshot(
                leader.ActorId, leader.DisplayName, GuildHeroRole.Companion,
                leader.Level, leader.MaximumHealth, leader.MovementSpeed, leader.Skills,
                leader.LoadoutId, leader.AllowedLoadouts);
            return new GuildProfileSnapshot(
                0, "-", updatedLeader, new[] { updatedCompanion },
                new[] { updatedCompanion, updatedLeader }, CreateProfileText());
        }

        private static GuildProfileTextSnapshot CreateProfileText() => new(
            Text("profile.header"),
            Text("profile.gold"),
            Text("profile.rank"),
            Text("profile.rank.unassigned"),
            Text("profile.leader"),
            Text("profile.leader.explanation"),
            Text("profile.team"),
            Text("profile.roster"),
            Text("profile.available"),
            Text("profile.level"),
            Text("profile.health"),
            Text("profile.speed"),
            Text("profile.skill.primary"),
            Text("profile.skill.active"),
            Text("profile.close"),
            Text("profile.make-leader"),
            Text("profile.add-companion"),
            Text("profile.remove-companion"),
            Text("profile.loadout"),
            Text("profile.rejection.team-size"),
            Text("profile.rejection.invalid-actor"),
            Text("profile.rejection.invalid-loadout"),
            Text("profile.rejection.persistence"));

        private static GuildHeroLoadoutSnapshot[] Loadouts(string loadoutId) =>
            new[] { new GuildHeroLoadoutSnapshot(loadoutId, loadoutId) };

        private static GuildTextSnapshot Text(string id) => new(id, id);
    }
}
