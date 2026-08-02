using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.LoadingScreen
{
    public sealed class LoadingScreenModel : LoadingScreenModelBase
    {
        private readonly ReactiveProperty<string> _statusText = new("Loading...");

        public IReadOnlyReactiveProperty<string> StatusText => _statusText;

        public LoadingScreenModel()
        {
            _statusText.AddTo(compositeDisposable);
        }

        public void SetStatusText(string statusText)
        {
            _statusText.SetValue(string.IsNullOrWhiteSpace(statusText) ? "Loading..." : statusText);
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
