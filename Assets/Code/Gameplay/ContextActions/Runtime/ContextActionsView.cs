using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DG.Tweening;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.ContextActions.Runtime
{
    public sealed class ContextActionsView : ContextActionsViewBase
    {
        private const float MinimumTouchTargetSize = 112f;

        [SerializeField]
        private Vector2 _buttonSize = new(180f, MinimumTouchTargetSize);

        [SerializeField, Min(0f)]
        private float _spacing = 12f;

        [SerializeField, Min(1f)]
        private float _fontSize = 24f;

        [SerializeField]
        private Vector2 _panelOffset = new(52f, 340f);

        [SerializeField]
        private Color _buttonColor = new(0.08f, 0.16f, 0.26f, 0.95f);

        [SerializeField]
        private Color _textColor = Color.white;

        [SerializeField, Min(0.01f)]
        private float _visibilityTransitionDuration = 0.15f;

        private readonly List<ButtonEntry> _buttons = new();
        private RectTransform _panel;
        private CanvasGroup _panelCanvasGroup;
        private Tween _panelTween;
        private bool _isPanelVisible;

        protected override void OnInitialize()
        {
            BuildPanel();
            viewModel.Labels.Subscribe(UpdateButtons).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
            for (var index = 0; index < _buttons.Count; index++)
            {
                _buttons[index].Button.onClick.RemoveAllListeners();
            }

            _buttons.Clear();
            _panelTween?.Kill();
            _panelTween = null;
            _panelCanvasGroup = null;
            _panel = null;
            _isPanelVisible = false;
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void BuildPanel()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var panelObject = new GameObject(
                "Actions",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(GridLayoutGroup),
                typeof(ContentSizeFitter));
            panelObject.layer = gameObject.layer;
            _panel = (RectTransform)panelObject.transform;
            _panelCanvasGroup = panelObject.GetComponent<CanvasGroup>();
            _panel.SetParent(root, false);
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 0f);
            _panel.pivot = new Vector2(1f, 0f);
            _panel.anchoredPosition = new Vector2(-_panelOffset.x, _panelOffset.y);

            var layout = panelObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(
                Mathf.Max(MinimumTouchTargetSize, _buttonSize.x),
                Mathf.Max(MinimumTouchTargetSize, _buttonSize.y));
            layout.spacing = Vector2.one * _spacing;
            layout.childAlignment = TextAnchor.LowerRight;
            layout.startCorner = GridLayoutGroup.Corner.LowerRight;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SetPanelVisibleImmediately(false);
        }

        private void UpdateButtons(IReadOnlyList<string> labels)
        {
            for (var index = _buttons.Count; index < labels.Count; index++)
            {
                _buttons.Add(CreateButton(index));
            }

            for (var index = 0; index < _buttons.Count; index++)
            {
                var isVisible = index < labels.Count;
                var entry = _buttons[index];
                entry.Button.gameObject.SetActive(isVisible);
                if (isVisible)
                {
                    entry.Label.SetText(labels[index]);
                }
            }

            SetPanelVisible(labels.Count > 0);
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (_isPanelVisible == isVisible)
                return;

            _isPanelVisible = isVisible;
            _panelTween?.Kill();
            if (isVisible)
                _panel.gameObject.SetActive(true);

            _panelTween = DOTween.To(
                    () => _panelCanvasGroup.alpha,
                    value => _panelCanvasGroup.alpha = value,
                    isVisible ? 1f : 0f,
                    _visibilityTransitionDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(_panelCanvasGroup)
                .SetLink(_panel.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
                {
                    if (!isVisible)
                        _panel.gameObject.SetActive(false);

                    _panelTween = null;
                });
        }

        private void SetPanelVisibleImmediately(bool isVisible)
        {
            _isPanelVisible = isVisible;
            _panelCanvasGroup.alpha = isVisible ? 1f : 0f;
            _panel.gameObject.SetActive(isVisible);
        }

        private ButtonEntry CreateButton(int index)
        {
            var buttonObject = new GameObject(
                $"Action_{index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            buttonObject.layer = gameObject.layer;
            buttonObject.transform.SetParent(_panel, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = _buttonColor;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => viewModel.ExecuteCommand.Execute(index));

            var labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            labelObject.layer = gameObject.layer;
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = _fontSize;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = _textColor;
            label.raycastTarget = false;

            return new ButtonEntry(button, label);
        }

        private readonly struct ButtonEntry
        {
            public ButtonEntry(Button button, TMP_Text label)
            {
                Button = button;
                Label = label;
            }

            public Button Button { get; }

            public TMP_Text Label { get; }
        }
    }
}
