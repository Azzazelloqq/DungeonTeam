using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace DungeonTeam.UI.CombatHud.Tests.PlayMode
{
    public sealed class VirtualJoystickControlPlayModeTests
    {
        [UnityTest]
        public IEnumerator DragReleaseAndDisable_PublishExpectedMovementAndResetKnob()
        {
            GameObject eventSystemObject = null;
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystemObject = new GameObject(
                    "JoystickTestEventSystem",
                    typeof(EventSystem));
                eventSystem = eventSystemObject.GetComponent<EventSystem>();
            }
            var canvasObject = new GameObject(
                "JoystickTestCanvas",
                typeof(RectTransform),
                typeof(Canvas));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var joystickObject = new GameObject(
                "Joystick",
                typeof(RectTransform),
                typeof(VirtualJoystickControl));
            var joystickRect = (RectTransform)joystickObject.transform;
            joystickRect.SetParent(canvasObject.transform, false);
            joystickRect.anchorMin = new Vector2(0.5f, 0.5f);
            joystickRect.anchorMax = new Vector2(0.5f, 0.5f);
            joystickRect.sizeDelta = Vector2.one * 200f;

            var knobObject = new GameObject("Knob", typeof(RectTransform));
            var knobRect = (RectTransform)knobObject.transform;
            knobRect.SetParent(joystickRect, false);
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = Vector2.one * 80f;

            var published = new List<Vector2>();
            var control = joystickObject.GetComponent<VirtualJoystickControl>();
            control.Bind(knobRect, 50f, published.Add);
            Canvas.ForceUpdateCanvases();

            var center = RectTransformUtility.WorldToScreenPoint(null, joystickRect.position);
            var pointer = new PointerEventData(eventSystem)
            {
                pointerId = 7,
                position = center + Vector2.right * 25f
            };

            try
            {
                control.OnPointerDown(pointer);
                Assert.That(published[^1].x, Is.EqualTo(0.5f).Within(0.01f));
                Assert.That(published[^1].y, Is.Zero.Within(0.01f));

                pointer.position = center + Vector2.up * 100f;
                control.OnDrag(pointer);
                Assert.That(published[^1].x, Is.Zero.Within(0.01f));
                Assert.That(published[^1].y, Is.EqualTo(1f).Within(0.01f));

                control.OnPointerUp(pointer);
                Assert.That(published[^1], Is.EqualTo(Vector2.zero));
                Assert.That(knobRect.anchoredPosition, Is.EqualTo(Vector2.zero));

                pointer.position = center + Vector2.left * 25f;
                control.OnPointerDown(pointer);
                control.enabled = false;
                Assert.That(published[^1], Is.EqualTo(Vector2.zero));
                Assert.That(knobRect.anchoredPosition, Is.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.Destroy(canvasObject);
                Object.Destroy(eventSystemObject);
            }

            yield return null;
        }
    }
}
