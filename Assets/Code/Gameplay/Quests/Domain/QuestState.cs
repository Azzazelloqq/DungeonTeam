using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Quests.Domain
{
    public sealed class QuestProgress
    {
        public QuestProgress(string questId, int currentProgress)
        {
            QuestId = QuestText.Require(questId, nameof(questId));
            if (currentProgress < 0) throw new ArgumentOutOfRangeException(nameof(currentProgress));
            CurrentProgress = currentProgress;
        }
        public string QuestId { get; }
        public int CurrentProgress { get; }
    }

    public sealed class QuestState
    {
        private readonly Dictionary<string, int> _progressByQuestId;
        private readonly List<string> _activeOrder;
        private readonly HashSet<string> _completedIds;
        private readonly List<string> _completedOrder;
        public QuestState(IReadOnlyList<QuestProgress> active = null, IReadOnlyList<string> completed = null)
        {
            _progressByQuestId = new Dictionary<string, int>(StringComparer.Ordinal);
            _completedIds = new HashSet<string>(StringComparer.Ordinal);
            _activeOrder = new List<string>();
            _completedOrder = new List<string>();
            foreach (var progress in active ?? Array.Empty<QuestProgress>())
            {
                if (progress == null || !_progressByQuestId.TryAdd(progress.QuestId, progress.CurrentProgress))
                    throw new ArgumentException("Active quest progress must be unique and non-null.", nameof(active));
                _activeOrder.Add(progress.QuestId);
            }
            foreach (var id in completed ?? Array.Empty<string>())
            {
                var completedId = QuestText.Require(id, nameof(completed));
                if (!_completedIds.Add(completedId) || _progressByQuestId.ContainsKey(completedId))
                    throw new ArgumentException("Quest cannot be duplicate, active and completed.", nameof(completed));
                _completedOrder.Add(completedId);
            }
        }
        public IReadOnlyList<QuestProgress> Active => SnapshotActive();
        public IReadOnlyList<string> CompletedIds => new ReadOnlyCollection<string>(new List<string>(_completedOrder));
        public bool IsCompleted(string questId) => !string.IsNullOrWhiteSpace(questId) && _completedIds.Contains(questId);
        public bool IsActive(string questId) => !string.IsNullOrWhiteSpace(questId) && _progressByQuestId.ContainsKey(questId);
        public int GetProgress(string questId) => _progressByQuestId.TryGetValue(questId, out var value) ? value : 0;
        public QuestState Clone() => new(Active, CompletedIds);
        public bool TryAccept(string questId, QuestCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var definition = catalog.Require(questId);
            if (IsActive(definition.QuestId) || IsCompleted(definition.QuestId) || !catalog.IsQuestUnlocked(definition.QuestId, this)) return false;
            _progressByQuestId.Add(definition.QuestId, 0);
            _activeOrder.Add(definition.QuestId);
            return true;
        }
        public bool ApplyDungeonCompleted(string dungeonId, QuestCatalog catalog) => Apply(catalog, QuestObjectiveKind.CompleteDungeon, dungeonId, 1);
        public bool ApplyDialogueCompleted(string npcId, QuestCatalog catalog) => Apply(catalog, QuestObjectiveKind.CompleteDialogue, npcId, 1);
        public bool ApplySettledResource(string resourceId, int amount, QuestCatalog catalog) => amount > 0 && Apply(catalog, QuestObjectiveKind.CollectResource, resourceId, amount);
        private bool Apply(QuestCatalog catalog, QuestObjectiveKind kind, string targetId, int amount)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            var changed = false;
            for (var index = 0; index < catalog.Definitions.Count; index++)
            {
                var definition = catalog.Definitions[index];
                if (!_progressByQuestId.TryGetValue(definition.QuestId, out var current) || definition.Objective.Kind != kind || definition.Objective.TargetId != targetId) continue;
                var next = Math.Min(definition.Objective.RequiredProgress, current + amount);
                if (next == current) continue;
                changed = true;
                if (next == definition.Objective.RequiredProgress)
                {
                    _progressByQuestId.Remove(definition.QuestId);
                    _activeOrder.Remove(definition.QuestId);
                    if (_completedIds.Add(definition.QuestId)) _completedOrder.Add(definition.QuestId);
                }
                else _progressByQuestId[definition.QuestId] = next;
            }
            return changed;
        }

        public void ValidateAgainst(QuestCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            foreach (var progress in _progressByQuestId)
            {
                var definition = catalog.Require(progress.Key);
                if (progress.Value > definition.Objective.RequiredProgress)
                    throw new InvalidOperationException(
                        $"Quest '{progress.Key}' has progress beyond its configured requirement.");
            }

            for (var index = 0; index < _completedOrder.Count; index++) catalog.Require(_completedOrder[index]);
        }
        private IReadOnlyList<QuestProgress> SnapshotActive()
        {
            var values = new List<QuestProgress>(_progressByQuestId.Count);
            for (var index = 0; index < _activeOrder.Count; index++)
            {
                var questId = _activeOrder[index];
                values.Add(new QuestProgress(questId, _progressByQuestId[questId]));
            }
            return new ReadOnlyCollection<QuestProgress>(values);
        }
    }
}
