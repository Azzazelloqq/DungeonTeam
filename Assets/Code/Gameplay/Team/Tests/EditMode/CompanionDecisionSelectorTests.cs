using DungeonTeam.Gameplay.Team.Domain;
using NUnit.Framework;

namespace DungeonTeam.Gameplay.Team.Tests
{
    public sealed class CompanionDecisionSelectorTests
    {
        [Test]
        public void Select_FollowCommandWithAvailableDuties_RecallsToFormation()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Follow,
                hasHealTarget: true,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.FollowFormation));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.FollowCommand));
        }

        [Test]
        public void Select_ActionInProgressDuringFollow_HoldsUntilActionCompletes()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: true,
                commandMode: CompanionCommandMode.Follow,
                hasHealTarget: true,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Hold));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.ActionInProgress));
        }

        [Test]
        public void Select_AttackCommandWithTarget_EngagesOrderedTarget()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Attack,
                hasHealTarget: true,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Attack));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.AttackCommand));
        }

        [Test]
        public void Select_AutonomousWithHealAndAttackTargets_PrioritizesHeal()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Autonomous,
                hasHealTarget: true,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Heal));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.AutonomousHeal));
        }

        [Test]
        public void Select_AutonomousWithAttackTarget_EngagesRetaliationTarget()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Autonomous,
                hasHealTarget: false,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Attack));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.AutonomousAttack));
        }

        [Test]
        public void Select_AutonomousWithoutDuty_FollowsFormation()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Autonomous,
                hasHealTarget: false,
                hasAttackTarget: false));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.FollowFormation));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.FormationFallback));
        }

        [Test]
        public void Select_AttackCommandWithoutTarget_HoldsPosition()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: true,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Attack,
                hasHealTarget: true,
                hasAttackTarget: false));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Hold));
            Assert.That(
                decision.Reason,
                Is.EqualTo(CompanionDecisionReason.AttackTargetUnavailable));
        }

        [Test]
        public void Select_InactiveActor_HoldsBeforeAnyCommandOrDuty()
        {
            var decision = CompanionDecisionSelector.Select(new CompanionDecisionContext(
                isActorActive: false,
                isActionInProgress: false,
                commandMode: CompanionCommandMode.Follow,
                hasHealTarget: true,
                hasAttackTarget: true));

            Assert.That(decision.Kind, Is.EqualTo(CompanionDecisionKind.Hold));
            Assert.That(decision.Reason, Is.EqualTo(CompanionDecisionReason.ActorInactive));
        }
    }
}
