using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardViewModel : ViewModelBase<NoticeBoardModel>
    {
        private readonly Action<string> _contractSelected;
        private readonly Action _closed;
        private readonly IReadOnlyList<NoticeBoardItemViewModel> _items;

        public NoticeBoardViewModel(
            NoticeBoardModel model,
            Action<string> contractSelected,
            Action closed)
            : base(model)
        {
            _contractSelected = contractSelected ?? throw new ArgumentNullException(nameof(contractSelected));
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));

            var items = new List<NoticeBoardItemViewModel>(model.Offers.Count);
            for (var index = 0; index < model.Offers.Count; index++)
            {
                var item = new NoticeBoardItemViewModel(
                    new NoticeBoardItemModel(model.Offers[index]),
                    Select);
                item.Initialize();
                items.Add(item);
                compositeDisposable.AddDisposable(item);
            }

            _items = new ReadOnlyCollection<NoticeBoardItemViewModel>(items);
            model.SelectedContractId.Subscribe(UpdateSelectedItems).AddTo(compositeDisposable);
            CloseCommand = new RelayCommand<object>(_ => Close());
            CloseCommand.AddTo(compositeDisposable);
        }

        public NoticeBoardTextSnapshot Text => model.Text;
        public IReadOnlyList<NoticeBoardItemViewModel> Items => _items;
        public IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public IRelayCommand<object> CloseCommand { get; }

        public void Select(string contractId)
        {
            if (!model.CanSelect(contractId))
            {
                return;
            }

            _contractSelected(contractId);
            model.ApplyAcceptedSelection(contractId);
        }

        public void Close()
        {
            if (model.IsVisible.Value)
            {
                _closed();
            }
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() => Close();
        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            Close();
            return default;
        }

        private void UpdateSelectedItems(string selectedContractId)
        {
            for (var index = 0; index < _items.Count; index++)
            {
                _items[index].SetSelected(_items[index].ContractId == selectedContractId);
            }
        }
    }
}
