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
        private string _defaultLeaderActorId;

        [SerializeField]
        private string[] _defaultCompanionActorIds = Array.Empty<string>();

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
                members[index] = new DungeonRunTeamMemberOption(
                    actor.ActorId,
                    actor.DisplayName);
            }

            return new DungeonRunTeamSetup(
                members,
                _minimumTeamSize,
                _maximumTeamSize,
                new DungeonRunTeamSelection(
                    _defaultLeaderActorId,
                    _defaultCompanionActorIds));
        }
    }
}
