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
        public void UpdateTarget_PublishesSelectionThroughViewModelContract()
        {
            var model = new CombatHudModel(new[] { State(SkillSlot.Primary, "Primary") });
            var viewModel = new CombatHudViewModel(model, _ => { }, _ => { });
            viewModel.Initialize();
            var target = new CombatHudTargetState(
                new Vector3(640f, 360f, 4f),
                CombatHudTargetSelection.Manual);

            model.UpdateTarget(target);

            Assert.That(viewModel.Target.Value, Is.EqualTo(target));
            viewModel.Dispose();
        }

        [Test]
        public void TargetState_WithNonFinitePosition_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new CombatHudTargetState(
                    new Vector3(float.NaN, 360f, 4f),
                    CombatHudTargetSelection.Automatic));
        }

        [Test]
        public void TargetState_BehindCamera_RemainsAvailableForEdgeLayout()
        {
            var target = new CombatHudTargetState(
                new Vector3(640f, 360f, -1f),
                CombatHudTargetSelection.Manual);

            Assert.That(target.HasTarget, Is.True);
            Assert.That(target.IsInFront, Is.False);
        }

        [Test]
        public void TargetMarkerLayout_OffscreenRight_ClampsInsideSafeArea()
        {
            var layout = CombatHudTargetMarkerLayout.Resolve(
                new Rect(-100f, -50f, 200f, 100f),
                new Vector2(160f, 0f),
                isInFront: true,
                markerHalfSize: 10f);

            Assert.That(layout.IsVisible, Is.True);
            Assert.That(layout.IsOffscreen, Is.True);
            Assert.That(layout.Position, Is.EqualTo(new Vector2(90f, 0f)));
            Assert.That(layout.Direction, Is.EqualTo(Vector2.right));
        }

        [Test]
        public void TargetMarkerLayout_BehindCamera_FlipsDirectionBeforeClamping()
        {
            var layout = CombatHudTargetMarkerLayout.Resolve(
                new Rect(-100f, -50f, 200f, 100f),
                new Vector2(20f, 0f),
                isInFront: false,
                markerHalfSize: 10f);

            Assert.That(layout.IsVisible, Is.True);
            Assert.That(layout.IsOffscreen, Is.True);
            Assert.That(layout.Position, Is.EqualTo(new Vector2(-90f, 0f)));
            Assert.That(layout.Direction, Is.EqualTo(Vector2.left));
        }

        [Test]
        public void TargetMarkerLayout_Onscreen_PreservesProjectedPosition()
        {
            var expectedPosition = new Vector2(25f, -12f);

            var layout = CombatHudTargetMarkerLayout.Resolve(
                new Rect(-100f, -50f, 200f, 100f),
                expectedPosition,
                isInFront: true,
                markerHalfSize: 10f);

            Assert.That(layout.IsVisible, Is.True);
            Assert.That(layout.IsOffscreen, Is.False);
            Assert.That(layout.Position, Is.EqualTo(expectedPosition));
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
