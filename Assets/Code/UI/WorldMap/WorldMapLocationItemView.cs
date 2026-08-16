using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DungeonTeam.UI.WorldMap
{
    public sealed class WorldMapLocationItemView : MonoBehaviour, IDisposable
    {
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        private WorldMapLocationItemViewModel _viewModel;
        private UnityAction _selectRequested;

        public void ValidateBindings()
        {
            if (_button == null || _label == null)
            {
                throw new InvalidOperationException(
                    "World Map location item requires button and label bindings.");
            }
        }

        public void Initialize(
            WorldMapLocationItemViewModel viewModel,
            Action interactionStateChanged)
        {
            ValidateBindings();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            if (interactionStateChanged == null)
            {
                throw new ArgumentNullException(nameof(interactionStateChanged));
            }

            _label.text = BuildLabel(viewModel);
            _button.interactable = viewModel.IsAvailable;
            _selectRequested = () =>
            {
                _viewModel.Select();
                interactionStateChanged();
            };
            _button.onClick.AddListener(_selectRequested);
        }

        public void Dispose()
        {
            if (_button != null && _selectRequested != null)
            {
                _button.onClick.RemoveListener(_selectRequested);
            }

            _selectRequested = null;
            _viewModel = null;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private static string BuildLabel(WorldMapLocationItemViewModel viewModel)
        {
            var label = $"{viewModel.Title}\n{viewModel.Description}";
            return viewModel.IsAvailable
                ? label
                : $"{label}\n{viewModel.DisabledReason}";
        }
    }
}
