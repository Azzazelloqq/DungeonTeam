using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    public sealed class TeamController : IDisposable
    {
        private const float MovementThreshold = 0.0001f;

        private readonly ActorInstance _leader;
        private readonly IReadOnlyList<ActorInstance> _enemies;
        private readonly Camera _camera;
        private readonly ITickHandler _tickHandler;
        private readonly ITeamCameraInput _cameraInput;
        private readonly TeamControlSettings _settings;
        private readonly List<CompanionController> _companions;
        private readonly float _minimumCommandViewDot;

        private ActorInstance _availableAttackTarget;
        private ActorInstance _combatTarget;
        private float _cameraYaw;
        private bool _isForcedFollow;
        private bool _lastCanOrderAttack;
        private bool _lastCanOrderFollow;
        private bool _isInitialized;
        private bool _isDisposed;

        public event Action CommandsChanged;

        public TeamController(
            ActorInstance leader,
            IReadOnlyList<ActorInstance> companions,
            IReadOnlyList<Vector3> formationOffsets,
            IReadOnlyList<ActorInstance> enemies,
            Camera camera,
            ITickHandler tickHandler,
            ITeamCameraInput cameraInput,
            TeamControlSettings settings)
        {
            _leader = leader ?? throw new ArgumentNullException(nameof(leader));
            if (companions == null)
            {
                throw new ArgumentNullException(nameof(companions));
            }

            if (formationOffsets == null)
            {
                throw new ArgumentNullException(nameof(formationOffsets));
            }

            if (companions.Count != formationOffsets.Count)
            {
                throw new ArgumentException(
                    "Each companion requires one formation offset.",
                    nameof(formationOffsets));
            }

            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _camera = camera != null
                ? camera
                : throw new ArgumentNullException(nameof(camera));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _cameraInput = cameraInput ?? throw new ArgumentNullException(nameof(cameraInput));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _companions = new List<CompanionController>(companions.Count);
            for (var index = 0; index < companions.Count; index++)
            {
                _companions.Add(new CompanionController(
                    companions[index] ?? throw new ArgumentException(
                        $"Companion at index {index} is missing.",
                        nameof(companions)),
                    formationOffsets[index],
                    settings));
            }

            _minimumCommandViewDot = Mathf.Cos(
                _settings.CommandViewAngle * 0.5f * Mathf.Deg2Rad);
            _cameraYaw = _settings.CameraInitialYaw;
        }

        public bool CanOrderAttack =>
            HasLivingCompanion() && _combatTarget == null && _availableAttackTarget != null;

        public bool CanOrderFollow =>
            HasLivingCompanion() &&
            !_isForcedFollow &&
            (_combatTarget != null || _availableAttackTarget != null);

        public void Initialize()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TeamController));
            }

            if (_isInitialized)
            {
                throw new InvalidOperationException("Team Controller is already initialized.");
            }

            _leader.AttackedBy += OnTeamMemberAttacked;
            for (var index = 0; index < _companions.Count; index++)
            {
                _companions[index].Actor.AttackedBy += OnTeamMemberAttacked;
            }

            RefreshAvailableAttackTarget();
            StoreCommandAvailability();
            PositionCamera(immediate: true, deltaTime: 0f);
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
            _tickHandler.SubscribeOnFrameLateUpdate(OnFrameLateUpdate);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _leader.AttackedBy -= OnTeamMemberAttacked;
            for (var index = _companions.Count - 1; index >= 0; index--)
            {
                _companions[index].Actor.AttackedBy -= OnTeamMemberAttacked;
                _companions[index].Dispose();
            }

            _companions.Clear();
            if (_isInitialized)
            {
                _tickHandler.UnsubscribeOnFrameLateUpdate(OnFrameLateUpdate);
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
            }

            _availableAttackTarget = null;
            _combatTarget = null;
            CommandsChanged = null;
        }

        public bool TryOrderAttack()
        {
            if (!CanOrderAttack || !IsVisibleToTeam(_availableAttackTarget, out _))
            {
                RefreshAvailableAttackTarget();
                PublishCommandAvailabilityIfChanged();
                return false;
            }

            _combatTarget = _availableAttackTarget;
            _availableAttackTarget = null;
            _isForcedFollow = false;
            PublishCommandAvailabilityIfChanged();
            return true;
        }

        public void OrderFollow()
        {
            _isForcedFollow = true;
            _combatTarget = null;
            RefreshAvailableAttackTarget();
            PublishCommandAvailabilityIfChanged();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            UpdateCombatTarget();
            RefreshAvailableAttackTarget();
            for (var index = 0; index < _companions.Count; index++)
            {
                _companions[index].Tick(
                    deltaTime,
                    _leader,
                    _combatTarget,
                    _isForcedFollow);
            }

            PublishCommandAvailabilityIfChanged();
        }

        private void OnFrameLateUpdate(float deltaTime)
        {
            _cameraYaw += _cameraInput.CameraYawDelta * _settings.MouseYawSensitivity;
            PositionCamera(immediate: false, deltaTime);
        }

        private void UpdateCombatTarget()
        {
            if (_combatTarget == null)
            {
                return;
            }

            if (!_combatTarget.IsAlive || !CanAnyCompanionContinueCombat(_combatTarget))
            {
                _combatTarget = null;
            }
        }

        private bool CanAnyCompanionContinueCombat(ActorInstance target)
        {
            var maximumDistanceSqr = _settings.CompanionTargetLossDistance *
                                     _settings.CompanionTargetLossDistance;
            for (var index = 0; index < _companions.Count; index++)
            {
                var companion = _companions[index].Actor;
                if (companion.IsAlive &&
                    PlanarSqrDistance(companion.Position, target.Position) <= maximumDistanceSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshAvailableAttackTarget()
        {
            if (_combatTarget != null || !HasLivingCompanion())
            {
                _availableAttackTarget = null;
                return;
            }

            ActorInstance nearest = null;
            var nearestDistanceSqr = float.MaxValue;
            for (var index = 0; index < _enemies.Count; index++)
            {
                var candidate = _enemies[index];
                if (!IsVisibleToTeam(candidate, out var distanceSqr) ||
                    distanceSqr >= nearestDistanceSqr)
                {
                    continue;
                }

                nearest = candidate;
                nearestDistanceSqr = distanceSqr;
            }

            _availableAttackTarget = nearest;
        }

        private bool IsVisibleToTeam(ActorInstance candidate, out float distanceSqr)
        {
            distanceSqr = float.MaxValue;
            if (candidate == null || !candidate.IsAlive)
            {
                return false;
            }

            var isVisible = CanSee(_leader, candidate, out distanceSqr);
            for (var index = 0; index < _companions.Count; index++)
            {
                if (!CanSee(_companions[index].Actor, candidate, out var companionDistanceSqr))
                {
                    continue;
                }

                distanceSqr = Mathf.Min(distanceSqr, companionDistanceSqr);
                isVisible = true;
            }

            return isVisible;
        }

        private bool CanSee(
            ActorInstance observer,
            ActorInstance candidate,
            out float distanceSqr)
        {
            distanceSqr = float.MaxValue;
            if (!observer.IsAlive)
            {
                return false;
            }

            var direction = candidate.Position - observer.Position;
            direction.y = 0f;
            distanceSqr = direction.sqrMagnitude;
            if (distanceSqr > _settings.CommandRange * _settings.CommandRange)
            {
                return false;
            }

            if (distanceSqr > MovementThreshold)
            {
                direction.Normalize();
                var forward = observer.Forward;
                forward.y = 0f;
                forward.Normalize();
                if (Vector3.Dot(forward, direction) < _minimumCommandViewDot)
                {
                    return false;
                }
            }

            return HasClearLine(observer, candidate);
        }

        private bool HasClearLine(ActorInstance observer, ActorInstance candidate)
        {
            var eyeOffset = Vector3.up * _settings.CommandEyeHeight;
            return !Physics.Linecast(
                observer.Position + eyeOffset,
                candidate.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private void OnTeamMemberAttacked(ActorInstance attacker)
        {
            if (_isForcedFollow ||
                _combatTarget != null ||
                !HasLivingCompanion() ||
                attacker == null ||
                !attacker.IsAlive ||
                !IsEnemy(attacker))
            {
                return;
            }

            _combatTarget = attacker;
            _availableAttackTarget = null;
            PublishCommandAvailabilityIfChanged();
        }

        private bool IsEnemy(ActorInstance actor)
        {
            for (var index = 0; index < _enemies.Count; index++)
            {
                if (ReferenceEquals(_enemies[index], actor))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasLivingCompanion()
        {
            for (var index = 0; index < _companions.Count; index++)
            {
                if (_companions[index].Actor.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        private void StoreCommandAvailability()
        {
            _lastCanOrderAttack = CanOrderAttack;
            _lastCanOrderFollow = CanOrderFollow;
        }

        private void PublishCommandAvailabilityIfChanged()
        {
            var canOrderAttack = CanOrderAttack;
            var canOrderFollow = CanOrderFollow;
            if (canOrderAttack == _lastCanOrderAttack &&
                canOrderFollow == _lastCanOrderFollow)
            {
                return;
            }

            _lastCanOrderAttack = canOrderAttack;
            _lastCanOrderFollow = canOrderFollow;
            CommandsChanged?.Invoke();
        }

        private void PositionCamera(bool immediate, float deltaTime)
        {
            var target = _leader.Position + Vector3.up * _settings.CameraTargetHeight;
            var targetRotation = Quaternion.Euler(
                _settings.CameraPitch,
                _cameraYaw,
                0f);
            var targetPosition = target + targetRotation * Vector3.back * _settings.CameraDistance;

            if (immediate)
            {
                _camera.transform.SetPositionAndRotation(targetPosition, targetRotation);
                return;
            }

            var blend = 1f - Mathf.Exp(-_settings.CameraFollowSharpness * deltaTime);
            _camera.transform.SetPositionAndRotation(
                Vector3.Lerp(_camera.transform.position, targetPosition, blend),
                Quaternion.Slerp(_camera.transform.rotation, targetRotation, blend));
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
