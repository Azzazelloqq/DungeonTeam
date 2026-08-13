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
        [SerializeField, Min(1f)] private float _targetMarkerSize = 96f;
        [SerializeField, Min(1f)] private float _targetMarkerThickness = 7f;

        [Header("State colors")]
        [SerializeField] private Color _readyColor = new(0.48f, 0.76f, 1f, 1f);
        [SerializeField] private Color _selectedColor = new(0.38f, 0.92f, 1f, 1f);
        [SerializeField] private Color _pendingColor = new(0.32f, 0.72f, 1f, 1f);
        [SerializeField] private Color _activeColor = new(1f, 0.62f, 0.24f, 1f);
        [SerializeField] private Color _busyColor = new(0.72f, 0.5f, 1f, 1f);
        [SerializeField] private Color _noTargetColor = new(0.82f, 0.38f, 0.34f, 1f);
        [SerializeField] private Color _disabledColor = new(0.26f, 0.32f, 0.4f, 0.72f);
        [SerializeField] private Color _cooldownColor = new(0.015f, 0.025f, 0.045f, 0.82f);
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _joystickBaseColor = new(0.48f, 0.75f, 1f, 0.58f);
        [SerializeField] private Color _joystickKnobColor = new(0.72f, 0.9f, 1f, 0.94f);
        [SerializeField] private Color _manualTargetColor = new(0.25f, 0.95f, 1f, 1f);
        [SerializeField] private Color _automaticTargetColor = new(0.65f, 0.82f, 0.9f, 0.58f);

        private readonly List<ButtonEntry> _buttons = new();
        private readonly List<Image> _targetMarkerSegments = new(4);
        private RectTransform _safeArea;
        private RectTransform _contextActionsHost;
        private RectTransform _targetMarker;
        private Canvas _canvas;
        private VirtualJoystickControl _joystickControl;
        private RawImage _joystickBase;
        private RawImage _joystickKnob;
        private Sprite _cooldownSprite;
        private bool _controlsEnabled;
        private bool _isApplyingSafeArea;
        private CombatHudTargetState _targetState;

        public override RectTransform ContextActionsHost => _contextActionsHost;

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
            viewModel.Target
                .Subscribe(ApplyTarget)
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
            _canvas = null;

            for (var index = 0; index < _buttons.Count; index++)
            {
                if (_buttons[index].Button != null)
                    _buttons[index].Button.onClick.RemoveAllListeners();
            }

            _buttons.Clear();
            _targetMarkerSegments.Clear();
            _safeArea = null;
            _contextActionsHost = null;
            _targetMarker = null;
            _targetState = CombatHudTargetState.Hidden;
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
            _canvas = root.GetComponentInParent<Canvas>();

            var safeAreaObject = new GameObject("SafeArea", typeof(RectTransform));
            safeAreaObject.layer = gameObject.layer;
            _safeArea = (RectTransform)safeAreaObject.transform;
            _safeArea.SetParent(root, false);
            _safeArea.offsetMin = Vector2.zero;
            _safeArea.offsetMax = Vector2.zero;
            ApplySafeArea();

            CreateContextActionsHost();
            CreateTargetMarker();
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
            ApplyTarget(_targetState);
        }

        private void CreateContextActionsHost()
        {
            var hostObject = new GameObject("ContextActionsHost", typeof(RectTransform));
            hostObject.layer = gameObject.layer;
            _contextActionsHost = (RectTransform)hostObject.transform;
            _contextActionsHost.SetParent(_safeArea, false);
            _contextActionsHost.anchorMin = Vector2.zero;
            _contextActionsHost.anchorMax = Vector2.one;
            _contextActionsHost.offsetMin = Vector2.zero;
            _contextActionsHost.offsetMax = Vector2.zero;
        }

        private void CreateTargetMarker()
        {
            var markerObject = new GameObject("TargetMarker", typeof(RectTransform));
            markerObject.layer = gameObject.layer;
            _targetMarker = (RectTransform)markerObject.transform;
            _targetMarker.SetParent(_safeArea, false);
            _targetMarker.anchorMin = new Vector2(0.5f, 0.5f);
            _targetMarker.anchorMax = new Vector2(0.5f, 0.5f);
            _targetMarker.pivot = new Vector2(0.5f, 0.5f);
            _targetMarker.sizeDelta = Vector2.one * _targetMarkerSize;

            CreateTargetMarkerSegment(
                "Top",
                new Vector2(0.5f, 1f),
                new Vector2(_targetMarkerSize, _targetMarkerThickness));
            CreateTargetMarkerSegment(
                "Bottom",
                new Vector2(0.5f, 0f),
                new Vector2(_targetMarkerSize, _targetMarkerThickness));
            CreateTargetMarkerSegment(
                "Left",
                new Vector2(0f, 0.5f),
                new Vector2(_targetMarkerThickness, _targetMarkerSize));
            CreateTargetMarkerSegment(
                "Right",
                new Vector2(1f, 0.5f),
                new Vector2(_targetMarkerThickness, _targetMarkerSize));
            markerObject.SetActive(false);
        }

        private void CreateTargetMarkerSegment(
            string name,
            Vector2 anchor,
            Vector2 size)
        {
            var segmentObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            segmentObject.layer = gameObject.layer;
            var segmentRect = (RectTransform)segmentObject.transform;
            segmentRect.SetParent(_targetMarker, false);
            segmentRect.anchorMin = anchor;
            segmentRect.anchorMax = anchor;
            segmentRect.pivot = anchor;
            segmentRect.sizeDelta = size;
            segmentRect.anchoredPosition = Vector2.zero;

            var segment = segmentObject.GetComponent<Image>();
            segment.color = _automaticTargetColor;
            segment.raycastTarget = false;
            _targetMarkerSegments.Add(segment);
        }

        private void ApplyTarget(CombatHudTargetState state)
        {
            _targetState = state;
            if (_targetMarker == null)
                return;

            if (!state.IsVisible ||
                state.ScreenPosition.x < 0f ||
                state.ScreenPosition.y < 0f ||
                state.ScreenPosition.x >= Screen.width ||
                state.ScreenPosition.y >= Screen.height)
            {
                _targetMarker.gameObject.SetActive(false);
                return;
            }

            var eventCamera = _canvas != null &&
                              _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _safeArea,
                    state.ScreenPosition,
                    eventCamera,
                    out var localPosition))
            {
                _targetMarker.gameObject.SetActive(false);
                return;
            }

            _targetMarker.anchoredPosition = localPosition;
            var color = state.Selection == CombatHudTargetSelection.Manual
                ? _manualTargetColor
                : _automaticTargetColor;
            for (var index = 0; index < _targetMarkerSegments.Count; index++)
                _targetMarkerSegments[index].color = color;

            _targetMarker.gameObject.SetActive(true);
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
                _busyColor,
                _noTargetColor,
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
            private readonly Color _busyColor;
            private readonly Color _noTargetColor;
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
                Color busyColor,
                Color noTargetColor,
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
                _busyColor = busyColor;
                _noTargetColor = noTargetColor;
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
                Button.interactable = _controlsEnabled && _state.CanActivate;
                _background.color = ResolveBackgroundColor();

                _icon.texture = _state.Icon;
                _icon.gameObject.SetActive(_state.Icon != null);

                switch (_state.Feedback)
                {
                    case CombatHudSlotFeedback.Casting:
                        ShowStatus("CAST");
                        return;
                    case CombatHudSlotFeedback.Recovery:
                        ShowStatus("RECOVERY");
                        return;
                    case CombatHudSlotFeedback.PendingApproach:
                        ShowStatus("APPROACH");
                        return;
                    case CombatHudSlotFeedback.Busy:
                        ShowStatus("BUSY");
                        return;
                    case CombatHudSlotFeedback.NoTargetOrInvalidTarget:
                        ShowStatus("NO TARGET");
                        return;
                    case CombatHudSlotFeedback.Cooldown:
                        ShowCooldown();
                        return;
                    default:
                        HideStatus();
                        return;
                }
            }

            private Color ResolveBackgroundColor()
            {
                return !_controlsEnabled
                    ? _disabledColor
                    : _state.Feedback switch
                    {
                        CombatHudSlotFeedback.Ready => _state.IsSelected
                            ? _selectedColor
                            : _readyColor,
                        CombatHudSlotFeedback.PendingApproach or CombatHudSlotFeedback.Casting =>
                            _pendingColor,
                        CombatHudSlotFeedback.Recovery => _activeColor,
                        CombatHudSlotFeedback.Busy => _busyColor,
                        CombatHudSlotFeedback.NoTargetOrInvalidTarget => _noTargetColor,
                        _ => _disabledColor
                    };
            }

            private void ShowCooldown()
            {
                _status.gameObject.SetActive(true);
                _status.SetText("{0:0.0}", _state.CooldownRemaining);
                _cooldownOverlay.gameObject.SetActive(true);
                _cooldownOverlay.fillAmount = Mathf.Clamp01(_state.CooldownProgress);
            }

            private void ShowStatus(string value)
            {
                _status.gameObject.SetActive(true);
                _status.SetText(value);
                _cooldownOverlay.gameObject.SetActive(false);
            }

            private void HideStatus()
            {
                _status.gameObject.SetActive(false);
                _cooldownOverlay.gameObject.SetActive(false);
            }
        }
    }
}
