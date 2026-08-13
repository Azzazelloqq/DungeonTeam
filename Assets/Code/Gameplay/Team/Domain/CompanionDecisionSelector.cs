using System;

namespace DungeonTeam.Gameplay.Team.Domain
{
    public enum CompanionCommandMode
    {
        Autonomous,
        Attack,
        Follow
    }

    public enum CompanionDecisionKind
    {
        Hold,
        FollowFormation,
        Heal,
        Attack
    }

    public enum CompanionDecisionReason
    {
        ActorInactive,
        ActionInProgress,
        FollowCommand,
        AttackCommand,
        AttackTargetUnavailable,
        AutonomousHeal,
        AutonomousAttack,
        FormationFallback
    }

    public readonly struct CompanionDecisionContext
    {
        public CompanionDecisionContext(
            bool isActorActive,
            bool isActionInProgress,
            CompanionCommandMode commandMode,
            bool hasHealTarget,
            bool hasAttackTarget)
        {
            IsActorActive = isActorActive;
            IsActionInProgress = isActionInProgress;
            CommandMode = commandMode;
            HasHealTarget = hasHealTarget;
            HasAttackTarget = hasAttackTarget;
        }

        public bool IsActorActive { get; }
        public bool IsActionInProgress { get; }
        public CompanionCommandMode CommandMode { get; }
        public bool HasHealTarget { get; }
        public bool HasAttackTarget { get; }
    }

    public readonly struct CompanionDecision
    {
        public CompanionDecision(
            CompanionDecisionKind kind,
            CompanionDecisionReason reason)
        {
            Kind = kind;
            Reason = reason;
        }

        public CompanionDecisionKind Kind { get; }
        public CompanionDecisionReason Reason { get; }
    }

    public static class CompanionDecisionSelector
    {
        public static CompanionDecision Select(CompanionDecisionContext context)
        {
            if (!context.IsActorActive)
            {
                return Hold(CompanionDecisionReason.ActorInactive);
            }

            if (context.IsActionInProgress)
            {
                return Hold(CompanionDecisionReason.ActionInProgress);
            }

            switch (context.CommandMode)
            {
                case CompanionCommandMode.Follow:
                    return new CompanionDecision(
                        CompanionDecisionKind.FollowFormation,
                        CompanionDecisionReason.FollowCommand);
                case CompanionCommandMode.Attack:
                    return context.HasAttackTarget
                        ? new CompanionDecision(
                            CompanionDecisionKind.Attack,
                            CompanionDecisionReason.AttackCommand)
                        : Hold(CompanionDecisionReason.AttackTargetUnavailable);
                case CompanionCommandMode.Autonomous:
                    return SelectAutonomous(context);
                default:
                    throw new ArgumentOutOfRangeException(nameof(context));
            }
        }

        private static CompanionDecision SelectAutonomous(CompanionDecisionContext context)
        {
            if (context.HasHealTarget)
            {
                return new CompanionDecision(
                    CompanionDecisionKind.Heal,
                    CompanionDecisionReason.AutonomousHeal);
            }

            return context.HasAttackTarget
                ? new CompanionDecision(
                    CompanionDecisionKind.Attack,
                    CompanionDecisionReason.AutonomousAttack)
                : new CompanionDecision(
                    CompanionDecisionKind.FollowFormation,
                    CompanionDecisionReason.FormationFallback);
        }

        private static CompanionDecision Hold(CompanionDecisionReason reason)
        {
            return new CompanionDecision(CompanionDecisionKind.Hold, reason);
        }
    }
}
