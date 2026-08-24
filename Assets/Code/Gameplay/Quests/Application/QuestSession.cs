using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Quests.Domain;

namespace DungeonTeam.Gameplay.Quests.Application
{
    public interface IQuestRepository { bool TryLoad(out QuestState state); void Save(QuestState state); }
    public sealed class QuestSession
    {
        private readonly IQuestRepository _repository;
        public QuestSession(IQuestRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            State = repository.TryLoad(out var state) ? state ?? throw new InvalidOperationException("Loaded quest state is missing.") : new QuestState();
        }
        public QuestState State { get; private set; }
        public bool Accept(string questId, QuestCatalog catalog) => Mutate(candidate => candidate.TryAccept(questId, catalog));
        public bool RecordDungeonCompleted(string dungeonId, QuestCatalog catalog) => Mutate(candidate => candidate.ApplyDungeonCompleted(dungeonId, catalog));
        public bool RecordDialogueCompleted(string npcId, QuestCatalog catalog) => Mutate(candidate => candidate.ApplyDialogueCompleted(npcId, catalog));
        public bool RecordSettledResources(IReadOnlyList<QuestResourceGrant> grants, QuestCatalog catalog)
        {
            if (grants == null) throw new ArgumentNullException(nameof(grants));
            return Mutate(candidate =>
            {
                var changed = false;
                for (var index = 0; index < grants.Count; index++)
                    changed |= candidate.ApplySettledResource(grants[index].ResourceId, grants[index].Amount, catalog);
                return changed;
            });
        }
        private bool Mutate(Func<QuestState, bool> mutation)
        {
            var candidate = State.Clone();
            if (!mutation(candidate)) return false;
            _repository.Save(candidate);
            State = candidate;
            return true;
        }
    }
    public readonly struct QuestResourceGrant
    {
        public QuestResourceGrant(string resourceId, int amount) { ResourceId = Require(resourceId); if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount)); Amount = amount; }
        public string ResourceId { get; }
        public int Amount { get; }
        private static string Require(string value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Resource ID cannot be empty.", nameof(value)) : value;
    }
}
