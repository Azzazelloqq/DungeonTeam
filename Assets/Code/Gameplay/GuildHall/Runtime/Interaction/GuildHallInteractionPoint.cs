using System;
using DungeonTeam.Gameplay.GuildHall.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Interaction
{
    public sealed class GuildHallInteractionPoint : MonoBehaviour
    {
        [SerializeField]
        private string _semanticId;

        [SerializeField]
        private GuildInteractionKind _kind;

        [SerializeField]
        private Transform _anchor;

        [SerializeField, Min(0.1f)]
        private float _radius = 2f;

        public string SemanticId => _semanticId;
        public GuildInteractionKind Kind => _kind;
        public Transform Anchor => _anchor;
        public float Radius => _radius;

        internal void Configure(
            string semanticId,
            GuildInteractionKind kind,
            Transform anchor,
            float radius)
        {
            _semanticId = semanticId;
            _kind = kind;
            _anchor = anchor;
            _radius = radius;
        }

        internal void Validate(int index)
        {
            if (string.IsNullOrWhiteSpace(_semanticId))
            {
                throw new InvalidOperationException(
                    $"Guild Hall interaction at index {index} has an empty semantic ID.");
            }

            if (!Enum.IsDefined(typeof(GuildInteractionKind), _kind))
            {
                throw new InvalidOperationException(
                    $"Guild Hall interaction '{_semanticId}' has an unsupported kind '{_kind}'.");
            }

            if (_anchor == null)
            {
                throw new InvalidOperationException(
                    $"Guild Hall interaction '{_semanticId}' has no anchor.");
            }

            if (_radius <= 0f)
            {
                throw new InvalidOperationException(
                    $"Guild Hall interaction '{_semanticId}' must have a positive radius.");
            }
        }

        internal bool IsInRange(Vector3 playerPosition)
        {
            var difference = _anchor.position - playerPosition;
            difference.y = 0f;
            return difference.sqrMagnitude <= _radius * _radius;
        }

        internal float SqrDistance(Vector3 playerPosition)
        {
            var difference = _anchor.position - playerPosition;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
