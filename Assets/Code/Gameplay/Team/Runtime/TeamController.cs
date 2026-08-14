using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Team.Domain;
using TickHandler;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    public sealed class TeamController : IDisposable
    {
        private const float MovementThreshold = 0.0001f;

        private readonly ActorInstance _leader;
        private readonly IReadOnlyList<ActorInstance> _enemies;
        private readonly ITickHandler _tickHandler;
        private readonly TeamControlSettings _settings;
        private readonly List<CompanionController> _companions;
        private readonly CompanionHealthSnapshot[] _healthSnapshots;
        private readonly float _minimumCommandViewDot;

        private ActorInstance _availableAttackTarget;
        private ActorInstance _orderedAttackTarget;
        private ActorInstance _retaliationTarget;
        private Vector3[] _tacticalAnchors;
        private CompanionCommandMode _commandMode;
        private bool _lastCanOrderAttack;
        private bool _lastCanOrderFollow;
        private bool _isInitialized;
        private bool _isDisposed;

        public event Action CommandsChanged;

        public TeamController(
            ActorInstance leader,
            IReadOnlyList<ActorInstance> companions,
            IReadOnlyList<ActorCombatController> companionCombatControllers,
            IReadOnlyList<Vector3> formationOffsets,
            IReadOnlyList<ActorInstance> enemies,
            ITickHandler tickHandler,
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

            if (companionCombatControllers == null)
            {
                throw new ArgumentNullException(nameof(companionCombatControllers));
            }

            if (companions.Count != formationOffsets.Count ||
                companions.Count != companionCombatControllers.Count)
            {
                throw new ArgumentException(
                    "Each companion requires one formation offset.",
                    nameof(formationOffsets));
            }

            _enemies = enemies ?? throw new ArgumentNullException(nameof(enemies));
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _settings.Validate();

            _companions = new List<CompanionController>(companions.Count);
            _healthSnapshots = new CompanionHealthSnapshot[companions.Count + 1];
            for (var index = 0; index < companions.Count; index++)
            {
                _companions.Add(new CompanionController(
                    companions[index] ?? throw new ArgumentException(
                        $"Companion at index {index} is missing.",
                        nameof(companions)),
                    formationOffsets[index],
                    settings,
                    companionCombatControllers[index]));
            }

            _minimumCommandViewDot = Mathf.Cos(
                _settings.CommandViewAngle * 0.5f * Mathf.Deg2Rad);
        }

        public bool CanOrderAttack =>
            HasLivingCompanion() &&
            _commandMode != CompanionCommandMode.Attack &&
            _availableAttackTarget != null;

        public bool CanOrderFollow =>
            HasLivingCompanion() &&
            _commandMode != CompanionCommandMode.Follow &&
            (_commandMode == CompanionCommandMode.Attack ||
             _retaliationTarget != null ||
             HasCompanionPreCommitAction());

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
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
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
                _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
            }

            _availableAttackTarget = null;
            _orderedAttackTarget = null;
            _retaliationTarget = null;
            _tacticalAnchors = null;
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

            _commandMode = CompanionCommandMode.Attack;
            _orderedAttackTarget = _availableAttackTarget;
            _retaliationTarget = null;
            _availableAttackTarget = null;
            CancelCompanionPreCommitActions();
            PublishCommandAvailabilityIfChanged();
            return true;
        }

        public void OrderFollow()
        {
            _commandMode = CompanionCommandMode.Follow;
            _orderedAttackTarget = null;
            _retaliationTarget = null;
            CancelCompanionPreCommitActions();
            RefreshAvailableAttackTarget();
            PublishCommandAvailabilityIfChanged();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            UpdateCommandTargets();
            RefreshAvailableAttackTarget();
            var healTarget = _commandMode == CompanionCommandMode.Autonomous
                ? SelectHealTarget()
                : null;
            var attackTarget = _commandMode == CompanionCommandMode.Attack
                ? _orderedAttackTarget
                : _retaliationTarget;
            var isRecallComplete = _commandMode == CompanionCommandMode.Follow;
            for (var index = 0; index < _companions.Count; index++)
            {
                isRecallComplete &= _companions[index].Tick(
                    deltaTime,
                    _leader,
                    healTarget,
                    attackTarget,
                    _commandMode,
                    _tacticalAnchors == null ? null : _tacticalAnchors[index]);
            }

            if (_commandMode == CompanionCommandMode.Follow && isRecallComplete)
            {
                _commandMode = CompanionCommandMode.Autonomous;
                RefreshAvailableAttackTarget();
            }

            PublishCommandAvailabilityIfChanged();
        }

        public void SetTacticalAnchors(IReadOnlyList<Vector3> anchors)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (anchors.Count != _companions.Count)
            {
                throw new ArgumentException(
                    "Each companion requires one tactical anchor.",
                    nameof(anchors));
            }

            _tacticalAnchors = new Vector3[anchors.Count];
            for (var index = 0; index < anchors.Count; index++)
            {
                _tacticalAnchors[index] = anchors[index];
            }
        }

        public void ClearTacticalAnchors()
        {
            _tacticalAnchors = null;
        }

        private void UpdateCommandTargets()
        {
            if (_orderedAttackTarget != null &&
                (!_orderedAttackTarget.IsAlive ||
                 !CanAnyCompanionContinueCombat(_orderedAttackTarget)))
            {
                _orderedAttackTarget = null;
                if (_commandMode == CompanionCommandMode.Attack)
                {
                    _commandMode = CompanionCommandMode.Autonomous;
                }
            }

            if (_retaliationTarget != null &&
                (!_retaliationTarget.IsAlive ||
                 !CanAnyCompanionContinueCombat(_retaliationTarget)))
            {
                _retaliationTarget = null;
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
            if (_commandMode == CompanionCommandMode.Attack || !HasLivingCompanion())
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
            if (_commandMode != CompanionCommandMode.Autonomous ||
                !HasLivingCompanion() ||
                attacker == null ||
                !attacker.IsAlive ||
                !IsEnemy(attacker))
            {
                return;
            }

            _retaliationTarget = attacker;
            PublishCommandAvailabilityIfChanged();
        }

        private ActorInstance SelectHealTarget()
        {
            _healthSnapshots[0] = CreateHealthSnapshot(_leader);
            for (var index = 0; index < _companions.Count; index++)
            {
                _healthSnapshots[index + 1] =
                    CreateHealthSnapshot(_companions[index].Actor);
            }

            var selectedIndex = CompanionHealTargetSelector.Select(
                _healthSnapshots,
                _settings.CompanionHealHealthRatio);
            if (selectedIndex < 0)
            {
                return null;
            }

            return selectedIndex == 0
                ? _leader
                : _companions[selectedIndex - 1].Actor;
        }

        private void CancelCompanionPreCommitActions()
        {
            for (var index = 0; index < _companions.Count; index++)
            {
                _companions[index].CancelPreCommitAction();
            }
        }

        private static CompanionHealthSnapshot CreateHealthSnapshot(ActorInstance actor)
        {
            return new CompanionHealthSnapshot(actor.CurrentHealth, actor.MaximumHealth);
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

        private bool HasCompanionPreCommitAction()
        {
            for (var index = 0; index < _companions.Count; index++)
            {
                if (_companions[index].CanCancelPreCommitAction)
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

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
