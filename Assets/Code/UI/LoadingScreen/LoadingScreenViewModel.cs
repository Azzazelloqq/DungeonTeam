using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.LoadingScreen
{
    public sealed class LoadingScreenViewModel : LoadingScreenViewModelBase
    {
        private readonly LoadingScreenModel _loadingScreenModel;

        public LoadingScreenViewModel(LoadingScreenModel model) : base(model)
        {
            _loadingScreenModel = model;
            StatusText = model.StatusText;
        }

        public override IReadOnlyReactiveProperty<string> StatusText { get; }

        public void SetStatusText(string statusText)
        {
            _loadingScreenModel.SetStatusText(statusText);
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
