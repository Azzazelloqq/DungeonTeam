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
        public const string MoveSpeedParameter = "MoveSpeed";
        public const string AttackParameter = "Attack";
        public const string CastParameter = "Cast";
        public const string DeathParameter = "Death";

        private static readonly int MoveSpeedHash = Animator.StringToHash(MoveSpeedParameter);
        private static readonly int AttackHash = Animator.StringToHash(AttackParameter);
        private static readonly int CastHash = Animator.StringToHash(CastParameter);
        private static readonly int DeathHash = Animator.StringToHash(DeathParameter);

        [SerializeField]
        private NavMeshAgent _agent;

        [SerializeField]
        private ActorCombatFeedback _combatFeedback;

        [SerializeField, Tooltip(
            "Optional. Controller parameters: Float MoveSpeed; Triggers Attack, Cast and Death. " +
            "Root Motion must be disabled.")]
        private Animator _animator;

        [SerializeField, Tooltip("Optional attachment point for the equipped weapon.")]
        private Transform _weaponAnchor;

        [SerializeField, Tooltip("Optional origin for hit and damage VFX.")]
        private Transform _hitVfxAnchor;

        [SerializeField, Tooltip("Optional anchor for health bars and other overhead UI.")]
        private Transform _overheadAnchor;

        [SerializeField, Tooltip("Required origin for actor-independent skill visuals.")]
        private Transform _skillOriginAnchor;

        [SerializeField, Min(0f)]
        private float _moveAnimationDampTime = 0.1f;

        private float _movementSpeed;
        private bool _isDirectlyControlled;

        public override Vector3 Position => transform.position;

        public override Vector3 Forward => transform.forward;

        public override bool IsOnNavMesh =>
            _agent != null && _agent.enabled && _agent.isOnNavMesh;

        public override Transform WeaponAnchor => _weaponAnchor;

        public override Transform HitVfxAnchor => _hitVfxAnchor;

        public override Transform OverheadAnchor => _overheadAnchor;

        public override Transform SkillOriginAnchor => _skillOriginAnchor;

        public override void Configure(float movementSpeed)
        {
            if (movementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(movementSpeed));
            }

            _agent.speed = movementSpeed;
            _movementSpeed = movementSpeed;
            _combatFeedback.Configure();
            if (_animator != null)
            {
                _animator.SetFloat(MoveSpeedHash, 0f);
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

        public override bool TryFaceTowards(Vector3 targetPosition)
        {
            var direction = targetPosition - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
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
            _animator?.SetTrigger(AttackHash);
        }

        public override void PlayCastFeedback()
        {
            _animator?.SetTrigger(CastHash);
        }

        public override void PlayDamageFeedback(int amount, bool isFatal)
        {
            _combatFeedback.PlayDamage(amount);
        }

        public override void PlayDeathFeedback()
        {
            StopMovement();
            _agent.enabled = false;
            if (_animator != null)
            {
                _animator.SetFloat(MoveSpeedHash, 0f);
                _animator.ResetTrigger(AttackHash);
                _animator.ResetTrigger(CastHash);
                _animator.SetTrigger(DeathHash);
            }

            enabled = false;
        }

        protected override void OnInitialize()
        {
            if (_agent == null)
            {
                throw new InvalidOperationException("Actor View requires a NavMeshAgent binding.");
            }

            if (_combatFeedback == null)
            {
                throw new InvalidOperationException(
                    "Actor View requires an Actor Combat Feedback binding.");
            }

            if (_skillOriginAnchor == null)
            {
                throw new InvalidOperationException(
                    "Actor View requires a Skill Origin Anchor binding.");
            }

            ValidateAnimator();
            enabled = _animator != null;
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

        private void Update()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _movementSpeed <= 0f)
            {
                return;
            }

            var normalizedSpeed = IsOnNavMesh
                ? Mathf.Clamp01(_agent.velocity.magnitude / _movementSpeed)
                : 0f;
            _animator.SetFloat(
                MoveSpeedHash,
                normalizedSpeed,
                _moveAnimationDampTime,
                Time.deltaTime);
        }

        private void ValidateAnimator()
        {
            if (_animator == null)
            {
                return;
            }

            if (_animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Actor Animator Root Motion must be disabled because movement is " +
                    "gameplay-authoritative.");
            }

            RequireAnimatorParameter(
                MoveSpeedParameter,
                MoveSpeedHash,
                AnimatorControllerParameterType.Float);
            RequireAnimatorParameter(
                AttackParameter,
                AttackHash,
                AnimatorControllerParameterType.Trigger);
            RequireAnimatorParameter(
                CastParameter,
                CastHash,
                AnimatorControllerParameterType.Trigger);
            RequireAnimatorParameter(
                DeathParameter,
                DeathHash,
                AnimatorControllerParameterType.Trigger);
        }

        private void RequireAnimatorParameter(
            string parameterName,
            int parameterHash,
            AnimatorControllerParameterType expectedType)
        {
            var parameters = _animator.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (parameter.nameHash == parameterHash && parameter.type == expectedType)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Actor Animator requires parameter '{parameterName}' of type " +
                $"'{expectedType}'.");
        }
    }
}
