using System;
using Code.Configuration;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.GuildHall.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Config
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Guild Hall Config",
        fileName = "GuildHallConfig")]
    public sealed class GuildHallConfigPage : ConfigPage
    {
        [SerializeField]
        private GuildNpcDefinitionConfig[] _npcs = Array.Empty<GuildNpcDefinitionConfig>();

        [SerializeField]
        private GuildHallMovementSettingsConfig _movement = new();

        [SerializeField]
        private GuildInteractionLabelsConfig _interactionLabels = new();

        [SerializeField]
        private NoticeBoardTextConfig _noticeBoardText = new();

        [SerializeField]
        private RunSummaryTextConfig _runSummaryText = new();

        [SerializeField]
        private GuildProfileTextConfig _profileText = new();

        public GuildHallCatalog CreateCatalog()
        {
            if (_npcs == null)
            {
                throw new InvalidOperationException("Guild Hall NPC definitions cannot be null.");
            }

            var npcs = new AmbientNpcSnapshot[_npcs.Length];
            for (var index = 0; index < _npcs.Length; index++)
            {
                var definition = _npcs[index] ?? throw new InvalidOperationException(
                    $"Guild Hall NPC definition at index {index} is missing.");
                npcs[index] = definition.ToSnapshot(index);
            }

            return new GuildHallCatalog(
                npcs,
                (_movement ?? throw new InvalidOperationException(
                    "Guild Hall movement settings are missing.")).ToSnapshot(),
                (_interactionLabels ?? throw new InvalidOperationException(
                    "Guild Hall interaction labels are missing.")).ToSnapshot(),
                (_noticeBoardText ?? throw new InvalidOperationException(
                    "Guild Hall Notice Board text is missing.")).ToSnapshot(),
                (_runSummaryText ?? throw new InvalidOperationException(
                    "Guild Hall Run Summary text is missing.")).ToSnapshot(),
                (_profileText ?? throw new InvalidOperationException(
                    "Guild Hall Profile text is missing.")).ToSnapshot());
        }
    }

    [Serializable]
    public sealed class GuildNpcDefinitionConfig
    {
        [SerializeField]
        private string _npcId;

        [SerializeField]
        private GuildTextDefinitionConfig _displayName = new();

        [SerializeField]
        private string _dialoguePoolId;

        [SerializeField]
        private string _ambientProfileId;

        internal AmbientNpcSnapshot ToSnapshot(int index)
        {
            var location = $"Guild Hall NPC definition at index {index}";
            var displayName = (_displayName ?? throw new InvalidOperationException(
                    $"{location} has no display name."))
                .ToSnapshot($"{location} display name");
            return new AmbientNpcSnapshot(
                _npcId,
                new AmbientTextSnapshot(displayName.TextId, displayName.DisplayText),
                _dialoguePoolId,
                _ambientProfileId);
        }
    }

    [Serializable]
    public sealed class GuildHallMovementSettingsConfig
    {
        [SerializeField, Min(0.1f)]
        private float _speed = 4f;

        [SerializeField, Min(0.1f)]
        private float _acceleration = 16f;

        [SerializeField, Min(0.01f)]
        private float _interactionScanInterval = 0.1f;

        internal GuildHallMovementSettings ToSnapshot()
        {
            return new GuildHallMovementSettings(
                _speed,
                _acceleration,
                _interactionScanInterval);
        }
    }

    [Serializable]
    public sealed class GuildInteractionLabelsConfig
    {
        [SerializeField]
        private GuildTextDefinitionConfig _npc = new();

        [SerializeField]
        private GuildTextDefinitionConfig _noticeBoard = new();

        [SerializeField]
        private GuildTextDefinitionConfig _reception = new();

        [SerializeField]
        private GuildTextDefinitionConfig _exit = new();

        internal GuildInteractionLabels ToSnapshot()
        {
            return new GuildInteractionLabels(
                Require(_npc, "NPC"),
                Require(_noticeBoard, "Notice Board"),
                Require(_reception, "reception"),
                Require(_exit, "exit"));
        }

        private static GuildTextSnapshot Require(
            GuildTextDefinitionConfig definition,
            string location)
        {
            return (definition ?? throw new InvalidOperationException(
                $"Guild Hall {location} interaction label is missing."))
                .ToSnapshot($"Guild Hall {location} interaction label");
        }
    }

    [Serializable]
    public sealed class NoticeBoardTextConfig
    {
        [SerializeField] private GuildTextDefinitionConfig _header = new();
        [SerializeField] private GuildTextDefinitionConfig _select = new();
        [SerializeField] private GuildTextDefinitionConfig _selected = new();
        [SerializeField] private GuildTextDefinitionConfig _close = new();
        [SerializeField] private GuildTextDefinitionConfig _empty = new();

        internal NoticeBoardTextSnapshot ToSnapshot()
        {
            return new NoticeBoardTextSnapshot(
                Require(_header, "header"),
                Require(_select, "select label"),
                Require(_selected, "selected label"),
                Require(_close, "close label"),
                Require(_empty, "empty state"));
        }

        private static GuildTextSnapshot Require(
            GuildTextDefinitionConfig definition,
            string location)
        {
            return (definition ?? throw new InvalidOperationException(
                $"Guild Hall Notice Board {location} is missing."))
                .ToSnapshot($"Guild Hall Notice Board {location}");
        }
    }

    [Serializable]
    public sealed class RunSummaryTextConfig
    {
        [SerializeField] private GuildTextDefinitionConfig _header = new();
        [SerializeField] private GuildTextDefinitionConfig _completedOutcome = new();
        [SerializeField] private GuildTextDefinitionConfig _defeatedOutcome = new();
        [SerializeField] private GuildTextDefinitionConfig _dungeonLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _rewardsLabel = new();
        [SerializeField] private string _rewardLineFormat = "{0} x{1}";
        [SerializeField] private GuildTextDefinitionConfig _emptyRewards = new();
        [SerializeField] private GuildTextDefinitionConfig _close = new();

        internal GuildRunSummaryTextSnapshot ToSnapshot()
        {
            return new GuildRunSummaryTextSnapshot(
                Require(_header, "header"),
                Require(_completedOutcome, "completed outcome"),
                Require(_defeatedOutcome, "defeated outcome"),
                Require(_dungeonLabel, "dungeon label"),
                Require(_rewardsLabel, "rewards label"),
                _rewardLineFormat,
                Require(_emptyRewards, "empty rewards"),
                Require(_close, "close label"));
        }

        private static GuildTextSnapshot Require(
            GuildTextDefinitionConfig definition,
            string location)
        {
            return (definition ?? throw new InvalidOperationException(
                $"Guild Hall Run Summary {location} is missing."))
                .ToSnapshot($"Guild Hall Run Summary {location}");
        }
    }

    [Serializable]
    public sealed class GuildProfileTextConfig
    {
        [SerializeField] private GuildTextDefinitionConfig _header = new();
        [SerializeField] private GuildTextDefinitionConfig _goldLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _rankLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _unassignedRank = new();
        [SerializeField] private GuildTextDefinitionConfig _leaderLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _leaderExplanation = new();
        [SerializeField] private GuildTextDefinitionConfig _teamLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _rosterLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _availableHeroLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _levelLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _healthLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _speedLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _primarySkillLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _activeSkillLabel = new();
        [SerializeField] private GuildTextDefinitionConfig _close = new();

        internal GuildProfileTextSnapshot ToSnapshot()
        {
            return new GuildProfileTextSnapshot(
                Require(_header, "header"),
                Require(_goldLabel, "Gold label"),
                Require(_rankLabel, "rank label"),
                Require(_unassignedRank, "unassigned rank"),
                Require(_leaderLabel, "leader label"),
                Require(_leaderExplanation, "leader explanation"),
                Require(_teamLabel, "team label"),
                Require(_rosterLabel, "roster label"),
                Require(_availableHeroLabel, "available hero label"),
                Require(_levelLabel, "level label"),
                Require(_healthLabel, "health label"),
                Require(_speedLabel, "speed label"),
                Require(_primarySkillLabel, "primary skill label"),
                Require(_activeSkillLabel, "active skill label"),
                Require(_close, "close label"));
        }

        private static GuildTextSnapshot Require(
            GuildTextDefinitionConfig definition,
            string location)
        {
            return (definition ?? throw new InvalidOperationException(
                    $"Guild Hall Profile {location} is missing."))
                .ToSnapshot($"Guild Hall Profile {location}");
        }
    }
}
