using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Runtime;
using DungeonTeam.Gameplay.Team.Domain;
using DungeonTeam.Gameplay.Skills.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Team.Runtime
{
    internal sealed class CompanionController : IDisposable
    {
        private const float DestinationUpdateDistance = 0.5f;

        private readonly ActorInstance _actor;
        private readonly Vector3 _formationOffset;
        private readonly TeamControlSettings _settings;
        private readonly CompanionFollowBrain _followBrain;
        private readonly CompanionCombatBrain _combatBrain;
        private readonly ActorCombatController _combat;

        private Vector3 _lastDestination;
        private bool _hasDestination;
        private bool _isMoving;
        private bool _isDisposed;

        public CompanionController(
            ActorInstance actor,
            Vector3 formationOffset,
            TeamControlSettings settings,
            ActorCombatController combat)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _formationOffset = formationOffset;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            if (!ReferenceEquals(_combat.Actor, _actor))
            {
                throw new ArgumentException(
                    "Companion combat controller must belong to the companion.",
                    nameof(combat));
            }
            _followBrain = new CompanionFollowBrain(
                settings.StartFollowingDistance,
                settings.StopFollowingDistance);
            _combatBrain = new CompanionCombatBrain(
                _combat.GetRange(SkillSlot.Primary),
                settings.CompanionTargetLossDistance);
        }

        public ActorInstance Actor => _actor;

        public void Tick(
            float deltaTime,
            ActorInstance leader,
            ActorInstance combatTarget,
            bool isForcedFollow)
        {
            if (_isDisposed)
            {
                return;
            }

            if (!_actor.IsAlive)
            {
                _combat.Tick(deltaTime);
                Stop();
                return;
            }

            if (!isForcedFollow && combatTarget != null && combatTarget.IsAlive)
            {
                UpdateCombat(deltaTime, combatTarget);
                return;
            }

            _combat.Tick(deltaTime);
            if (!leader.IsAlive)
            {
                Stop();
                return;
            }

            var followPosition = GetFollowPosition(leader);
            var state = _followBrain.Evaluate(
                Vector3.Distance(_actor.Position, followPosition));
            if (state == CompanionFollowState.Holding)
            {
                Stop();
                return;
            }

            MoveTo(followPosition);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            Stop();
        }

        private void UpdateCombat(float deltaTime, ActorInstance target)
        {
            var distance = PlanarDistance(_actor.Position, target.Position);
            var state = _combatBrain.Evaluate(
                target.IsAlive,
                HasClearLine(target),
                distance);
            _combat.Tick(deltaTime);

            switch (state)
            {
                case CompanionCombatState.Follow:
                    Stop();
                    break;
                case CompanionCombatState.Chase:
                    MoveTo(target.Position);
                    break;
                case CompanionCombatState.Attack:
                    Stop();
                    _combat.TryUse(SkillSlot.Primary, target, HasClearLine(target));

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Vector3 GetFollowPosition(ActorInstance leader)
        {
            var forward = leader.Forward;
            forward.y = 0f;
            var rotation = forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
            return leader.Position + rotation * _formationOffset;
        }

        private void MoveTo(Vector3 destination)
        {
            _combat.CancelActiveUse();
            if (_hasDestination &&
                PlanarSqrDistance(destination, _lastDestination) <
                DestinationUpdateDistance * DestinationUpdateDistance)
            {
                return;
            }

            if (_actor.TryMoveTo(destination))
            {
                _lastDestination = destination;
                _hasDestination = true;
                _isMoving = true;
            }
        }

        private void Stop()
        {
            _hasDestination = false;
            if (!_isMoving)
            {
                return;
            }

            _actor.StopMovement();
            _isMoving = false;
        }

        private bool HasClearLine(ActorInstance target)
        {
            var eyeOffset = Vector3.up * _settings.CommandEyeHeight;
            return !Physics.Linecast(
                _actor.Position + eyeOffset,
                target.Position + eyeOffset,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore);
        }

        private static float PlanarDistance(Vector3 first, Vector3 second)
        {
            return Mathf.Sqrt(PlanarSqrDistance(first, second));
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
