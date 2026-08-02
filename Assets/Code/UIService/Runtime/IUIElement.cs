using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.UIService
{
    public interface IUIElement
    {
        UIElementSettings Settings { get; }

        void HideImmediately();

        UniTask ShowAsync(CancellationToken token);

        UniTask HideAsync(CancellationToken token);
    }
}
