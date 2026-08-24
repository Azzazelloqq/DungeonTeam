using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard
{
    public sealed class QuestBoardItemViewModel : ViewModelBase<QuestBoardItemModel>
    {
        private readonly Func<string, bool> _accepted;

        public QuestBoardItemViewModel(QuestBoardItemModel model, Func<string, bool> accepted)
            : base(model)
        {
            _accepted = accepted ?? throw new ArgumentNullException(nameof(accepted));
            AcceptCommand = new RelayCommand<object>(_ => Accept());
            AcceptCommand.AddTo(compositeDisposable);
        }

        public string QuestId => model.Quest.QuestId;
        public string Title => model.Quest.Title.DisplayText;
        public string Summary => model.Quest.Summary.DisplayText;
        public string Objective => model.Quest.Objective.DisplayText;
        public string Progress => model.Quest.Progress.DisplayText;
        public string StatusText => model.Quest.StatusText.DisplayText;
        public bool IsCompleted => model.Quest.IsCompleted;
        public bool CanAccept => model.Quest.CanAccept && !model.IsAccepted.Value;
        public IReadOnlyReactiveProperty<bool> IsAccepted => model.IsAccepted;
        public IRelayCommand<object> AcceptCommand { get; }

        public void Accept()
        {
            if (!CanAccept || !_accepted(QuestId)) return;
            model.MarkAccepted();
        }

        internal void AcceptLocally() => model.MarkAccepted();

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
