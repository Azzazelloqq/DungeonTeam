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
        private readonly Func<string, bool> _contractAccepted;
        private readonly Func<string, bool> _questAccepted;
        private readonly Action _closed;
        private readonly IReadOnlyList<NoticeBoardItemViewModel> _items;
        private readonly IReadOnlyList<QuestBoardItemViewModel> _questItems;

        public NoticeBoardViewModel(
            NoticeBoardModel model,
            Action<string> contractSelected,
            Action closed)
            : this(
                model,
                contractSelected == null
                    ? null
                    : new Func<string, bool>(contractId =>
                    {
                        contractSelected(contractId);
                        return true;
                    }),
                _ => true,
                closed)
        {
        }

        public NoticeBoardViewModel(
            NoticeBoardModel model,
            Func<string, bool> contractAccepted,
            Action closed)
            : this(model, contractAccepted, _ => true, closed)
        {
        }

        public NoticeBoardViewModel(
            NoticeBoardModel model,
            Func<string, bool> contractAccepted,
            Func<string, bool> questAccepted,
            Action closed)
            : base(model)
        {
            _contractAccepted = contractAccepted ?? throw new ArgumentNullException(nameof(contractAccepted));
            _questAccepted = questAccepted ?? throw new ArgumentNullException(nameof(questAccepted));
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
            var quests = new List<QuestBoardItemViewModel>(model.Quests.Count);
            for (var index = 0; index < model.Quests.Count; index++)
            {
                var item = new QuestBoardItemViewModel(
                    new QuestBoardItemModel(model.Quests[index]), AcceptQuest);
                item.Initialize();
                quests.Add(item);
                compositeDisposable.AddDisposable(item);
            }

            _questItems = new ReadOnlyCollection<QuestBoardItemViewModel>(quests);
            model.SelectedContractId.Subscribe(UpdateSelectedItems).AddTo(compositeDisposable);
            CloseCommand = new RelayCommand<object>(_ => Close());
            CloseCommand.AddTo(compositeDisposable);
        }

        public NoticeBoardTextSnapshot Text => model.Text;
        public IReadOnlyList<NoticeBoardItemViewModel> Items => _items;
        public IReadOnlyList<QuestBoardItemViewModel> QuestItems => _questItems;
        public IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public IRelayCommand<object> CloseCommand { get; }

        public void Select(string contractId)
        {
            if (!model.CanSelect(contractId))
            {
                return;
            }

            if (_contractAccepted(contractId))
            {
                model.ApplyAcceptedSelection(contractId);
            }
        }

        private bool AcceptQuest(string questId)
        {
            if (!_questAccepted(questId)) return false;
            for (var index = 0; index < _questItems.Count; index++)
            {
                if (_questItems[index].QuestId == questId)
                {
                    _questItems[index].AcceptLocally();
                    return true;
                }
            }
            return false;
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
