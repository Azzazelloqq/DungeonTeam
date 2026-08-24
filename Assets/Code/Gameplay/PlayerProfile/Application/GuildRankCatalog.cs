using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DungeonTeam.Gameplay.PlayerProfile.Application
{
    public sealed class GuildRankDefinition
    {
        public GuildRankDefinition(string rankId, string displayName, long promotionCost)
        {
            RankId = Require(rankId, nameof(rankId));
            DisplayName = Require(displayName, nameof(displayName));
            PromotionCost = promotionCost >= 0
                ? promotionCost
                : throw new ArgumentOutOfRangeException(nameof(promotionCost));
        }

        public string RankId { get; }
        public string DisplayName { get; }
        public long PromotionCost { get; }

        private static string Require(string value, string parameterName) =>
            !string.IsNullOrWhiteSpace(value)
                ? value
                : throw new ArgumentException("Guild rank value cannot be empty.", parameterName);
    }

    public sealed class GuildRankCatalog
    {
        public const string BaseRankId = "rank.f";

        private readonly IReadOnlyDictionary<string, GuildRankDefinition> _definitionsById;

        public GuildRankCatalog(IReadOnlyList<GuildRankDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            var copy = new GuildRankDefinition[definitions.Count];
            var byId = new Dictionary<string, GuildRankDefinition>(StringComparer.Ordinal);
            for (var index = 0; index < copy.Length; index++)
            {
                var definition = definitions[index] ?? throw new ArgumentException(
                    $"Guild rank at index {index} is missing.",
                    nameof(definitions));
                if (!byId.TryAdd(definition.RankId, definition))
                {
                    throw new ArgumentException(
                        $"Guild rank ID '{definition.RankId}' is duplicated.",
                        nameof(definitions));
                }

                if (index == 0 && definition.PromotionCost != 0)
                {
                    throw new ArgumentException(
                        "The base guild rank must have a zero promotion cost.",
                        nameof(definitions));
                }

                copy[index] = definition;
            }

            if (!byId.ContainsKey(BaseRankId))
            {
                throw new ArgumentException(
                    $"Guild rank catalog must contain '{BaseRankId}'.",
                    nameof(definitions));
            }

            if (copy.Length == 0 || !string.Equals(copy[0].RankId, BaseRankId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Guild rank catalog must start with '{BaseRankId}'.",
                    nameof(definitions));
            }

            Definitions = Array.AsReadOnly(copy);
            _definitionsById = new ReadOnlyDictionary<string, GuildRankDefinition>(byId);
        }

        public IReadOnlyList<GuildRankDefinition> Definitions { get; }

        public GuildRankDefinition Require(string rankId)
        {
            if (string.IsNullOrWhiteSpace(rankId) || !_definitionsById.TryGetValue(rankId, out var definition))
            {
                throw new KeyNotFoundException($"Unknown guild rank ID '{rankId}'.");
            }

            return definition;
        }

        public bool Contains(string rankId) =>
            !string.IsNullOrWhiteSpace(rankId) && _definitionsById.ContainsKey(rankId);

        public bool TryGetNext(string rankId, out GuildRankDefinition definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(rankId) || !_definitionsById.ContainsKey(rankId))
            {
                return false;
            }

            for (var index = 0; index < Definitions.Count - 1; index++)
            {
                if (string.Equals(Definitions[index].RankId, rankId, StringComparison.Ordinal))
                {
                    definition = Definitions[index + 1];
                    return true;
                }
            }

            return false;
        }

        public int Compare(string leftRankId, string rightRankId)
        {
            var leftIndex = IndexOf(leftRankId);
            var rightIndex = IndexOf(rightRankId);
            return leftIndex.CompareTo(rightIndex);
        }

        private int IndexOf(string rankId)
        {
            if (string.IsNullOrWhiteSpace(rankId))
            {
                throw new KeyNotFoundException($"Unknown guild rank ID '{rankId}'.");
            }

            for (var index = 0; index < Definitions.Count; index++)
            {
                if (string.Equals(Definitions[index].RankId, rankId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            throw new KeyNotFoundException($"Unknown guild rank ID '{rankId}'.");
        }
    }

    public enum RankPromotionRejection
    {
        AlreadyTerminal = 0,
        InsufficientGold = 1,
        InvalidCurrentRank = 2
    }

    public sealed class RankPromotionResult
    {
        private RankPromotionResult(
            bool accepted,
            string nextRankId,
            RankPromotionRejection? rejection)
        {
            Accepted = accepted;
            NextRankId = nextRankId;
            Rejection = rejection;
        }

        public bool Accepted { get; }
        public string NextRankId { get; }
        public RankPromotionRejection? Rejection { get; }

        public static RankPromotionResult Accept(string nextRankId) =>
            new(true, !string.IsNullOrWhiteSpace(nextRankId)
                ? nextRankId
                : throw new ArgumentException("Next rank ID cannot be empty.", nameof(nextRankId)), null);

        public static RankPromotionResult Reject(RankPromotionRejection rejection) =>
            new(false, null, rejection);
    }
}
