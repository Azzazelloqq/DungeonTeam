using System;
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
			_applicationRoot = new ApplicationRoot(_canvasContext, _configCatalog);

			try
			{
				await _applicationRoot.InitializeAsync(destroyCancellationToken);
			}
			catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
			{
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				_applicationRoot.Dispose();
				_applicationRoot = null;
			}
		}

		private void OnDestroy()
		{
			_applicationRoot?.Dispose();
			_applicationRoot = null;
		}
	}
}
