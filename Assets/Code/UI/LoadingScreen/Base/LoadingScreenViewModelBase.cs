using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.UI.LoadingScreen
{
    public abstract class LoadingScreenViewModelBase : ViewModelBase<LoadingScreenModelBase>
    {
        protected LoadingScreenViewModelBase(LoadingScreenModelBase model) : base(model)
        {
        }

        public abstract IReadOnlyReactiveProperty<string> StatusText { get; }
    }
}
