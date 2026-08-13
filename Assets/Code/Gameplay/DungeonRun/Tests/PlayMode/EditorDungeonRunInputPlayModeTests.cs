#if UNITY_EDITOR
using System;
using System.Collections;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.DungeonRun.Tests.PlayMode
{
    public sealed class EditorDungeonRunInputPlayModeTests : InputTestFixture
    {
        private EditorDungeonRunInput _input;
        private GameObject _eventSystemObject;
        private GameObject _canvasObject;

        public override void Setup()
        {
            base.Setup();
            _input = new EditorDungeonRunInput();
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
        public void WasdMovement_EmitsExpectedDirection()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.wKey);

            Assert.That(_input.Movement, Is.EqualTo(Vector2.up));
            Release(keyboard.wKey);
            Assert.That(_input.Movement, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void SkillBindings_AreNotProvided()
        {
            InputSystem.AddDevice<Keyboard>();

            Assert.That(_input.TryConsumeSkillRequest(out _), Is.False);
        }

        [Test]
        public void LeftMousePress_CapturesTargetSelectionExactlyOnce()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            var expectedPosition = new Vector2(320f, 180f);
            Move(mouse.position, expectedPosition);

            Press(mouse.leftButton);

            Assert.That(_input.TryConsumeTargetSelection(out var captured), Is.True);
            Assert.That(captured, Is.EqualTo(expectedPosition));
            Assert.That(_input.TryConsumeTargetSelection(out _), Is.False);
            Release(mouse.leftButton);
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
        }

        private RectTransform CreateFullScreenUiBlocker()
        {
            if (EventSystem.current == null)
            {
                _eventSystemObject = new GameObject(
                    "EditorInputTestEventSystem",
                    typeof(EventSystem));
            }

            _canvasObject = new GameObject(
                "EditorInputTestCanvas",
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
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
#endif
