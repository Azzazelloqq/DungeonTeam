using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.UIService
{
	public interface IUiService : IDisposable
	{
		UniTask<TUI> CreateAsync<TUI>(
			string addressableId,
			bool hideOnCreate = true,
			CancellationToken token = default)
			where TUI : class, IUIElement;

		UniTask ShowAsync(IUIElement element, CancellationToken token = default);
		UniTask HideAsync(IUIElement element, CancellationToken token = default);
		UniTask CloseAsync(IUIElement element, CancellationToken token = default);
	}
}