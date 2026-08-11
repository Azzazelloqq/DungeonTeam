using System;
using System.Collections;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.PlayMode
{
    public sealed class DesktopDungeonRunInputPlayModeTests : InputTestFixture
    {
        private DesktopDungeonRunInput _input;
        private GameObject _eventSystemObject;
        private GameObject _canvasObject;

        public override void Setup()
        {
            base.Setup();
            _input = new DesktopDungeonRunInput();
            _input.Enable();
        }

        public override void TearDown()
        {
            _input?.Dispose();
            _input = null;
            DestroyImmediate(_canvasObject);
            DestroyImmediate(_eventSystemObject);
            _canvasObject = null;
            _eventSystemObject = null;
            base.TearDown();
        }

        [Test]
        public void KeyboardSkillBindings_EmitExpectedSlotsExactlyOnce()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            PressAndRelease(keyboard.spaceKey);

            Assert.That(_input.TryConsumeSkillRequest(out var primary), Is.True);
            Assert.That(primary, Is.EqualTo(SkillSlot.Primary));
            Assert.That(_input.TryConsumeSkillRequest(out _), Is.False);

            PressAndRelease(keyboard.qKey);

            Assert.That(_input.TryConsumeSkillRequest(out var active), Is.True);
            Assert.That(active, Is.EqualTo(SkillSlot.Active1));
            Assert.That(_input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void SecondTouch_WhileFirstIsHeld_CapturesSecondTouchPosition()
        {
            var touchscreen = InputSystem.AddDevice<Touchscreen>();
            var firstPosition = new Vector2(120f, 180f);
            var secondPosition = new Vector2(760f, 420f);

            BeginTouch(1, firstPosition, screen: touchscreen);
            Assert.That(
                _input.TryConsumeTargetSelection(out var capturedFirst),
                Is.True);
            Assert.That(capturedFirst, Is.EqualTo(firstPosition));

            BeginTouch(2, secondPosition, screen: touchscreen);

            Assert.That(
                _input.TryConsumeTargetSelection(out var capturedSecond),
                Is.True);
            Assert.That(capturedSecond, Is.EqualTo(secondPosition));
            Assert.That(_input.TryConsumeTargetSelection(out _), Is.False);

            EndTouch(2, secondPosition, screen: touchscreen);
            EndTouch(1, firstPosition, screen: touchscreen);
        }

        [UnityTest]
        public IEnumerator PointerPress_OverGraphicRaycaster_IsNotWorldTargetCommand()
        {
            var blocker = CreateFullScreenUiBlocker();
            yield return null;
            Canvas.ForceUpdateCanvases();

            var mouse = InputSystem.AddDevice<Mouse>();
            var blockerCenter = RectTransformUtility.WorldToScreenPoint(
                null,
                blocker.TransformPoint(blocker.rect.center));
            Move(mouse.position, blockerCenter);

            Press(mouse.leftButton);

            Assert.That(_input.TryConsumeTargetSelection(out _), Is.False);
            Release(mouse.leftButton);
        }

        [Test]
        public void Dispose_IsIdempotent_AndEnableAfterDisposeThrows()
        {
            _input.Dispose();
            _input.Dispose();

            Assert.Throws<ObjectDisposedException>(() => _input.Enable());
            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
            Assert.That(_input.CameraYawDelta, Is.Zero);
        }

        private RectTransform CreateFullScreenUiBlocker()
        {
            if (EventSystem.current == null)
            {
                _eventSystemObject = new GameObject(
                    "DesktopInputTestEventSystem",
                    typeof(EventSystem));
            }
            _canvasObject = new GameObject(
                "DesktopInputTestCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(GraphicRaycaster));
            _canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var blocker = new GameObject(
                "Blocker",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            var blockerRect = (RectTransform)blocker.transform;
            blockerRect.SetParent(_canvasObject.transform, false);
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;
            blocker.GetComponent<Image>().raycastTarget = true;
            Canvas.ForceUpdateCanvases();
            return blockerRect;
        }

        private static void DestroyImmediate(GameObject target)
        {
            if (target != null)
                UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
