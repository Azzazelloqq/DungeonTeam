using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.Quests.Domain
{
    public enum QuestObjectiveKind { CompleteDungeon, CollectResource, CompleteDialogue }

    public enum QuestRewardClaimPointKind { Reception, Npc }

    public sealed class QuestRewardClaimPoint
    {
        private QuestRewardClaimPoint(QuestRewardClaimPointKind kind, string npcId)
        {
            if (!Enum.IsDefined(typeof(QuestRewardClaimPointKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (kind == QuestRewardClaimPointKind.Npc && string.IsNullOrWhiteSpace(npcId))
                throw new ArgumentException("NPC reward claim point requires an NPC ID.", nameof(npcId));
            if (kind == QuestRewardClaimPointKind.Reception && npcId != null)
                throw new ArgumentException("Reception reward claim point cannot target an NPC.", nameof(npcId));
            Kind = kind;
            NpcId = npcId;
        }

        public QuestRewardClaimPointKind Kind { get; }
        public string NpcId { get; }
        public static QuestRewardClaimPoint Reception => new(QuestRewardClaimPointKind.Reception, null);
        public static QuestRewardClaimPoint Npc(string npcId) => new(QuestRewardClaimPointKind.Npc, QuestText.Require(npcId, nameof(npcId)));
        public bool Matches(QuestRewardClaimPoint other) =>
            other != null && Kind == other.Kind && string.Equals(NpcId, other.NpcId, StringComparison.Ordinal);
    }

    public sealed class QuestRewardResource
    {
        public QuestRewardResource(string definitionId, int amount)
        {
            DefinitionId = QuestText.Require(definitionId, nameof(definitionId));
            Amount = amount > 0 ? amount : throw new ArgumentOutOfRangeException(nameof(amount));
        }

        public string DefinitionId { get; }
        public int Amount { get; }
    }

    public sealed class QuestRewardDefinition
    {
        private readonly ReadOnlyCollection<QuestRewardResource> _resources;

        public QuestRewardDefinition(
            long goldAmount,
            IReadOnlyList<QuestRewardResource> resources,
            QuestRewardClaimPoint claimPoint,
            QuestText claimHint)
        {
            if (goldAmount < 0) throw new ArgumentOutOfRangeException(nameof(goldAmount));
            if (resources == null) throw new ArgumentNullException(nameof(resources));
            ClaimPoint = claimPoint ?? throw new ArgumentNullException(nameof(claimPoint));
            ClaimHint = claimHint ?? throw new ArgumentNullException(nameof(claimHint));

            var copy = new QuestRewardResource[resources.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = resources[index] ?? throw new ArgumentException("Reward resource is missing.", nameof(resources));
                if (!ids.Add(copy[index].DefinitionId))
                    throw new ArgumentException($"Reward resource '{copy[index].DefinitionId}' is duplicated.", nameof(resources));
            }
            if (goldAmount == 0 && copy.Length == 0)
                throw new ArgumentException("Reward must contain Gold or at least one resource.", nameof(resources));
            GoldAmount = goldAmount;
            _resources = Array.AsReadOnly(copy);
        }

        public long GoldAmount { get; }
        public IReadOnlyList<QuestRewardResource> Resources => _resources;
        public QuestRewardClaimPoint ClaimPoint { get; }
        public QuestText ClaimHint { get; }
    }

    public sealed class QuestText
    {
        public QuestText(string textId, string displayText)
        {
            TextId = Require(textId, nameof(textId));
            DisplayText = string.IsNullOrWhiteSpace(displayText)
                ? throw new ArgumentException("Quest text cannot be empty.", nameof(displayText))
                : displayText;
        }

        public string TextId { get; }
        public string DisplayText { get; }
        internal static string Require(string value, string parameterName) =>
            string.IsNullOrWhiteSpace(value)
                ? throw new ArgumentException("Stable ID cannot be empty.", parameterName)
                : value;
    }

    public sealed class QuestObjective
    {
        public QuestObjective(QuestObjectiveKind kind, string targetId, int requiredProgress = 1)
        {
            Kind = kind;
            TargetId = QuestText.Require(targetId, nameof(targetId));
            if (requiredProgress <= 0) throw new ArgumentOutOfRangeException(nameof(requiredProgress));
            RequiredProgress = requiredProgress;
        }

        public QuestObjectiveKind Kind { get; }
        public string TargetId { get; }
        public int RequiredProgress { get; }
    }

    public sealed class QuestDefinition
    {
        public QuestDefinition(string questId, QuestText title, QuestText summary, QuestText objectiveText, QuestObjective objective)
            : this(questId, title, summary, objectiveText, objective, null, null)
        {
        }

        public QuestDefinition(string questId, QuestText title, QuestText summary, QuestText objectiveText, QuestObjective objective, string chainId)
            : this(questId, title, summary, objectiveText, objective, chainId, null)
        {
        }

        public QuestDefinition(string questId, QuestText title, QuestText summary, QuestText objectiveText, QuestObjective objective, string chainId, QuestRewardDefinition reward)
        {
            QuestId = QuestText.Require(questId, nameof(questId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            ObjectiveText = objectiveText ?? throw new ArgumentNullException(nameof(objectiveText));
            Objective = objective ?? throw new ArgumentNullException(nameof(objective));
            ChainId = string.IsNullOrWhiteSpace(chainId) ? null : QuestText.Require(chainId, nameof(chainId));
            Reward = reward;
        }

        public string QuestId { get; }
        public QuestText Title { get; }
        public QuestText Summary { get; }
        public QuestText ObjectiveText { get; }
        public QuestObjective Objective { get; }
        public string ChainId { get; }
        public QuestRewardDefinition Reward { get; }
    }

    public sealed class QuestChainDefinition
    {
        public QuestChainDefinition(string chainId, QuestText title, IReadOnlyList<string> questIds)
        {
            ChainId = QuestText.Require(chainId, nameof(chainId));
            Title = title ?? throw new ArgumentNullException(nameof(title));
            if (questIds == null || questIds.Count == 0)
                throw new ArgumentException("Quest chain requires at least one step.", nameof(questIds));
            var copy = new string[questIds.Count];
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = QuestText.Require(questIds[index], nameof(questIds));
                if (!ids.Add(copy[index]))
                    throw new ArgumentException($"Quest chain '{ChainId}' repeats quest '{copy[index]}'.", nameof(questIds));
            }
            QuestIds = new ReadOnlyCollection<string>(copy);
        }

        public string ChainId { get; }
        public QuestText Title { get; }
        public IReadOnlyList<string> QuestIds { get; }
    }

    public sealed class QuestCatalog
    {
        private readonly IReadOnlyDictionary<string, QuestDefinition> _byId;
        private readonly IReadOnlyDictionary<string, QuestChainDefinition> _chainsById;
        public QuestCatalog(IReadOnlyList<QuestDefinition> definitions, IReadOnlyList<QuestChainDefinition> chains = null)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var copy = new QuestDefinition[definitions.Count];
            var byId = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException($"Quest at index {index} is missing.", nameof(definitions));
                if (!byId.TryAdd(definition.QuestId, definition)) throw new ArgumentException($"Quest ID '{definition.QuestId}' is duplicated.", nameof(definitions));
                copy[index] = definition;
            }
            Definitions = new ReadOnlyCollection<QuestDefinition>(copy);
            _byId = new ReadOnlyDictionary<string, QuestDefinition>(byId);
            var chainCopy = chains ?? Array.Empty<QuestChainDefinition>();
            var byChainId = new Dictionary<string, QuestChainDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < chainCopy.Count; index++)
            {
                var chain = chainCopy[index] ?? throw new ArgumentException(
                    $"Quest chain at index {index} is missing.", nameof(chains));
                if (!byChainId.TryAdd(chain.ChainId, chain))
                    throw new ArgumentException($"Quest chain ID '{chain.ChainId}' is duplicated.", nameof(chains));
                for (var step = 0; step < chain.QuestIds.Count; step++)
                {
                    var quest = Require(chain.QuestIds[step]);
                    if (!string.Equals(quest.ChainId, chain.ChainId, StringComparison.Ordinal))
                        throw new ArgumentException(
                            $"Quest '{quest.QuestId}' must reference chain '{chain.ChainId}'.", nameof(chains));
                }
            }
            for (var index = 0; index < Definitions.Count; index++)
            {
                var quest = Definitions[index];
                if (quest.ChainId != null && !byChainId.ContainsKey(quest.ChainId))
                    throw new ArgumentException(
                        $"Quest '{quest.QuestId}' references unknown chain '{quest.ChainId}'.", nameof(definitions));
            }
            Chains = new ReadOnlyCollection<QuestChainDefinition>(
                chainCopy is QuestChainDefinition[] array ? array : new List<QuestChainDefinition>(chainCopy).ToArray());
            _chainsById = new ReadOnlyDictionary<string, QuestChainDefinition>(byChainId);
        }

        public IReadOnlyList<QuestDefinition> Definitions { get; }
        public IReadOnlyList<QuestChainDefinition> Chains { get; }
        public QuestDefinition Require(string questId)
        {
            var id = QuestText.Require(questId, nameof(questId));
            return _byId.TryGetValue(id, out var definition)
                ? definition
                : throw new KeyNotFoundException($"Unknown quest ID '{id}'.");
        }

        public bool Contains(string questId) =>
            !string.IsNullOrWhiteSpace(questId) && _byId.ContainsKey(questId);

        public QuestChainDefinition RequireChain(string chainId)
        {
            var id = QuestText.Require(chainId, nameof(chainId));
            return _chainsById.TryGetValue(id, out var chain)
                ? chain
                : throw new KeyNotFoundException($"Unknown quest chain ID '{id}'.");
        }

        public bool IsQuestUnlocked(string questId, QuestState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var definition = Require(questId);
            if (definition.ChainId == null) return true;
            var chain = RequireChain(definition.ChainId);
            for (var index = 0; index < chain.QuestIds.Count; index++)
            {
                if (chain.QuestIds[index] == definition.QuestId) return true;
                if (!state.IsCompleted(chain.QuestIds[index])) return false;
            }
            return false;
        }
    }
}
