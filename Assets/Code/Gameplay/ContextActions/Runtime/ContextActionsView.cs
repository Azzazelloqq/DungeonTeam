using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.ContextActions.Runtime
{
    public sealed class ContextActionsView : ContextActionsViewBase
    {
        [SerializeField]
        private Vector2 _buttonSize = new(180f, 54f);

        [SerializeField, Min(0f)]
        private float _spacing = 12f;

        [SerializeField, Min(1f)]
        private float _fontSize = 24f;

        [SerializeField]
        private Color _buttonColor = new(0.08f, 0.16f, 0.26f, 0.95f);

        [SerializeField]
        private Color _textColor = Color.white;

        private readonly List<ButtonEntry> _buttons = new();
        private RectTransform _panel;

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
            _panel = null;
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
                typeof(HorizontalLayoutGroup),
                typeof(ContentSizeFitter));
            panelObject.layer = gameObject.layer;
            _panel = (RectTransform)panelObject.transform;
            _panel.SetParent(root, false);
            _panel.anchorMin = new Vector2(0.5f, 0f);
            _panel.anchorMax = new Vector2(0.5f, 0f);
            _panel.pivot = new Vector2(0.5f, 0f);
            _panel.anchoredPosition = new Vector2(0f, 48f);

            var layout = panelObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = _spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = panelObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
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
        }

        private ButtonEntry CreateButton(int index)
        {
            var buttonObject = new GameObject(
                $"Action_{index}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));
            buttonObject.layer = gameObject.layer;
            buttonObject.transform.SetParent(_panel, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = _buttonColor;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => viewModel.ExecuteCommand.Execute(index));

            var layout = buttonObject.GetComponent<LayoutElement>();
            layout.preferredWidth = _buttonSize.x;
            layout.preferredHeight = _buttonSize.y;

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
