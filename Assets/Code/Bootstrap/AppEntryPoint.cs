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

		[SerializeField]
		private Camera _worldCamera;

		private ApplicationRoot _applicationRoot;

		// ReSharper disable once UnusedMember.Local
		private async UniTask Start()
		{
			_applicationRoot = new ApplicationRoot(_canvasContext, _configCatalog, _worldCamera);

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
				DisposeApplicationRoot();
			}
		}

		private void OnApplicationQuit()
		{
			DisposeApplicationRoot();
		}

		private void OnDestroy()
		{
			DisposeApplicationRoot();
		}

		private void DisposeApplicationRoot()
		{
			var applicationRoot = _applicationRoot;
			_applicationRoot = null;
			applicationRoot?.Dispose();
		}
	}
}
