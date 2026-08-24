using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.GuildHall.Application;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class QuestBoardItemModel : ModelBase
    {
        private readonly ReactiveProperty<bool> _isAccepted;

        public QuestBoardItemModel(QuestBoardEntrySnapshot quest)
        {
            Quest = quest ?? throw new System.ArgumentNullException(nameof(quest));
            _isAccepted = new ReactiveProperty<bool>(quest.IsAccepted);
            _isAccepted.AddTo(compositeDisposable);
        }

        public QuestBoardEntrySnapshot Quest { get; }
        public IReadOnlyReactiveProperty<bool> IsAccepted => _isAccepted;
        public void MarkAccepted() => _isAccepted.SetValue(true);

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
