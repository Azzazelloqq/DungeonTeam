using System;
using TMPro;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    public sealed class ActorCombatFeedback : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _damageText;

        [SerializeField, Min(0.01f)]
        private float _damageNumberDuration = 0.7f;

        [SerializeField, Min(0f)]
        private float _damageNumberRiseDistance = 0.8f;

        [SerializeField]
        private Color _damageNumberColor = new(1f, 0.2f, 0.1f, 1f);

        private Transform _cameraTransform;
        private Vector3 _damageTextStartPosition;
        private float _numberElapsed;
        private bool _isNumberVisible;
        private bool _isConfigured;

        public void Configure()
        {
            _damageText ??= CreateDefaultDamageText();
            ValidateBindings();

            _damageTextStartPosition = _damageText.transform.localPosition;
            _damageText.gameObject.SetActive(false);
            _isNumberVisible = false;
            _numberElapsed = 0f;
            _isConfigured = true;
            enabled = false;
        }

        public void PlayDamage(int amount)
        {
            RequireConfigured();
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _damageText.SetText("-{0}", amount);
            _damageText.color = _damageNumberColor;
            _damageText.transform.localPosition = _damageTextStartPosition;
            _damageText.gameObject.SetActive(true);
            var worldCamera = Camera.main;
            _cameraTransform = worldCamera != null ? worldCamera.transform : null;
            _numberElapsed = 0f;
            _isNumberVisible = true;
            enabled = true;
        }

        private void Update()
        {
            if (!_isConfigured || !_isNumberVisible)
            {
                enabled = false;
                return;
            }

            _numberElapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(_numberElapsed / _damageNumberDuration);
            _damageText.transform.localPosition = _damageTextStartPosition +
                                                  Vector3.up *
                                                  (_damageNumberRiseDistance * progress);
            if (_cameraTransform != null)
            {
                _damageText.transform.rotation = _cameraTransform.rotation;
            }

            var textColor = _damageNumberColor;
            textColor.a *= 1f - progress;
            _damageText.color = textColor;
            if (progress < 1f)
            {
                return;
            }

            _damageText.gameObject.SetActive(false);
            _isNumberVisible = false;
            enabled = false;
        }

        private void ValidateBindings()
        {
            if (_damageText == null)
            {
                throw new InvalidOperationException(
                    "Actor Combat Feedback requires a damage text binding.");
            }

            if (_damageNumberDuration <= 0f || _damageNumberRiseDistance < 0f)
            {
                throw new InvalidOperationException(
                    "Damage number duration must be positive and rise distance cannot be negative.");
            }
        }

        private void RequireConfigured()
        {
            if (!_isConfigured)
            {
                throw new InvalidOperationException(
                    "Actor Combat Feedback must be configured before playback.");
            }
        }

        private TMP_Text CreateDefaultDamageText()
        {
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                throw new InvalidOperationException(
                    "TextMeshPro default font asset is required for damage numbers.");
            }

            var textObject = new GameObject("DamageText", typeof(RectTransform));
            textObject.layer = gameObject.layer;
            textObject.transform.SetParent(transform, false);

            var damageText = textObject.AddComponent<TextMeshPro>();
            damageText.font = defaultFont;
            damageText.text = string.Empty;
            damageText.fontSize = 4f;
            damageText.fontStyle = FontStyles.Bold;
            damageText.alignment = TextAlignmentOptions.Center;
            damageText.textWrappingMode = TextWrappingModes.NoWrap;
            damageText.color = _damageNumberColor;
            damageText.renderer.sortingOrder = 100;

            var rectTransform = damageText.rectTransform;
            rectTransform.localPosition = new Vector3(0f, 2.4f, 0f);
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one * 0.25f;
            rectTransform.sizeDelta = new Vector2(8f, 2f);
            return damageText;
        }
    }
}
