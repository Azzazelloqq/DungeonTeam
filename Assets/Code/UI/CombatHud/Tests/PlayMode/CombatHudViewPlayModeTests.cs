using System.Collections;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Skills.Domain;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonTeam.UI.CombatHud.Tests.PlayMode
{
    public sealed class CombatHudViewPlayModeTests
    {
        [UnityTest]
        public IEnumerator FeedbackStates_ShowDistinctStatusAndEnableOnlyReady()
        {
            var harness = CreateHarness();
            try
            {
                yield return null;
                var button = FindSkillButton(harness.View, SkillSlot.Active1);
                var status = button.transform.Find("Status").GetComponent<TMP_Text>();

                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.Ready,
                    expectedStatus: null,
                    isInteractable: true);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.PendingApproach,
                    expectedStatus: "APPROACH",
                    isInteractable: false);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.Busy,
                    expectedStatus: "BUSY",
                    isInteractable: false);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.NoTargetOrInvalidTarget,
                    expectedStatus: "NO TARGET",
                    isInteractable: false);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.Cooldown,
                    expectedStatus: "2.0",
                    isInteractable: false);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.Casting,
                    expectedStatus: "CAST",
                    isInteractable: false);
                AssertFeedback(
                    harness.Model,
                    button,
                    status,
                    CombatHudSlotFeedback.Recovery,
                    expectedStatus: "RECOVERY",
                    isInteractable: false);
            }
            finally
            {
                harness.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator JoystickHeld_WhenSecondPointerClicksSkill_PreservesMovementAndRequestsOnce()
        {
            var harness = CreateHarness();
            try
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                var joystick = harness.View.GetComponentInChildren<VirtualJoystickControl>();
                var joystickRect = (RectTransform)joystick.transform;
                var joystickPointer = new PointerEventData(harness.EventSystem)
                {
                    pointerId = 1,
                    position = RectTransformUtility.WorldToScreenPoint(null, joystickRect.position) +
                               Vector2.right * 30f
                };

                joystick.OnPointerDown(joystickPointer);
                var heldMovement = harness.MovementCommands[^1];
                Assert.That(heldMovement.sqrMagnitude, Is.GreaterThan(0f));

                var skillButton = FindSkillButton(harness.View, SkillSlot.Active1);
                var skillPointer = new PointerEventData(harness.EventSystem)
                {
                    pointerId = 2,
                    button = PointerEventData.InputButton.Left
                };
                ExecuteEvents.Execute(
                    skillButton.gameObject,
                    skillPointer,
                    ExecuteEvents.pointerClickHandler);

                Assert.That(harness.RequestedSlots, Is.EqualTo(new[] { SkillSlot.Active1 }));
                Assert.That(harness.MovementCommands[^1], Is.EqualTo(heldMovement));

                joystick.OnPointerUp(joystickPointer);
                Assert.That(harness.MovementCommands[^1], Is.EqualTo(Vector2.zero));
            }
            finally
            {
                harness.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator TargetState_UpdatesMarkerStyleAndVisibility()
        {
            var harness = CreateHarness();
            try
            {
                yield return null;
                var marker = (RectTransform)harness.View.transform.Find(
                    "SafeArea/TargetMarker");
                var topSegment = marker.Find("Top").GetComponent<Image>();
                var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                Assert.That(marker.gameObject.activeSelf, Is.False);

                harness.Model.UpdateTarget(new CombatHudTargetState(
                    new Vector3(screenCenter.x, screenCenter.y, 1f),
                    CombatHudTargetSelection.Manual));
                Assert.That(marker.gameObject.activeSelf, Is.True);
                var manualColor = topSegment.color;

                harness.Model.UpdateTarget(new CombatHudTargetState(
                    new Vector3(screenCenter.x, screenCenter.y, 1f),
                    CombatHudTargetSelection.Automatic));
                Assert.That(marker.gameObject.activeSelf, Is.True);
                Assert.That(topSegment.color.a, Is.LessThan(manualColor.a));

                harness.Model.UpdateTarget(new CombatHudTargetState(
                    new Vector3(-1f, screenCenter.y, 1f),
                    CombatHudTargetSelection.Manual));
                Assert.That(marker.gameObject.activeSelf, Is.False);

                harness.Model.UpdateTarget(CombatHudTargetState.Hidden);
                Assert.That(marker.gameObject.activeSelf, Is.False);
            }
            finally
            {
                harness.Dispose();
            }
        }

        [UnityTest]
        public IEnumerator ContextActionsHost_IsPassiveAndStretchesAcrossSafeArea()
        {
            var harness = CreateHarness();
            try
            {
                yield return null;
                var host = harness.View.ContextActionsHost;

                Assert.That(host, Is.Not.Null);
                Assert.That(host.name, Is.EqualTo("ContextActionsHost"));
                Assert.That(host.parent.name, Is.EqualTo("SafeArea"));
                Assert.That(host.anchorMin, Is.EqualTo(Vector2.zero));
                Assert.That(host.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(host.GetComponents<Component>(), Has.Length.EqualTo(1));
            }
            finally
            {
                harness.Dispose();
            }
        }

        private static void AssertFeedback(
            CombatHudModel model,
            Button button,
            TMP_Text status,
            CombatHudSlotFeedback feedback,
            string expectedStatus,
            bool isInteractable)
        {
            model.UpdateSlot(State(SkillSlot.Active1, feedback));

            Assert.That(button.interactable, Is.EqualTo(isInteractable));
            Assert.That(status.gameObject.activeSelf, Is.EqualTo(expectedStatus != null));
            if (expectedStatus != null)
                Assert.That(status.text, Is.EqualTo(expectedStatus));
        }

        private static Button FindSkillButton(CombatHudView view, SkillSlot slot)
        {
            return view.transform
                .Find($"SafeArea/Skill_{slot}")
                .GetComponent<Button>();
        }

        private static Harness CreateHarness()
        {
            var createdEventSystem = EventSystem.current == null
                ? new GameObject("CombatHudTestEventSystem", typeof(EventSystem))
                : null;
            var eventSystem = EventSystem.current ?? createdEventSystem.GetComponent<EventSystem>();
            var canvasObject = new GameObject(
                "CombatHudTestCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var hudObject = new GameObject(
                "CombatHudTestView",
                typeof(RectTransform),
                typeof(CombatHudView));
            hudObject.transform.SetParent(canvasObject.transform, false);

            var movementCommands = new List<Vector2>();
            var requestedSlots = new List<SkillSlot>();
            var model = new CombatHudModel(new[]
            {
                State(SkillSlot.Primary, CombatHudSlotFeedback.Ready),
                State(SkillSlot.Active1, CombatHudSlotFeedback.Ready)
            });
            var viewModel = new CombatHudViewModel(
                model,
                movementCommands.Add,
                requestedSlots.Add);
            viewModel.Initialize();

            var view = hudObject.GetComponent<CombatHudView>();
            view.Initialize(viewModel);
            return new Harness(
                createdEventSystem,
                canvasObject,
                eventSystem,
                model,
                viewModel,
                view,
                movementCommands,
                requestedSlots);
        }

        private static CombatHudSlotState State(SkillSlot slot, CombatHudSlotFeedback feedback)
        {
            var isPending = feedback == CombatHudSlotFeedback.PendingApproach;
            SkillUsePhase? activePhase = feedback == CombatHudSlotFeedback.Casting
                ? SkillUsePhase.Preparing
                : feedback == CombatHudSlotFeedback.Recovery
                    ? SkillUsePhase.Recovering
                    : null;
            return new CombatHudSlotState(
                slot,
                slot.ToString(),
                null,
                4f,
                feedback == CombatHudSlotFeedback.Cooldown ? 2f : 0f,
                isReady: feedback == CombatHudSlotFeedback.Ready,
                isSelected: false,
                isPending: isPending,
                activePhase: activePhase,
                isActorBusy: feedback == CombatHudSlotFeedback.Busy,
                feedback: feedback);
        }

        private sealed class Harness
        {
            private readonly GameObject _createdEventSystem;
            private readonly GameObject _canvasObject;

            public Harness(
                GameObject createdEventSystem,
                GameObject canvasObject,
                EventSystem eventSystem,
                CombatHudModel model,
                CombatHudViewModel viewModel,
                CombatHudView view,
                List<Vector2> movementCommands,
                List<SkillSlot> requestedSlots)
            {
                _createdEventSystem = createdEventSystem;
                _canvasObject = canvasObject;
                EventSystem = eventSystem;
                Model = model;
                ViewModel = viewModel;
                View = view;
                MovementCommands = movementCommands;
                RequestedSlots = requestedSlots;
            }

            public EventSystem EventSystem { get; }
            public CombatHudModel Model { get; }
            public CombatHudViewModel ViewModel { get; }
            public CombatHudView View { get; }
            public List<Vector2> MovementCommands { get; }
            public List<SkillSlot> RequestedSlots { get; }

            public void Dispose()
            {
                ViewModel.Dispose();
                Model.Dispose();
                Object.Destroy(_canvasObject);
                Object.Destroy(_createdEventSystem);
            }
        }
    }
}
