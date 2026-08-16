using System;
using System.Collections.Generic;
using System.Threading;
using Code.UIService;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonTeam.UI.WorldMap
{
    public sealed class WorldMapView : MonoBehaviour, IUIElement
    {
        [SerializeField] private UIElementSettings _settings = new(UIElementGroup.FullScreen, UIElementHideBehavior.KeepInQueue);
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Text _header;
        [SerializeField] private Button _backButton;
        [SerializeField] private Text _emptyState;
        [SerializeField] private RectTransform _itemContainer;
        [SerializeField] private WorldMapLocationItemView _itemTemplate;

        private WorldMapViewModel _viewModel;
        private readonly List<WorldMapLocationItemView> _itemViews = new();

        public UIElementSettings Settings => _settings;

        public void Initialize(WorldMapViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            if (_canvasGroup == null || _header == null || _backButton == null ||
                _emptyState == null || _itemContainer == null || _itemTemplate == null)
            {
                throw new InvalidOperationException(
                    "World Map view requires canvas, text, back button, item container and template bindings.");
            }

            if (_itemTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException("World Map location item template must be inactive.");
            }

            _itemTemplate.ValidateBindings();
            _header.text = viewModel.Context.Texts.Title.DisplayText;
            var backLabel = _backButton.GetComponentInChildren<Text>(includeInactive: true);
            if (backLabel == null)
            {
                throw new InvalidOperationException("World Map Back button requires a text label.");
            }

            backLabel.text = viewModel.Context.Texts.Back.DisplayText;
            _emptyState.text = viewModel.Context.Texts.Empty.DisplayText;
            _emptyState.gameObject.SetActive(viewModel.Items.Count == 0);
            _backButton.onClick.AddListener(OnBackRequested);
            CreateItemViews();
            UpdateInteractionState();
        }

        public void HideImmediately() => SetVisible(false);
        public UniTask ShowAsync(CancellationToken token) { SetVisible(true); return UniTask.CompletedTask; }
        public UniTask HideAsync(CancellationToken token) { SetVisible(false); return UniTask.CompletedTask; }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(OnBackRequested);
            }

            for (var index = _itemViews.Count - 1; index >= 0; index--)
            {
                _itemViews[index]?.Dispose();
            }

            _itemViews.Clear();
            _viewModel = null;
        }

        internal void RefreshInteractionState()
        {
            UpdateInteractionState();
        }

        private void CreateItemViews()
        {
            for (var index = 0; index < _viewModel.Items.Count; index++)
            {
                var itemView = Instantiate(_itemTemplate, _itemContainer);
                var itemTransform = (RectTransform)itemView.transform;
                itemTransform.anchorMin = new Vector2(0f, 1f);
                itemTransform.anchorMax = new Vector2(1f, 1f);
                itemTransform.pivot = new Vector2(0.5f, 1f);
                itemTransform.anchoredPosition = new Vector2(0f, -index * 92f);
                itemTransform.sizeDelta = new Vector2(0f, 82f);
                itemView.gameObject.SetActive(true);
                itemView.Initialize(_viewModel.Items[index], UpdateInteractionState);
                _itemViews.Add(itemView);
            }
        }

        private void OnBackRequested()
        {
            _viewModel.RequestBack();
            UpdateInteractionState();
        }

        private void UpdateInteractionState()
        {
            if (_canvasGroup == null || _viewModel == null)
            {
                return;
            }

            var isEnabled = !_viewModel.IsInteractionBlocked;
            _canvasGroup.interactable = isEnabled;
            _canvasGroup.blocksRaycasts = isEnabled;
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible && (_viewModel == null || !_viewModel.IsInteractionBlocked);
            _canvasGroup.blocksRaycasts = visible && (_viewModel == null || !_viewModel.IsInteractionBlocked);
        }
    }
}
