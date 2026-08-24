using System;
using DungeonTeam.Gameplay.Quests.Application;
using DungeonTeam.Gameplay.Quests.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Quests.Tests.EditMode
{
    public sealed class QuestSessionTests
    {
        [Test]
        public void Chain_OnlyFirstStepCanBeAccepted_CompletionUnlocksNext()
        {
            var session = new QuestSession(new Repository());
            var catalog = Catalog();
            Assert.That(session.Accept("quest.crystals", catalog), Is.False);
            Assert.That(session.Accept("quest.clear", catalog), Is.True);
            Assert.That(session.RecordDungeonCompleted("dungeon.crypt", catalog), Is.True);
            Assert.That(session.Accept("quest.crystals", catalog), Is.True);
        }

        [Test]
        public void AcceptedObjectives_OnlyMatchingSourcesAdvanceAndComplete()
        {
            var session = new QuestSession(new Repository());
            var catalog = Catalog();
            Assert.That(session.Accept("quest.talk", catalog), Is.True);
            Assert.That(session.RecordDialogueCompleted("npc.other", catalog), Is.False);
            Assert.That(session.RecordDialogueCompleted("npc.debater", catalog), Is.True);
            Assert.That(session.Accept("quest.clear", catalog), Is.True);
            Assert.That(session.RecordDungeonCompleted("dungeon.crypt", catalog), Is.True);
            Assert.That(session.Accept("quest.crystals", catalog), Is.True);
            Assert.That(session.RecordSettledResources(new[] { new QuestResourceGrant("resource.monster-crystal", 2) }, catalog), Is.True);
            Assert.That(session.State.GetProgress("quest.crystals"), Is.EqualTo(2));
            Assert.That(session.RecordSettledResources(new[] { new QuestResourceGrant("resource.monster-crystal", 1) }, catalog), Is.True);
            Assert.That(session.State.IsCompleted("quest.crystals"), Is.True);
        }

        [Test]
        public void SaveFailure_DoesNotPublishCandidateState()
        {
            var session = new QuestSession(new Repository { ThrowOnSave = true });
            Assert.Throws<InvalidOperationException>(() => session.Accept("quest.talk", Catalog()));
            Assert.That(session.State.IsActive("quest.talk"), Is.False);
        }

        [Test]
        public void RewardClaim_RequiresCompletionAndMatchesPoint_ThenCanBeMarkedOnce()
        {
            var point = QuestRewardClaimPoint.Reception;
            var catalog = new QuestCatalog(new[]
            {
                Definition("quest.rewarded", QuestObjectiveKind.CompleteDialogue, "npc.debater", 1,
                    reward: new QuestRewardDefinition(4,
                        new[] { new QuestRewardResource("resource.crystal", 2) },
                        point,
                        Text("quest.rewarded.hint"))),
                Definition("quest.other", QuestObjectiveKind.CompleteDialogue, "npc.other", 1)
            });
            var session = new QuestSession(new Repository());

            Assert.That(session.Accept("quest.rewarded", catalog), Is.True);
            Assert.That(session.State.GetClaimableAt(point, catalog), Is.Empty);
            Assert.That(session.RecordDialogueCompleted("npc.debater", catalog), Is.True);
            Assert.That(session.State.GetClaimableAt(QuestRewardClaimPoint.Npc("npc.debater"), catalog), Is.Empty);
            Assert.That(session.State.GetClaimableAt(point, catalog), Is.EqualTo(new[] { "quest.rewarded" }));
            Assert.That(session.MarkRewardClaimed("quest.rewarded", catalog), Is.True);
            Assert.That(session.MarkRewardClaimed("quest.rewarded", catalog), Is.False);
            Assert.That(session.State.ClaimedRewardQuestIds, Is.EqualTo(new[] { "quest.rewarded" }));
        }

        [Test]
        public void QuestV1ToV2Migration_AddsEmptyClaimsWithoutChangingProgress()
        {
            var dto = new DungeonTeam.Gameplay.Quests.Infrastructure.QuestSaveV1
            {
                Active = new[] { new DungeonTeam.Gameplay.Quests.Infrastructure.QuestProgressSaveV1 { QuestId = "quest.one", Progress = 2 } },
                Completed = new[] { "quest.done" }
            };
            var migrator = new DungeonTeam.Gameplay.Quests.Infrastructure.QuestV1ToV2Migrator();

            migrator.Migrate(dto);

            Assert.That(dto.Active[0].Progress, Is.EqualTo(2));
            Assert.That(dto.Completed, Is.EqualTo(new[] { "quest.done" }));
            Assert.That(dto.ClaimedRewardQuestIds, Is.Empty);
        }

        private static QuestCatalog Catalog()
        {
            var chainId = "chain.first";
            return new QuestCatalog(new[]
            {
                Definition("quest.clear", QuestObjectiveKind.CompleteDungeon, "dungeon.crypt", 1, chainId),
                Definition("quest.crystals", QuestObjectiveKind.CollectResource, "resource.monster-crystal", 3, chainId),
                Definition("quest.talk", QuestObjectiveKind.CompleteDialogue, "npc.debater", 1)
            }, new[] { new QuestChainDefinition(chainId, Text("chain.title"), new[] { "quest.clear", "quest.crystals" }) });
        }
        private static QuestDefinition Definition(string id, QuestObjectiveKind kind, string target, int required, string chainId = null, QuestRewardDefinition reward = null) => new(id, Text(id + ".title"), Text(id + ".summary"), Text(id + ".objective"), new QuestObjective(kind, target, required), chainId, reward);
        private static QuestText Text(string id) => new(id, id);
        private sealed class Repository : IQuestRepository
        {
            public bool ThrowOnSave { get; set; }
            public bool TryLoad(out QuestState state) { state = null; return false; }
            public void Save(QuestState state) { if (ThrowOnSave) throw new InvalidOperationException("save failed"); }
        }
    }
}
