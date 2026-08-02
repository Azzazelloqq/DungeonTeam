using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class ActorView : ActorViewBase
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private NavMeshAgent _agent;

        [SerializeField]
        private Renderer[] _colorRenderers = Array.Empty<Renderer>();

        private MaterialPropertyBlock _propertyBlock;
        private float _movementSpeed;
        private bool _isDirectlyControlled;

        public override Vector3 Position => transform.position;

        public override bool IsOnNavMesh =>
            _agent != null && _agent.enabled && _agent.isOnNavMesh;

        public override void Configure(Color color, float movementSpeed)
        {
            if (movementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            _agent.speed = movementSpeed;
            _movementSpeed = movementSpeed;
            _propertyBlock ??= new MaterialPropertyBlock();

            for (var index = 0; index < _colorRenderers.Length; index++)
            {
                var targetRenderer = _colorRenderers[index];
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColor, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        public override bool TryMoveTo(Vector3 destination)
        {
            if (!IsOnNavMesh)
            {
                return false;
            }

            _isDirectlyControlled = false;
            return _agent.SetDestination(destination);
        }

        public override bool SetMoveDirection(Vector3 direction)
        {
            if (!IsOnNavMesh)
            {
                return false;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                StopMovement();
                return true;
            }

            if (!_isDirectlyControlled)
            {
                _agent.ResetPath();
                _isDirectlyControlled = true;
            }

            _agent.velocity = Vector3.ClampMagnitude(direction, 1f) * _movementSpeed;
            return true;
        }

        public override void StopMovement()
        {
            if (IsOnNavMesh)
            {
                _agent.ResetPath();
                _agent.velocity = Vector3.zero;
            }

            _isDirectlyControlled = false;
        }

        public override void ShowDead()
        {
            StopMovement();
            _agent.enabled = false;
        }

        protected override void OnInitialize()
        {
            if (_agent == null)
            {
                throw new InvalidOperationException("Actor View requires a NavMeshAgent binding.");
            }

            if (_colorRenderers == null || _colorRenderers.Length == 0)
            {
                throw new InvalidOperationException("Actor View requires at least one color renderer.");
            }

            for (var index = 0; index < _colorRenderers.Length; index++)
            {
                if (_colorRenderers[index] == null)
                {
                    throw new InvalidOperationException(
                        $"Actor View color renderer at index {index} is missing.");
                }
            }
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
