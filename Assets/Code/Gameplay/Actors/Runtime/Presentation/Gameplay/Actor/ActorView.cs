using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor.Base;
using UnityEngine;
using UnityEngine.AI;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    [RequireComponent(typeof(NavMeshAgent), typeof(ActorCombatFeedback))]
    public sealed class ActorView : ActorViewBase
    {
        private static readonly Color DeadColor = new(0.2f, 0.2f, 0.2f, 1f);

        [SerializeField]
        private NavMeshAgent _agent;

        [SerializeField]
        private Renderer[] _colorRenderers = Array.Empty<Renderer>();

        [SerializeField]
        private ActorCombatFeedback _combatFeedback;

        private float _movementSpeed;
        private bool _isDirectlyControlled;

        public override Vector3 Position => transform.position;

        public override Vector3 Forward => transform.forward;

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
            _combatFeedback.Configure(_colorRenderers, color);
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

        public override void PlayAttackFeedback()
        {
            _combatFeedback.PlayAttack();
        }

        public override void SetTargetHighlighted(bool isHighlighted)
        {
            _combatFeedback.SetTargetHighlighted(isHighlighted);
        }

        public override void PlayDamageFeedback(int amount)
        {
            _combatFeedback.PlayDamage(amount);
        }

        public override void PlayDeathFeedback()
        {
            StopMovement();
            _agent.enabled = false;
            _combatFeedback.PlayDeath(DeadColor);
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

            if (_combatFeedback == null)
            {
                throw new InvalidOperationException(
                    "Actor View requires an Actor Combat Feedback binding.");
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
