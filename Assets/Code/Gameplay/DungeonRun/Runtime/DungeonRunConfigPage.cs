using System;
using Code.Configuration;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.DungeonRun.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Dungeon Run Config",
        fileName = "DungeonRunConfig")]
    public sealed class DungeonRunConfigPage : ConfigPage
    {
        [SerializeField]
        private string[] _allowedTeamActorIds = Array.Empty<string>();

        [SerializeField, Min(1)]
        private int _minimumTeamSize = 2;

        [SerializeField, Min(1)]
        private int _maximumTeamSize = 4;

        [SerializeField]
        private DungeonRunActorSelectionConfig _defaultLeader;

        [SerializeField]
        private DungeonRunActorSelectionConfig[] _defaultCompanions =
            Array.Empty<DungeonRunActorSelectionConfig>();

        public DungeonRunTeamSetup CreateTeamSetup(ActorConfigCatalog actorCatalog)
        {
            if (actorCatalog == null)
            {
                throw new ArgumentNullException(nameof(actorCatalog));
            }

            var members = new DungeonRunTeamMemberOption[_allowedTeamActorIds.Length];
            for (var index = 0; index < _allowedTeamActorIds.Length; index++)
            {
                var actor = actorCatalog.Require(_allowedTeamActorIds[index]);
                var levels = new int[actor.Levels.Count];
                for (var levelIndex = 0; levelIndex < actor.Levels.Count; levelIndex++)
                {
                    levels[levelIndex] = actor.Levels[levelIndex].Level;
                }

                members[index] = new DungeonRunTeamMemberOption(
                    actor.ActorId,
                    actor.DisplayName,
                    levels);
            }

            return new DungeonRunTeamSetup(
                members,
                _minimumTeamSize,
                _maximumTeamSize,
                CreateDefaultSelection());
        }

        private DungeonRunTeamSelection CreateDefaultSelection()
        {
            if (_defaultLeader == null)
            {
                throw new InvalidOperationException("Default team leader is required.");
            }

            if (_defaultCompanions == null)
            {
                throw new InvalidOperationException("Default team companions cannot be null.");
            }

            var companions = new DungeonRunActorSelection[_defaultCompanions.Length];
            for (var index = 0; index < companions.Length; index++)
            {
                var companion = _defaultCompanions[index] ?? throw new InvalidOperationException(
                    $"Default team companion at index {index} is missing.");
                companions[index] = companion.ToDomain();
            }

            return new DungeonRunTeamSelection(
                _defaultLeader.ToDomain(),
                companions);
        }
    }

    [Serializable]
    public sealed class DungeonRunActorSelectionConfig
    {
        [SerializeField]
        private string _actorId;

        [SerializeField, Min(1)]
        private int _level = 1;

        internal DungeonRunActorSelection ToDomain()
        {
            return new DungeonRunActorSelection(_actorId, _level);
        }
    }
}
