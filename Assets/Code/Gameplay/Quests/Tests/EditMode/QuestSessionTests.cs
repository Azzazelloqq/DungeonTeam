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
        private static QuestDefinition Definition(string id, QuestObjectiveKind kind, string target, int required, string chainId = null) => new(id, Text(id + ".title"), Text(id + ".summary"), Text(id + ".objective"), new QuestObjective(kind, target, required), chainId);
        private static QuestText Text(string id) => new(id, id);
        private sealed class Repository : IQuestRepository
        {
            public bool ThrowOnSave { get; set; }
            public bool TryLoad(out QuestState state) { state = null; return false; }
            public void Save(QuestState state) { if (ThrowOnSave) throw new InvalidOperationException("save failed"); }
        }
    }
}
