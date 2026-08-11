using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.Skills.Domain;
using DungeonTeam.UI.CombatHud.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonTeam.UI.CombatHud
{
    public sealed class CombatHudView : CombatHudViewBase
    {
        [Header("Shared visuals")]
        [SerializeField] private Texture2D _joystickDisc;
        [SerializeField] private Material _circleMaterial;

        [Header("Mobile layout")]
        [SerializeField, Min(1f)] private float _joystickDiameter = 224f;
        [SerializeField, Min(1f)] private float _joystickKnobDiameter = 92f;
        [SerializeField, Min(1f)] private float _primarySkillSize = 152f;
        [SerializeField, Min(1f)] private float _activeSkillSize = 112f;
        [SerializeField] private Vector2 _edgePadding = new(52f, 48f);
        [SerializeField] private Vector2 _activeSkillOffset = new(-132f, 122f);
        [SerializeField, Range(0f, 0.45f)] private float _skillIconInset = 0.12f;
        [SerializeField, Min(1f)] private float _statusFontSize = 24f;

        [Header("State colors")]
        [SerializeField] private Color _readyColor = new(0.48f, 0.76f, 1f, 1f);
        [SerializeField] private Color _selectedColor = new(0.38f, 0.92f, 1f, 1f);
        [SerializeField] private Color _pendingColor = new(0.32f, 0.72f, 1f, 1f);
        [SerializeField] private Color _activeColor = new(1f, 0.62f, 0.24f, 1f);
        [SerializeField] private Color _disabledColor = new(0.26f, 0.32f, 0.4f, 0.72f);
        [SerializeField] private Color _cooldownColor = new(0.015f, 0.025f, 0.045f, 0.82f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _joystickBaseColor = new(0.48f, 0.75f, 1f, 0.58f);
        [SerializeField] private Color _joystickKnobColor = new(0.72f, 0.9f, 1f, 0.94f);

        private readonly List<ButtonEntry> _buttons = new();
        private RectTransform _safeArea;
        private VirtualJoystickControl _joystickControl;
        private RawImage _joystickBase;
        private RawImage _joystickKnob;
        private Sprite _cooldownSprite;
        private bool _controlsEnabled;
        private bool _isApplyingSafeArea;

        protected override void OnInitialize()
        {
            _controlsEnabled = viewModel.ControlsEnabled.Value;
            BuildLayout();

            var hasPrimarySlot = HasSlot(SkillSlot.Primary);
            for (var index = 0; index < viewModel.Slots.Count; index++)
            {
                var slotState = viewModel.Slots[index];
                var entry = CreateSkillButton(slotState.Value.Slot, hasPrimarySlot);
                _buttons.Add(entry);
                slotState.Subscribe(entry.Apply).AddTo(compositeDisposable);
            }

            viewModel.ControlsEnabled
                .Subscribe(ApplyControlsEnabled)
                .AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            if (_joystickControl != null)
                _joystickControl.Unbind();

            _joystickControl = null;
            _joystickBase = null;
            _joystickKnob = null;

            for (var index = 0; index < _buttons.Count; index++)
            {
                if (_buttons[index].Button != null)
                    _buttons[index].Button.onClick.RemoveAllListeners();
            }

            _buttons.Clear();
            _safeArea = null;
            DestroyCooldownSprite();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplySafeArea();
        }

        private void BuildLayout()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var safeAreaObject = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaObject.layer = gameObject.layer;
            _safeArea = (RectTransform)safeAreaObject.transform;
            _safeArea.SetParent(root, false);
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
            ApplySafeArea();

            CreateCooldownSprite();
            CreateJoystick();
        }

        private void ApplySafeArea()
        {
            if (_safeArea == null || _isApplyingSafeArea)
                return;

            _isApplyingSafeArea = true;
            try
            {
                var screenWidth = Screen.width;
                var screenHeight = Screen.height;
                if (screenWidth <= 0 || screenHeight <= 0)
                {
                    SetSafeAreaAnchors(Vector2.zero, Vector2.one);
                    return;
                }

                var safeArea = Screen.safeArea;
                var anchorMin = new Vector2(
                    Mathf.Clamp01(safeArea.xMin / screenWidth),
                    Mathf.Clamp01(safeArea.yMin / screenHeight));
                var anchorMax = new Vector2(
                    Mathf.Clamp01(safeArea.xMax / screenWidth),
                    Mathf.Clamp01(safeArea.yMax / screenHeight));
                if (anchorMax.x <= anchorMin.x || anchorMax.y <= anchorMin.y)
                {
                    anchorMin = Vector2.zero;
                    anchorMax = Vector2.one;
                }

                SetSafeAreaAnchors(anchorMin, anchorMax);
            }
            finally
            {
                _isApplyingSafeArea = false;
            }
        }

        private void SetSafeAreaAnchors(Vector2 anchorMin, Vector2 anchorMax)
        {
            if (_safeArea.anchorMin != anchorMin)
                _safeArea.anchorMin = anchorMin;
            if (_safeArea.anchorMax != anchorMax)
                _safeArea.anchorMax = anchorMax;

            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
        }

        private void CreateJoystick()
        {
            var joystickObject = new GameObject(
                "MovementJoystick",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(VirtualJoystickControl));
            joystickObject.layer = gameObject.layer;
            var joystickRect = (RectTransform)joystickObject.transform;
            joystickRect.SetParent(_safeArea, false);
            joystickRect.anchorMin = Vector2.zero;
            joystickRect.anchorMax = Vector2.zero;
            joystickRect.pivot = new Vector2(0.5f, 0.5f);
            joystickRect.sizeDelta = Vector2.one * _joystickDiameter;
            joystickRect.anchoredPosition = _edgePadding +
                                            Vector2.one * (_joystickDiameter * 0.5f);

            _joystickBase = joystickObject.GetComponent<RawImage>();
            ConfigureRoundImage(
                _joystickBase,
                _joystickDisc,
                _joystickBaseColor,
                raycastTarget: true);

            var knobObject = new GameObject(
                "Knob",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            knobObject.layer = gameObject.layer;
            var knobRect = (RectTransform)knobObject.transform;
            knobRect.SetParent(joystickRect, false);
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.sizeDelta = Vector2.one * _joystickKnobDiameter;
            knobRect.anchoredPosition = Vector2.zero;

            _joystickKnob = knobObject.GetComponent<RawImage>();
            ConfigureRoundImage(
                _joystickKnob,
                _joystickDisc,
                _joystickKnobColor,
                raycastTarget: false);

            _joystickControl = joystickObject.GetComponent<VirtualJoystickControl>();
            var travelRadius = Mathf.Max(
                1f,
                (_joystickDiameter - _joystickKnobDiameter) * 0.5f);
            _joystickControl.Bind(
                knobRect,
                travelRadius,
                movement => viewModel.SetMovementCommand.Execute(movement));
            _joystickControl.enabled = _controlsEnabled;
        }

        private ButtonEntry CreateSkillButton(SkillSlot slot, bool hasPrimarySlot)
        {
            var size = slot == SkillSlot.Primary ? _primarySkillSize : _activeSkillSize;
            var buttonObject = new GameObject(
                $"Skill_{slot}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage),
                typeof(Button));
            buttonObject.layer = gameObject.layer;
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(_safeArea, false);
            buttonRect.anchorMin = new Vector2(1f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = Vector2.one * size;
            buttonRect.anchoredPosition = GetSkillPosition(slot, hasPrimarySlot, size);

            var background = buttonObject.GetComponent<RawImage>();
            ConfigureRoundImage(
                background,
                _joystickDisc,
                _readyColor,
                raycastTarget: true);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => viewModel.RequestSkillCommand.Execute(slot));

            var icon = CreateSkillIcon(buttonRect, size);
            var cooldownOverlay = CreateCooldownOverlay(buttonRect, size);
            var status = CreateStatusText(buttonRect);
            var entry = new ButtonEntry(
                button,
                background,
                icon,
                cooldownOverlay,
                status,
                _readyColor,
                _selectedColor,
                _pendingColor,
                _activeColor,
                _disabledColor);
            entry.SetControlsEnabled(_controlsEnabled);
            return entry;
        }

        private Vector2 GetSkillPosition(SkillSlot slot, bool hasPrimarySlot, float size)
        {
            if (slot == SkillSlot.Primary)
            {
                return new Vector2(
                    -_edgePadding.x - _primarySkillSize * 0.5f,
                    _edgePadding.y + _primarySkillSize * 0.5f);
            }

            if (!hasPrimarySlot)
            {
                return new Vector2(
                    -_edgePadding.x - size * 0.5f,
                    _edgePadding.y + size * 0.5f);
            }

            return new Vector2(
                       -_edgePadding.x - _primarySkillSize * 0.5f,
                       _edgePadding.y + _primarySkillSize * 0.5f) +
                   _activeSkillOffset;
        }

        private RawImage CreateSkillIcon(RectTransform parent, float buttonSize)
        {
            var iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            iconObject.layer = gameObject.layer;
            var iconRect = (RectTransform)iconObject.transform;
            iconRect.SetParent(parent, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            var inset = buttonSize * _skillIconInset;
            iconRect.offsetMin = Vector2.one * inset;
            iconRect.offsetMax = -Vector2.one * inset;

            var icon = iconObject.GetComponent<RawImage>();
            icon.material = _circleMaterial;
            icon.color = Color.white;
            icon.raycastTarget = false;
            return icon;
        }

        private Image CreateCooldownOverlay(RectTransform parent, float buttonSize)
        {
            var overlayObject = new GameObject(
                "CooldownOverlay",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            overlayObject.layer = gameObject.layer;
            var overlayRect = (RectTransform)overlayObject.transform;
            overlayRect.SetParent(parent, false);
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            var inset = buttonSize * _skillIconInset;
            overlayRect.offsetMin = Vector2.one * inset;
            overlayRect.offsetMax = -Vector2.one * inset;

            var overlay = overlayObject.GetComponent<Image>();
            overlay.sprite = _cooldownSprite;
            overlay.material = _circleMaterial;
            overlay.color = _cooldownColor;
            overlay.type = Image.Type.Filled;
            overlay.fillMethod = Image.FillMethod.Radial360;
            overlay.fillOrigin = (int)Image.Origin360.Top;
            overlay.fillClockwise = true;
            overlay.fillAmount = 0f;
            overlay.raycastTarget = false;
            return overlay;
        }

        private TMP_Text CreateStatusText(RectTransform parent)
        {
            var textObject = new GameObject(
                "Status",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.layer = gameObject.layer;
            var textRect = (RectTransform)textObject.transform;
            textRect.SetParent(parent, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 8f);
            textRect.offsetMax = new Vector2(-8f, -8f);

            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = _statusFontSize;
            text.fontSizeMin = 12f;
            text.fontSizeMax = _statusFontSize;
            text.enableAutoSizing = true;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = _textColor;
            text.raycastTarget = false;
            return text;
        }

        private void ConfigureRoundImage(
            RawImage image,
            Texture texture,
            Color color,
            bool raycastTarget)
        {
            image.texture = texture;
            image.material = _circleMaterial;
            image.color = color;
            image.raycastTarget = raycastTarget;
        }

        private void ApplyControlsEnabled(bool isEnabled)
        {
            _controlsEnabled = isEnabled;
            if (_joystickControl != null)
                _joystickControl.enabled = isEnabled;

            if (_joystickBase != null)
            {
                _joystickBase.color = isEnabled
                    ? _joystickBaseColor
                    : WithAlpha(_joystickBaseColor, _joystickBaseColor.a * 0.4f);
            }

            if (_joystickKnob != null)
            {
                _joystickKnob.color = isEnabled
                    ? _joystickKnobColor
                    : WithAlpha(_joystickKnobColor, _joystickKnobColor.a * 0.4f);
            }

            for (var index = 0; index < _buttons.Count; index++)
                _buttons[index].SetControlsEnabled(isEnabled);
        }

        private bool HasSlot(SkillSlot slot)
        {
            for (var index = 0; index < viewModel.Slots.Count; index++)
            {
                if (viewModel.Slots[index].Value.Slot == slot)
                    return true;
            }

            return false;
        }

        private void CreateCooldownSprite()
        {
            if (_joystickDisc == null)
                return;

            _cooldownSprite = Sprite.Create(
                _joystickDisc,
                new Rect(0f, 0f, _joystickDisc.width, _joystickDisc.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _cooldownSprite.name = "CombatHudCooldownDisc";
        }

        private void DestroyCooldownSprite()
        {
            if (_cooldownSprite == null)
                return;

            if (Application.isPlaying)
                Destroy(_cooldownSprite);
            else
                DestroyImmediate(_cooldownSprite);

            _cooldownSprite = null;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private sealed class ButtonEntry
        {
            private readonly RawImage _background;
            private readonly RawImage _icon;
            private readonly Image _cooldownOverlay;
            private readonly TMP_Text _status;
            private readonly Color _readyColor;
            private readonly Color _selectedColor;
            private readonly Color _pendingColor;
            private readonly Color _activeColor;
            private readonly Color _disabledColor;

            private CombatHudSlotState _state;
            private bool _hasState;
            private bool _controlsEnabled;

            public ButtonEntry(
                Button button,
                RawImage background,
                RawImage icon,
                Image cooldownOverlay,
                TMP_Text status,
                Color readyColor,
                Color selectedColor,
                Color pendingColor,
                Color activeColor,
                Color disabledColor)
            {
                Button = button;
                _background = background;
                _icon = icon;
                _cooldownOverlay = cooldownOverlay;
                _status = status;
                _readyColor = readyColor;
                _selectedColor = selectedColor;
                _pendingColor = pendingColor;
                _activeColor = activeColor;
                _disabledColor = disabledColor;
            }

            public Button Button { get; }

            public void SetControlsEnabled(bool isEnabled)
            {
                _controlsEnabled = isEnabled;
                if (_hasState)
                    ApplyVisualState();
            }

            public void Apply(CombatHudSlotState state)
            {
                _state = state;
                _hasState = true;
                ApplyVisualState();
            }

            private void ApplyVisualState()
            {
                Button.interactable = _controlsEnabled && _state.IsReady;
                _background.color = !_controlsEnabled
                    ? _disabledColor
                    : _state.ActivePhase == SkillUsePhase.Preparing
                        ? _pendingColor
                        : _state.ActivePhase == SkillUsePhase.Recovering
                            ? _activeColor
                            : _state.IsPending
                                ? _pendingColor
                                : _state.IsSelected
                                    ? _selectedColor
                                    : _state.IsReady
                                        ? _readyColor
                                        : _disabledColor;

                _icon.texture = _state.Icon;
                _icon.gameObject.SetActive(_state.Icon != null);

                if (_state.ActivePhase.HasValue)
                {
                    _status.gameObject.SetActive(true);
                    _status.SetText(
                        _state.ActivePhase == SkillUsePhase.Preparing
                            ? "CAST"
                            : "RECOVERY");
                    _cooldownOverlay.gameObject.SetActive(false);
                    return;
                }

                var hasCooldown = _state.CooldownRemaining > 0f;
                _status.gameObject.SetActive(hasCooldown);
                _cooldownOverlay.gameObject.SetActive(hasCooldown);
                if (!hasCooldown)
                    return;

                _status.SetText("{0:0.0}", _state.CooldownRemaining);
                _cooldownOverlay.fillAmount = Mathf.Clamp01(_state.CooldownProgress);
            }
        }
    }
}
