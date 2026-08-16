using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardItemViewModel : ViewModelBase<NoticeBoardItemModel>
    {
        public NoticeBoardItemViewModel(NoticeBoardItemModel model, Action<string> selected)
            : base(model)
        {
            if (selected == null)
            {
                throw new ArgumentNullException(nameof(selected));
            }

            SelectCommand = new RelayCommand<object>(_ => selected(model.Offer.ContractId));
            SelectCommand.AddTo(compositeDisposable);
        }

        public string ContractId => model.Offer.ContractId;
        public string Title => model.Offer.Title.DisplayText;
        public string Summary => model.Offer.Summary.DisplayText;
        public bool IsAvailable => model.Offer.IsAvailable;
        public string DisabledReason => model.Offer.DisabledReason?.DisplayText ?? string.Empty;
        public IReadOnlyReactiveProperty<bool> IsSelected => model.IsSelected;
        public IRelayCommand<object> SelectCommand { get; }

        public void SetSelected(bool isSelected) => model.SetSelected(isSelected);

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
