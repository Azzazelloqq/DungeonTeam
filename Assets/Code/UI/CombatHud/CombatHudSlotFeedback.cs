using DungeonTeam.Gameplay.Skills.Domain;

namespace DungeonTeam.UI.CombatHud
{
    public enum CombatHudSlotFeedback
    {
        Disabled,
        Ready,
        PendingApproach,
        Busy,
        NoTargetOrInvalidTarget,
        Cooldown,
        Casting,
        Recovery
    }

    public static class CombatHudSlotFeedbackResolver
    {
        public static CombatHudSlotFeedback Resolve(
            bool controlsEnabled,
            bool canRequestSkill,
            bool isPending,
            SkillUsePhase? activePhase,
            float cooldownRemaining,
            bool isActorBusy)
        {
            if (!controlsEnabled)
                return CombatHudSlotFeedback.Disabled;
            if (activePhase == SkillUsePhase.Preparing)
                return CombatHudSlotFeedback.Casting;
            if (activePhase == SkillUsePhase.Recovering)
                return CombatHudSlotFeedback.Recovery;
            if (isPending)
                return CombatHudSlotFeedback.PendingApproach;
            if (cooldownRemaining > 0f)
                return CombatHudSlotFeedback.Cooldown;
            if (isActorBusy)
                return CombatHudSlotFeedback.Busy;

            return canRequestSkill
                ? CombatHudSlotFeedback.Ready
                : CombatHudSlotFeedback.NoTargetOrInvalidTarget;
        }
    }
}
