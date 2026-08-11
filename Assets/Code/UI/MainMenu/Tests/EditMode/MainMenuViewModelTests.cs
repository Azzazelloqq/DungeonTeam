using DungeonTeam.Gameplay.DungeonRun.Application;
using NUnit.Framework;

namespace Code.UI.MainMenu.Tests
{
    public sealed class MainMenuViewModelTests
    {
        [Test]
        public void RequestQuitThenCancel_HidesConfirmationWithoutQuitting()
        {
            var wasQuit = false;
            using var viewModel = CreateViewModel(() => wasQuit = true);

            viewModel.RequestQuitCommand.Execute();
            viewModel.CancelQuitCommand.Execute();

            Assert.That(viewModel.IsQuitConfirmationVisible.Value, Is.False);
            Assert.That(wasQuit, Is.False);
        }

        [Test]
        public void ConfirmQuit_AfterRequest_InvokesQuitAction()
        {
            var quitCount = 0;
            using var viewModel = CreateViewModel(() => quitCount++);

            viewModel.RequestQuitCommand.Execute();
            viewModel.ConfirmQuitCommand.Execute();

            Assert.That(quitCount, Is.EqualTo(1));
        }

        [Test]
        public void ConfirmQuit_WithoutRequest_DoesNotInvokeQuitAction()
        {
            var quitCount = 0;
            using var viewModel = CreateViewModel(() => quitCount++);

            viewModel.ConfirmQuitCommand.Execute();

            Assert.That(quitCount, Is.EqualTo(0));
        }

        [Test]
        public void Play_InvokesPlayAction()
        {
            MainMenuPlayRequest request = default;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                value => request = value,
                () => { },
                () => { });

            viewModel.Initialize();
            viewModel.SelectNextDungeonCommand.Execute();
            viewModel.IncreaseSeedCommand.Execute();

            viewModel.PlayCommand.Execute();

            Assert.That(request.DungeonId, Is.EqualTo("dungeon.chunked"));
            Assert.That(request.Seed, Is.EqualTo(43));
            Assert.That(request.Team.LeaderActorId, Is.EqualTo("actor.king"));
            Assert.That(request.Team.CompanionActorIds, Is.EqualTo(new[] { "actor.druid" }));
        }

        [Test]
        public void Back_FromPreview_ReturnsToSelectionAndInvokesBackAction()
        {
            var backCount = 0;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                _ => { },
                () => backCount++,
                () => { });
            viewModel.Initialize();

            viewModel.ShowPreview("Preview");
            viewModel.BackCommand.Execute();
            viewModel.ShowSelection();

            Assert.That(backCount, Is.EqualTo(1));
            Assert.That(viewModel.IsPreviewVisible.Value, Is.False);
        }

        private static MainMenuViewModel CreateViewModel(System.Action onQuit)
        {
            var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                _ => { },
                () => { },
                onQuit);
            viewModel.Initialize();
            return viewModel;
        }

        private static MainMenuDungeonOption[] CreateDungeonOptions()
        {
            return new[]
            {
                new MainMenuDungeonOption("AUTHORED", "dungeon.authored"),
                new MainMenuDungeonOption("CHUNKED", "dungeon.chunked")
            };
        }

        [Test]
        public void TeamSelection_ChangeLeaderAndAddCompanion_ProducesSelectedTeam()
        {
            MainMenuPlayRequest request = default;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                value => request = value,
                () => { },
                () => { });
            viewModel.Initialize();

            viewModel.TeamMembers[3].SelectLeaderCommand.Execute();
            viewModel.TeamMembers[2].ToggleCompanionCommand.Execute();
            viewModel.PlayCommand.Execute();

            Assert.That(request.Team.LeaderActorId, Is.EqualTo("actor.wizard"));
            Assert.That(
                request.Team.CompanionActorIds,
                Is.EqualTo(new[] { "actor.king", "actor.druid", "actor.rogue" }));
        }

        [Test]
        public void TeamSelection_BelowMinimum_DisablesPlay()
        {
            var playCount = 0;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                _ => playCount++,
                () => { },
                () => { });
            viewModel.Initialize();

            viewModel.TeamMembers[1].ToggleCompanionCommand.Execute();
            viewModel.PlayCommand.Execute();

            Assert.That(viewModel.CanPlay.Value, Is.False);
            Assert.That(playCount, Is.Zero);
        }

        [Test]
        public void TeamSelection_IncreaseLevel_UsesConfiguredLevelInRequest()
        {
            MainMenuPlayRequest request = default;
            using var viewModel = new MainMenuViewModel(
                new MainMenuModel(),
                CreateDungeonOptions(),
                CreateTeamSetup(),
                value => request = value,
                () => { },
                () => { });
            viewModel.Initialize();

            viewModel.TeamMembers[0].IncreaseLevelCommand.Execute();
            viewModel.PlayCommand.Execute();

            Assert.That(request.Team.Leader.Level, Is.EqualTo(2));
            Assert.That(viewModel.TeamMembers[0].LevelLabel.Value, Is.EqualTo("LVL 2"));
        }

        private static DungeonRunTeamSetup CreateTeamSetup()
        {
            return new DungeonRunTeamSetup(
                new[]
                {
                    new DungeonRunTeamMemberOption("actor.king", "KING", new[] { 1, 2 }),
                    new DungeonRunTeamMemberOption("actor.druid", "DRUID", new[] { 1, 2 }),
                    new DungeonRunTeamMemberOption("actor.rogue", "ROGUE", new[] { 1, 2 }),
                    new DungeonRunTeamMemberOption("actor.wizard", "WIZARD", new[] { 1, 2 })
                },
                2,
                4,
                new DungeonRunTeamSelection(
                    "actor.king",
                    new[] { "actor.druid" }));
        }
    }
}
