using System;
using Code.Configuration;
using Code.UIService;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.DungeonRun.Runtime;
using DungeonTeam.Gameplay.Team.Runtime;
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

		[SerializeField]
		private DungeonRunBindings _dungeonRunBindings = new();

		[SerializeField]
		private TeamControlSettings _teamControlSettings = new();

		private ApplicationRoot _applicationRoot;

		// ReSharper disable once UnusedMember.Local
		private async UniTask Start()
		{
			_applicationRoot = new ApplicationRoot(
				_canvasContext,
				_configCatalog,
				_worldCamera,
				_dungeonRunBindings,
				_teamControlSettings);

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
