using System;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.UI.CombatHud.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    [Serializable]
    public sealed class DungeonRunBindings
    {
        [SerializeField]
        private ContextActionsViewBase _contextActionsPrefab;

        [SerializeField]
        private CombatHudViewBase _combatHudPrefab;

        [SerializeField, Min(0.1f)]
        private float _rewardPickupDistance = 2f;

        [SerializeField, Min(0.1f)]
        private float _chestOpenDistance = 2f;

        [SerializeField, Min(0.1f)]
        private float _exitDistance = 2f;

        [SerializeField, Min(0.1f)]
        private float _companionSpawnSpacing = 1.25f;

        [SerializeField, Min(0.1f)]
        private float _companionSpawnRowSpacing = 1.25f;

        [SerializeField, Range(0.01f, 0.5f)]
        [Tooltip("Wall cutout radius relative to screen height.")]
        private float _wallOcclusionRadius = 0.18f;

        [SerializeField, Range(0f, 0.2f)]
        [Tooltip("Dithered cutout edge width relative to screen height.")]
        private float _wallOcclusionFeather = 0.025f;

        [SerializeField, Min(0f)]
        [Tooltip("Minimum camera-space gap between a wall and a hero.")]
        private float _wallOcclusionDepthBias = 0.1f;

        [SerializeField, Min(0f)]
        [Tooltip("Vertical offset from each hero origin to the cutout center.")]
        private float _wallOcclusionTargetHeight = 1f;

        public DungeonRunBindings()
        {
        }

        public DungeonRunBindings(
            ContextActionsViewBase contextActionsPrefab,
            CombatHudViewBase combatHudPrefab)
        {
            _contextActionsPrefab = contextActionsPrefab != null
                ? contextActionsPrefab
                : throw new ArgumentNullException(nameof(contextActionsPrefab));
            _combatHudPrefab = combatHudPrefab != null
                ? combatHudPrefab
                : throw new ArgumentNullException(nameof(combatHudPrefab));
        }

        internal ContextActionsViewBase ContextActionsPrefab => _contextActionsPrefab;
        internal CombatHudViewBase CombatHudPrefab => _combatHudPrefab;
        internal float RewardPickupDistance => _rewardPickupDistance;
        internal float ChestOpenDistance => _chestOpenDistance;
        internal float ExitDistance => _exitDistance;
        internal Vector3 GetCompanionSpawnOffset(int companionIndex)
        {
            if (companionIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(companionIndex));
            }

            var row = companionIndex / 2 + 1;
            var side = companionIndex % 2 == 0 ? 1f : -1f;
            return new Vector3(
                side * _companionSpawnSpacing,
                0f,
                -row * _companionSpawnRowSpacing);
        }
        internal float WallOcclusionRadius => _wallOcclusionRadius;
        internal float WallOcclusionFeather => _wallOcclusionFeather;
        internal float WallOcclusionDepthBias => _wallOcclusionDepthBias;
        internal float WallOcclusionTargetHeight => _wallOcclusionTargetHeight;

        internal void Validate()
        {
            if (_contextActionsPrefab == null)
            {
                throw new InvalidOperationException(
                    "Dungeon Run requires a Context Actions prefab binding.");
            }

            if (_combatHudPrefab == null)
            {
                throw new InvalidOperationException(
                    "Dungeon Run requires a Combat HUD prefab binding.");
            }

            if (_rewardPickupDistance <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run reward pickup distance must be positive.");
            }

            if (_chestOpenDistance <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run chest open distance must be positive.");
            }

            if (_exitDistance <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run exit distance must be positive.");
            }

            if (_companionSpawnSpacing <= 0f || _companionSpawnRowSpacing <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run companion spawn spacing must be positive.");
            }

            ValidateWallOcclusionSettings();
        }

        internal void ValidateWallOcclusionSettings()
        {
            if (_wallOcclusionRadius <= 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run wall occlusion radius must be positive.");
            }

            if (_wallOcclusionFeather < 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run wall occlusion feather cannot be negative.");
            }

            if (_wallOcclusionDepthBias < 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run wall occlusion depth bias cannot be negative.");
            }

            if (_wallOcclusionTargetHeight < 0f)
            {
                throw new InvalidOperationException(
                    "Dungeon Run wall occlusion target height cannot be negative.");
            }
        }
    }
}
