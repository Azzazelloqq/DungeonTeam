using System.Threading;
using Code.UIService;
using Cysharp.Threading.Tasks;
using LightDI.Runtime;
using RootPattern;

namespace Code.MainMenu
{
	public class MainMenuRoot : Root
	{
		private readonly IUiService _uiService;

		public MainMenuRoot([Inject] IUiService uiService)
		{
			_uiService = uiService;
		}
		
		protected override UniTask OnInitializeAsync(CancellationToken token)
		{
			
			return UniTask.CompletedTask;
		}
	}
}