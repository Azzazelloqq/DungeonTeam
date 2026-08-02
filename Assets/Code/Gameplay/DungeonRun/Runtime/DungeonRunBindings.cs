using System;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.DungeonRun.Runtime
{
    [Serializable]
    public sealed class DungeonRunBindings
    {
        [SerializeField]
        private ActorViewBase _actorPrefab;

        [SerializeField]
        private ContextActionsViewBase _contextActionsPrefab;

        [SerializeField]
        private GreyboxActorSettings _leader = new(
            maximumHealth: 100,
            movementSpeed: 4f,
            color: new Color(0.2f, 0.55f, 1f));

        [SerializeField]
        private GreyboxActorSettings _companion = new(
            maximumHealth: 80,
            movementSpeed: 4f,
            color: new Color(0.2f, 1f, 0.4f));

        [SerializeField]
        private GreyboxActorSettings _enemy = new(
            maximumHealth: 60,
            movementSpeed: 3.5f,
            color: new Color(1f, 0.25f, 0.2f));

        [SerializeField]
        private Vector3 _companionOffset = new(1.25f, 0f, -1.25f);

        public DungeonRunBindings()
        {
        }

        public DungeonRunBindings(
            ActorViewBase actorPrefab,
            ContextActionsViewBase contextActionsPrefab)
        {
            _actorPrefab = actorPrefab != null
                ? actorPrefab
                : throw new ArgumentNullException(nameof(actorPrefab));
            _contextActionsPrefab = contextActionsPrefab != null
                ? contextActionsPrefab
                : throw new ArgumentNullException(nameof(contextActionsPrefab));
        }

        internal ActorViewBase ActorPrefab => _actorPrefab;
        internal ContextActionsViewBase ContextActionsPrefab => _contextActionsPrefab;
        internal GreyboxActorSettings Leader => _leader;
        internal GreyboxActorSettings Companion => _companion;
        internal GreyboxActorSettings Enemy => _enemy;
        internal Vector3 CompanionOffset => _companionOffset;

        internal void Validate()
        {
            if (_actorPrefab == null)
            {
                throw new InvalidOperationException("Dungeon Run requires an Actor prefab binding.");
            }

            if (_contextActionsPrefab == null)
            {
                throw new InvalidOperationException(
                    "Dungeon Run requires a Context Actions prefab binding.");
            }

            RequireSettings(_leader, nameof(_leader)).Validate(nameof(_leader));
            RequireSettings(_companion, nameof(_companion)).Validate(nameof(_companion));
            RequireSettings(_enemy, nameof(_enemy)).Validate(nameof(_enemy));
        }

        private static GreyboxActorSettings RequireSettings(
            GreyboxActorSettings settings,
            string fieldName)
        {
            return settings ?? throw new InvalidOperationException(
                $"Dungeon Run actor settings '{fieldName}' are missing.");
        }
    }

    [Serializable]
    internal sealed class GreyboxActorSettings
    {
        [SerializeField, Min(1)]
        private int _maximumHealth = 1;

        [SerializeField, Min(0.1f)]
        private float _movementSpeed = 1f;

        [SerializeField]
        private Color _color = Color.white;

        public GreyboxActorSettings(int maximumHealth, float movementSpeed, Color color)
        {
            _maximumHealth = maximumHealth;
            _movementSpeed = movementSpeed;
            _color = color;
        }

        public int MaximumHealth => _maximumHealth;
        public float MovementSpeed => _movementSpeed;
        public Color Color => _color;

        public void Validate(string fieldName)
        {
            if (_maximumHealth <= 0)
            {
                throw new InvalidOperationException(
                    $"Dungeon Run actor settings '{fieldName}' require positive health.");
            }

            if (_movementSpeed <= 0f)
            {
                throw new InvalidOperationException(
                    $"Dungeon Run actor settings '{fieldName}' require positive movement speed.");
            }
        }
    }
}
