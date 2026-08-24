using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.GuildHall.Tests.EditMode
{
    public sealed class QuestRewardCollectionViewModelTests
    {
        [Test]
        public void Receive_NotInvokedUntilExplicitCommand()
        {
            var requests = new List<QuestRewardClaimRequest>();
            var viewModel = Create(request => { requests.Add(request); return true; });
            viewModel.Initialize();

            Assert.That(requests, Is.Empty);
            Assert.That(viewModel.Entries, Has.Count.EqualTo(1));

            viewModel.ReceiveCommand.Execute("quest.rewarded");

            Assert.That(requests, Has.Count.EqualTo(1));
            Assert.That(viewModel.Entries, Is.Empty);
        }

        [Test]
        public void Receive_FailedCallback_KeepsEntryAvailable()
        {
            var viewModel = Create(_ => false);
            viewModel.Initialize();

            viewModel.ReceiveCommand.Execute("quest.rewarded");

            Assert.That(viewModel.Entries, Has.Count.EqualTo(1));
        }

        private static QuestRewardCollectionViewModel Create(Func<QuestRewardClaimRequest, bool> claim)
        {
            var point = new QuestRewardClaimPointSnapshot(QuestRewardClaimPointKind.Reception);
            var entry = new QuestRewardCollectionEntrySnapshot(
                "quest.rewarded",
                Text("quest.title"),
                new[] { Text("Gold: 4") },
                Text("quest.reception"),
                Text("Receive"),
                point);
            var snapshot = new QuestRewardCollectionSnapshot(
                point,
                new[] { entry },
                Text("Rewards"),
                Text("Close"));
            return new QuestRewardCollectionViewModel(
                new QuestRewardCollectionModel(snapshot),
                claim,
                () => { });
        }

        private static GuildTextSnapshot Text(string value) => new(value, value);
    }
}
