using System;
using Code.Configuration;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Config
{
    [CreateAssetMenu(menuName = "DungeonTeam/Gameplay/Ambient NPC Config", fileName = "AmbientNpcConfig")]
    public sealed class AmbientNpcConfigPage : ConfigPage
    {
        [SerializeField]
        private AmbientNpcProfileDefinitionConfig[] _profiles = Array.Empty<AmbientNpcProfileDefinitionConfig>();

        public AmbientNpcProfileCatalog CreateCatalog()
        {
            if (_profiles == null)
            {
                throw new InvalidOperationException("Ambient NPC profiles cannot be null.");
            }

            var profiles = new AmbientNpcProfileSnapshot[_profiles.Length];
            for (var index = 0; index < _profiles.Length; index++)
            {
                profiles[index] = (_profiles[index] ?? throw new InvalidOperationException(
                        $"Ambient NPC profile at index {index} is missing."))
                    .ToSnapshot();
            }

            return new AmbientNpcProfileCatalog(profiles);
        }
    }

    [Serializable]
    public sealed class AmbientNpcProfileDefinitionConfig
    {
        [SerializeField] private string _ambientProfileId;
        [SerializeField, Min(0.01f)] private float _movementSpeed = 1.5f;
        [SerializeField, Min(1f)] private float _turnSpeed = 360f;
        [SerializeField, Min(0f)] private float _idleDurationMin = 1f;
        [SerializeField, Min(0f)] private float _idleDurationMax = 2f;
        [SerializeField, Min(0f)] private float _activityDurationMin = 1f;
        [SerializeField, Min(0f)] private float _activityDurationMax = 2f;
        [SerializeField] private bool _usesAuthoredRoute;

        internal AmbientNpcProfileSnapshot ToSnapshot()
        {
            return new AmbientNpcProfileSnapshot(
                _ambientProfileId,
                _movementSpeed,
                _turnSpeed,
                _idleDurationMin,
                _idleDurationMax,
                _activityDurationMin,
                _activityDurationMax,
                _usesAuthoredRoute);
        }
    }
}
