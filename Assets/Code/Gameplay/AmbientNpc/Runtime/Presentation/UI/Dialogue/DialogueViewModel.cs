using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue
{
    public sealed class DialogueViewModel : DialogueViewModelBase
    {
        private readonly Action _closed;

        public DialogueViewModel(DialogueModelBase model, Action closed) : base(model)
        {
            _closed = closed ?? throw new ArgumentNullException(nameof(closed));
            CloseCommand = new RelayCommand<object>(_ => Close());
            CloseCommand.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<string> Speaker => model.Speaker;
        public override IReadOnlyReactiveProperty<string> Line => model.Line;
        public override IReadOnlyReactiveProperty<bool> IsVisible => model.IsVisible;
        public override IRelayCommand<object> CloseCommand { get; }

        public void Close()
        {
            if (!model.IsVisible.Value)
            {
                return;
            }

            model.Hide();
            _closed();
        }

        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() => Close();
        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            Close();
            return default;
        }
    }
}
