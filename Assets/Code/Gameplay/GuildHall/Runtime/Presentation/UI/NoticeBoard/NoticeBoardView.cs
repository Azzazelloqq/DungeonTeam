using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardView : NoticeBoardViewBase
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _headerText;
        [SerializeField] private TMP_Text _emptyStateText;
        [SerializeField] private RectTransform _itemContainer;
        [SerializeField] private NoticeBoardItemView _itemTemplate;
        [SerializeField] private QuestBoardItemView _questItemTemplate;
        [SerializeField] private Button _closeButton;

        private readonly List<NoticeBoardItemView> _itemViews = new();
        private readonly List<QuestBoardItemView> _questItemViews = new();
        private UnityAction _closeRequested;

        public override void ValidateBindings()
        {
            if (_panel == null || _headerText == null || _emptyStateText == null ||
                _itemContainer == null || _itemTemplate == null || _questItemTemplate == null || _closeButton == null)
            {
                throw new InvalidOperationException(
                    "Notice Board view requires panel, text, item container, template and close button.");
            }

            if (_itemTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Notice Board item template must be inactive.");
            }

            if (_questItemTemplate.gameObject.activeSelf)
            {
                throw new InvalidOperationException("Quest Board item template must be inactive.");
            }
        }

        internal int ItemCount => _itemViews.Count;
        internal NoticeBoardItemView GetItem(int index) => _itemViews[index];
        internal Button CloseButton => _closeButton;

        protected override void OnInitialize()
        {
            ValidateBindings();
            _headerText.SetText(viewModel.Text.Header.DisplayText);
            _emptyStateText.SetText(viewModel.Text.Empty.DisplayText);
            _closeRequested = () => viewModel.CloseCommand.Execute(null);
            _closeButton.onClick.AddListener(_closeRequested);
            CreateItemViews();
            _emptyStateText.gameObject.SetActive(viewModel.Items.Count == 0);
            viewModel.IsVisible.Subscribe(SetVisible).AddTo(compositeDisposable);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;

        protected override void OnDispose()
        {
            if (_closeButton != null && _closeRequested != null)
            {
                _closeButton.onClick.RemoveListener(_closeRequested);
            }

            _closeRequested = null;
            for (var index = _itemViews.Count - 1; index >= 0; index--)
            {
                var itemView = _itemViews[index];
                if (itemView != null)
                {
                    itemView.Dispose();
                    Destroy(itemView.gameObject);
                }
            }

            _itemViews.Clear();
            for (var index = _questItemViews.Count - 1; index >= 0; index--)
            {
                var itemView = _questItemViews[index];
                if (itemView != null)
                {
                    itemView.Dispose();
                    Destroy(itemView.gameObject);
                }
            }

            _questItemViews.Clear();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            OnDispose();
            return default;
        }

        private void CreateItemViews()
        {
            for (var index = 0; index < viewModel.Items.Count; index++)
            {
                var itemView = Instantiate(_itemTemplate, _itemContainer);
                var itemTransform = (RectTransform)itemView.transform;
                itemTransform.anchorMin = new Vector2(0f, 1f);
                itemTransform.anchorMax = new Vector2(1f, 1f);
                itemTransform.pivot = new Vector2(0.5f, 1f);
                itemTransform.anchoredPosition = new Vector2(0f, -index * 132f);
                itemTransform.sizeDelta = new Vector2(0f, 120f);
                itemView.gameObject.SetActive(true);
                itemView.Initialize(viewModel.Items[index], viewModel.Text);
                _itemViews.Add(itemView);
            }

            for (var index = 0; index < viewModel.QuestItems.Count; index++)
            {
                var itemView = Instantiate(_questItemTemplate, _itemContainer);
                var itemTransform = (RectTransform)itemView.transform;
                itemTransform.anchorMin = new Vector2(0f, 1f);
                itemTransform.anchorMax = new Vector2(1f, 1f);
                itemTransform.pivot = new Vector2(0.5f, 1f);
                itemTransform.anchoredPosition = new Vector2(0f, -(viewModel.Items.Count + index) * 132f);
                itemTransform.sizeDelta = new Vector2(0f, 120f);
                itemView.gameObject.SetActive(true);
                itemView.Initialize(viewModel.QuestItems[index], viewModel.Text);
                _questItemViews.Add(itemView);
            }
        }

        private void SetVisible(bool isVisible)
        {
            if (_panel != null)
            {
                _panel.SetActive(isVisible);
            }
        }
    }
}
