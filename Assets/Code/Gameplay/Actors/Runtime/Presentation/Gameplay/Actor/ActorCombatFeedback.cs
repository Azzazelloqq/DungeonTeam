using System;
using TMPro;
using UnityEngine;

namespace DungeonTeam.Gameplay.Actors.Runtime.Presentation.Gameplay.Actor
{
    public sealed class ActorCombatFeedback : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        private TMP_Text _damageText;

        [SerializeField, Min(0.01f)]
        private float _attackFlashDuration = 0.12f;

        [SerializeField, Min(0.01f)]
        private float _damageFlashDuration = 0.15f;

        [SerializeField, Min(0.01f)]
        private float _damageNumberDuration = 0.7f;

        [SerializeField, Min(0f)]
        private float _damageNumberRiseDistance = 0.8f;

        [SerializeField]
        private Color _attackFlashColor = new(1f, 0.8f, 0.2f, 1f);

        [SerializeField]
        private Color _damageFlashColor = new(1f, 0.15f, 0.1f, 1f);

        [SerializeField]
        private Color _damageNumberColor = new(1f, 0.2f, 0.1f, 1f);

        [SerializeField]
        private Color _targetHighlightColor = new(1f, 0.75f, 0.1f, 1f);

        private Renderer[] _colorRenderers;
        private MaterialPropertyBlock _propertyBlock;
        private Transform _cameraTransform;
        private Vector3 _damageTextStartPosition;
        private Color _restingColor;
        private float _flashRemaining;
        private float _numberElapsed;
        private bool _isNumberVisible;
        private bool _isTargetHighlighted;
        private bool _isConfigured;

        public void Configure(Renderer[] colorRenderers, Color restingColor)
        {
            _damageText ??= CreateDefaultDamageText();
            ValidateBindings(colorRenderers);

            _colorRenderers = colorRenderers;
            _restingColor = restingColor;
            _propertyBlock ??= new MaterialPropertyBlock();
            _damageTextStartPosition = _damageText.transform.localPosition;
            _damageText.gameObject.SetActive(false);
            _isNumberVisible = false;
            _flashRemaining = 0f;
            _numberElapsed = 0f;
            _isTargetHighlighted = false;
            _isConfigured = true;
            ApplyRestingColor();
            enabled = false;
        }

        public void PlayAttack()
        {
            RequireConfigured();
            StartFlash(_attackFlashColor, _attackFlashDuration);
        }

        public void PlayDamage(int amount)
        {
            RequireConfigured();
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            StartFlash(_damageFlashColor, _damageFlashDuration);
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

        public void SetTargetHighlighted(bool isHighlighted)
        {
            RequireConfigured();
            _isTargetHighlighted = isHighlighted;
            if (_flashRemaining <= 0f)
            {
                ApplyRestingColor();
            }
        }

        public void PlayDeath(Color deadColor)
        {
            RequireConfigured();
            _isTargetHighlighted = false;
            _restingColor = deadColor;
            if (_flashRemaining <= 0f)
            {
                ApplyRestingColor();
            }

            enabled = _flashRemaining > 0f || _isNumberVisible;
        }

        private void Update()
        {
            if (!_isConfigured)
            {
                return;
            }

            UpdateFlash(Time.deltaTime);
            UpdateDamageNumber(Time.deltaTime);
            if (_flashRemaining <= 0f && !_isNumberVisible)
            {
                enabled = false;
            }
        }

        private void UpdateFlash(float deltaTime)
        {
            if (_flashRemaining <= 0f)
            {
                return;
            }

            _flashRemaining = Mathf.Max(0f, _flashRemaining - deltaTime);
            if (_flashRemaining <= 0f)
            {
                ApplyRestingColor();
            }
        }

        private void UpdateDamageNumber(float deltaTime)
        {
            if (!_isNumberVisible)
            {
                return;
            }

            _numberElapsed += deltaTime;
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
        }

        private void StartFlash(Color color, float duration)
        {
            _flashRemaining = duration;
            ApplyColor(color);
            enabled = true;
        }

        private void ApplyColor(Color color)
        {
            for (var index = 0; index < _colorRenderers.Length; index++)
            {
                var targetRenderer = _colorRenderers[index];
                targetRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor(BaseColor, color);
                targetRenderer.SetPropertyBlock(_propertyBlock);
            }
        }

        private void ApplyRestingColor()
        {
            ApplyColor(_isTargetHighlighted ? _targetHighlightColor : _restingColor);
        }

        private void ValidateBindings(Renderer[] colorRenderers)
        {
            if (_damageText == null)
            {
                throw new InvalidOperationException(
                    "Actor Combat Feedback requires a damage text binding.");
            }

            if (colorRenderers == null || colorRenderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "Actor Combat Feedback requires at least one color renderer.");
            }

            if (_attackFlashDuration <= 0f ||
                _damageFlashDuration <= 0f ||
                _damageNumberDuration <= 0f ||
                _damageNumberRiseDistance < 0f)
            {
                throw new InvalidOperationException(
                    "Actor Combat Feedback durations must be positive and rise distance cannot be negative.");
            }

            for (var index = 0; index < colorRenderers.Length; index++)
            {
                if (colorRenderers[index] == null)
                {
                    throw new InvalidOperationException(
                        $"Actor Combat Feedback color renderer at index {index} is missing.");
                }
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
                    "TextMeshPro default font asset is required for greybox damage numbers.");
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
