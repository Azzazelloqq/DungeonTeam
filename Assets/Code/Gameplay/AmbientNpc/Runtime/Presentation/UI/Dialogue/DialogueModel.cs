using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue
{
    public sealed class DialogueModel : DialogueModelBase
    {
        private readonly ReactiveProperty<string> _speaker = new(string.Empty);
        private readonly ReactiveProperty<string> _line = new(string.Empty);
        private readonly ReactiveProperty<bool> _isVisible = new(false);

        public DialogueModel()
        {
            _speaker.AddTo(compositeDisposable);
            _line.AddTo(compositeDisposable);
            _isVisible.AddTo(compositeDisposable);
        }

        public override IReadOnlyReactiveProperty<string> Speaker => _speaker;
        public override IReadOnlyReactiveProperty<string> Line => _line;
        public override IReadOnlyReactiveProperty<bool> IsVisible => _isVisible;

        public override void Show(string speaker, string line)
        {
            if (string.IsNullOrWhiteSpace(speaker) || string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentException("Dialogue speaker and line are required.");
            }

            _speaker.SetValue(speaker);
            _line.SetValue(line);
            _isVisible.SetValue(true);
        }

        public override void Hide() => _isVisible.SetValue(false);
        protected override void OnInitialize() { }
        protected override ValueTask OnInitializeAsync(CancellationToken token) => default;
        protected override void OnDispose() { }
        protected override ValueTask OnDisposeAsync(CancellationToken token) => default;
    }
}
