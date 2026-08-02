using Code.Configuration;
using Code.UIService;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.ApplicationRoot
{
	internal class AppEntryPoint : MonoBehaviour
	{
		[SerializeField]
		private UICanvasContext _canvasContext;

		[SerializeField]
		private ConfigCatalog _configCatalog;

		private ApplicationRoot _applicationRoot;

		// ReSharper disable once UnusedMember.Local
		private async UniTask Start()
		{
			Application.quitting += OnApplicationQuit;

			_applicationRoot = new ApplicationRoot(_canvasContext, _configCatalog);
			await _applicationRoot.InitializeAsync(destroyCancellationToken);
		}

		private void OnApplicationQuit()
		{
			_applicationRoot?.Dispose();
		}
	}
}
