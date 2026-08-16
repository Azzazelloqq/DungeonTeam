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

        private static GuildProfileViewModel Create(out Func<int> closeCount)
        {
            var closed = 0;
            closeCount = () => closed;
            var leader = new GuildHeroSnapshot(
                "leader",
                "Leader",
                GuildHeroRole.Leader,
                1,
                10,
                2f,
                Array.Empty<GuildHeroSkillSnapshot>());
            var companion = new GuildHeroSnapshot(
                "companion",
                "Companion",
                GuildHeroRole.Companion,
                2,
                11,
                3f,
                Array.Empty<GuildHeroSkillSnapshot>());
            var snapshot = new GuildProfileSnapshot(
                0,
                "-",
                leader,
                new[] { companion },
                new[] { leader, companion },
                CreateProfileText());
            return new GuildProfileViewModel(
                new GuildProfileModel(snapshot),
                () => closed++);
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
            Text("profile.close"));

        private static GuildTextSnapshot Text(string id) => new(id, id);
    }
}
