using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Combat.Domain;
using UnityEngine;

namespace DungeonTeam.Gameplay.Combat.Runtime
{
    public enum AttackExecutionResult
    {
        Executed,
        OnCooldown,
        OutOfRange,
        Blocked,
        InvalidTarget
    }

    public sealed class ActorCombatController
    {
        private readonly ActorInstance _actor;
        private readonly AttackRankDefinition _primaryAttack;
        private readonly AttackCooldown _cooldown;

        public ActorCombatController(
            ActorInstance actor,
            AttackRankDefinition primaryAttack)
        {
            _actor = actor ?? throw new ArgumentNullException(nameof(actor));
            _primaryAttack = primaryAttack;
            _cooldown = new AttackCooldown(primaryAttack.Cooldown);
        }

        public ActorInstance Actor => _actor;
        public float PrimaryAttackRange => _primaryAttack.Range;
        public int PrimaryAttackDamage => _primaryAttack.Damage;
        public bool IsPrimaryAttackReady => _cooldown.IsReady;

        public void Tick(float deltaTime)
        {
            _cooldown.Tick(deltaTime);
        }

        public AttackExecutionResult TryUsePrimaryAttack(
            ActorInstance target,
            bool hasClearLine)
        {
            if (!_actor.IsAlive || target == null || !target.IsAlive)
            {
                return AttackExecutionResult.InvalidTarget;
            }

            if (PlanarSqrDistance(_actor.Position, target.Position) >
                _primaryAttack.Range * _primaryAttack.Range)
            {
                return AttackExecutionResult.OutOfRange;
            }

            if (!hasClearLine)
            {
                return AttackExecutionResult.Blocked;
            }

            if (!_cooldown.TryConsume())
            {
                return AttackExecutionResult.OnCooldown;
            }

            _actor.TryFaceTowards(target.Position);
            _actor.PlayAttackFeedback();
            target.ApplyDamage(_primaryAttack.Damage, _actor);
            return AttackExecutionResult.Executed;
        }

        private static float PlanarSqrDistance(Vector3 first, Vector3 second)
        {
            var difference = first - second;
            difference.y = 0f;
            return difference.sqrMagnitude;
        }
    }
}
