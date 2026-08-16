using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class NoticeBoardItemModel : ModelBase
    {
        private readonly ReactiveProperty<bool> _isSelected = new(false);

        public NoticeBoardItemModel(NoticeBoardOfferSnapshot offer)
        {
            Offer = offer ?? throw new System.ArgumentNullException(nameof(offer));
            _isSelected.AddTo(compositeDisposable);
        }

        public NoticeBoardOfferSnapshot Offer { get; }
        public IReadOnlyReactiveProperty<bool> IsSelected => _isSelected;

        public void SetSelected(bool isSelected) => _isSelected.SetValue(isSelected);

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
