using DungeonTeam.Gameplay.Skills.Domain;
using NUnit.Framework;
using UnityEngine;

namespace DungeonTeam.UI.CombatHud.Tests
{
    public sealed class CombatHudTests
    {
        [Test]
        public void UpdateSlot_ChangesOnlyRequestedSlotState()
        {
            var model = new CombatHudModel(new[]
            {
                State(SkillSlot.Primary, "Primary"),
                State(SkillSlot.Active1, "Active")
            });
            var viewModel = new CombatHudViewModel(model, _ => { }, _ => { });
            viewModel.Initialize();

            model.UpdateSlot(new CombatHudSlotState(
                SkillSlot.Active1,
                "Active",
                null,
                4f,
                2f,
                isReady: false,
                isSelected: true,
                isPending: true,
                activePhase: SkillUsePhase.Preparing,
                isActorBusy: false,
                feedback: CombatHudSlotFeedback.Casting));

            Assert.That(model.Slots[0].Value.CooldownRemaining, Is.Zero);
            Assert.That(model.Slots[1].Value.CooldownRemaining, Is.EqualTo(2f));
            Assert.That(model.Slots[1].Value.IsPending, Is.True);
            Assert.That(
                model.Slots[1].Value.ActivePhase,
                Is.EqualTo(SkillUsePhase.Preparing));
            viewModel.Dispose();
        }

        [Test]
        public void RequestSkillCommand_ForwardsExactSlot()
        {
            SkillSlot? requested = null;
            var model = new CombatHudModel(new[] { State(SkillSlot.Active1, "Active") });
            var viewModel = new CombatHudViewModel(
                model,
                _ => { },
                slot => requested = slot);
            viewModel.Initialize();

            viewModel.RequestSkillCommand.Execute(SkillSlot.Active1);

            Assert.That(requested, Is.EqualTo(SkillSlot.Active1));
            viewModel.Dispose();
        }

        [Test]
        public void SetMovementCommand_ForwardsExactVector()
        {
            var requested = Vector2.zero;
            var model = new CombatHudModel(new[] { State(SkillSlot.Primary, "Primary") });
            var viewModel = new CombatHudViewModel(
                model,
                movement => requested = movement,
                _ => { });
            viewModel.Initialize();

            viewModel.SetMovementCommand.Execute(new Vector2(-0.4f, 0.75f));

            Assert.That(requested, Is.EqualTo(new Vector2(-0.4f, 0.75f)));
            viewModel.Dispose();
        }

        [Test]
        public void SetControlsEnabled_PublishesThroughViewModelContract()
        {
            var model = new CombatHudModel(new[] { State(SkillSlot.Primary, "Primary") });
            var viewModel = new CombatHudViewModel(model, _ => { }, _ => { });
            viewModel.Initialize();

            model.SetControlsEnabled(false);

            Assert.That(viewModel.ControlsEnabled.Value, Is.False);
            viewModel.Dispose();
        }

        [Test]
        public void Create_WithDuplicateSlot_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => new CombatHudModel(new[]
            {
                State(SkillSlot.Primary, "One"),
                State(SkillSlot.Primary, "Two")
            }));
        }

        [Test]
        public void FeedbackResolver_ReportsDistinctRuntimeFeedback()
        {
            Assert.That(Resolve(isPending: true), Is.EqualTo(CombatHudSlotFeedback.PendingApproach));
            Assert.That(Resolve(isActorBusy: true), Is.EqualTo(CombatHudSlotFeedback.Busy));
            Assert.That(
                Resolve(canRequestSkill: false),
                Is.EqualTo(CombatHudSlotFeedback.NoTargetOrInvalidTarget));
            Assert.That(Resolve(cooldownRemaining: 2f), Is.EqualTo(CombatHudSlotFeedback.Cooldown));
            Assert.That(
                Resolve(activePhase: SkillUsePhase.Preparing),
                Is.EqualTo(CombatHudSlotFeedback.Casting));
            Assert.That(
                Resolve(activePhase: SkillUsePhase.Recovering),
                Is.EqualTo(CombatHudSlotFeedback.Recovery));
        }

        [Test]
        public void FeedbackResolver_RecognizesOutOfRangeAutoTargetAsReadyBeforeApproachBegins()
        {
            Assert.That(Resolve(canRequestSkill: true), Is.EqualTo(CombatHudSlotFeedback.Ready));
            Assert.That(
                Resolve(canRequestSkill: true, isPending: true),
                Is.EqualTo(CombatHudSlotFeedback.PendingApproach));
        }

        private static CombatHudSlotState State(SkillSlot slot, string title)
        {
            return new CombatHudSlotState(
                slot,
                title,
                null,
                4f,
                0f,
                isReady: true,
                isSelected: false,
                isPending: false,
                activePhase: null,
                isActorBusy: false,
                feedback: CombatHudSlotFeedback.Ready);
        }

        private static CombatHudSlotFeedback Resolve(
            bool canRequestSkill = true,
            bool isPending = false,
            SkillUsePhase? activePhase = null,
            float cooldownRemaining = 0f,
            bool isActorBusy = false)
        {
            return CombatHudSlotFeedbackResolver.Resolve(
                true,
                canRequestSkill,
                isPending,
                activePhase,
                cooldownRemaining,
                isActorBusy);
        }
    }
}
