using System.Threading;
using Azzazelloqq.MVVM.Core;
using Code.UIService;
using Cysharp.Threading.Tasks;

namespace Code.UI.MainMenu
{
    public abstract class MainMenuViewBase : ViewMonoBehavior<MainMenuViewModelBase>, IUIElement
    {
        public abstract UIElementSettings Settings { get; }

        public abstract void HideImmediately();

        public abstract UniTask ShowAsync(CancellationToken token);

        public abstract UniTask HideAsync(CancellationToken token);
    }
}
