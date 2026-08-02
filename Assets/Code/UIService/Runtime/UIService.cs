using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.UIService
{
	public sealed class UIService : IUiService
	{
		private readonly object _sync = new();
		private readonly IResourceLoader _resourceLoader;
		private readonly UICanvasContext _canvasContext;
		private readonly CancellationTokenSource _lifetimeCancellation = new();
		private readonly Dictionary<IUIElement, UIElementEntry> _entries = new();
		private readonly HistoryGroupState _background = new();
		private readonly HistoryGroupState _fullScreen = new();
		private readonly PopupGroupState _popup = new();
		private readonly ParallelGroupState _overlayElement = new();
		private readonly ParallelGroupState _dynamicOverlayElement = new();

		private bool _isDisposed;

		public UIService(IResourceLoader resourceLoader, UICanvasContext canvasContext)
		{
			_resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
			_canvasContext = canvasContext;
		}

		public async UniTask<TUI> CreateAsync<TUI>(
			string addressableId,
			bool hideOnCreate = true,
			CancellationToken token = default)
			where TUI : class, IUIElement
		{
			ThrowIfDisposed();

			if (string.IsNullOrWhiteSpace(addressableId))
			{
				throw new ArgumentException("Addressable ID cannot be empty.", nameof(addressableId));
			}

			token.ThrowIfCancellationRequested();

			GameObject prefab = null;
			GameObject instanceObject = null;
			UIElementEntry entry = null;

			try
			{
				prefab = await _resourceLoader.LoadResourceAsync<GameObject>(addressableId, token);
				token.ThrowIfCancellationRequested();

				if (prefab == null)
				{
					throw new InvalidOperationException($"ResourceLoader returned no prefab for '{addressableId}'.");
				}

				if (prefab.activeSelf)
				{
					throw new InvalidOperationException(
						$"UI prefab '{addressableId}' must have an inactive root to prevent a visible first frame.");
				}

				var prefabElement = FindSingleElement<TUI>(prefab, addressableId);
				var settings = prefabElement.Settings;
				var parent = _canvasContext.GetParent(settings.Group);

				if (parent == null)
				{
					throw new InvalidOperationException(
						$"Canvas parent for group '{settings.Group}' was destroyed before '{addressableId}' was created.");
				}

				instanceObject = Object.Instantiate(prefab, parent, false);
				var instanceElement = FindSingleElement<TUI>(instanceObject, addressableId);

				if (instanceElement.Settings.Group != settings.Group ||
				    instanceElement.Settings.HideBehavior != settings.HideBehavior)
				{
					throw new InvalidOperationException(
						$"UI element '{addressableId}' changed its settings during instantiation.");
				}

				entry = new UIElementEntry(instanceElement, instanceObject, prefab, instanceElement.Settings);
				Register(entry);

				instanceElement.HideImmediately();
				instanceObject.SetActive(true);

				if (!hideOnCreate)
				{
					await ShowAsync(instanceElement, token);
				}

				return instanceElement;
			}
			catch
			{
				if (entry != null)
				{
					RemoveFromAllGroups(entry);
					Release(entry);
				}
				else
				{
					DestroyInstance(instanceObject);

					if (prefab != null)
					{
						_resourceLoader.ReleaseResource(prefab);
					}
				}

				throw;
			}
		}

		public UniTask ShowAsync(IUIElement element, CancellationToken token = default)
		{
			var entry = RequireEntry(element);

			return entry.Settings.Group switch
			{
				UIElementGroup.Background or UIElementGroup.FullScreen => EnqueueWithLifetime(
					GetHistoryGroup(entry.Settings.Group).Operations,
					transitionToken => ShowHistoryAsync(
						entry,
						GetHistoryGroup(entry.Settings.Group),
						transitionToken),
					token),
				UIElementGroup.Popup => ShowPopupAsync(entry, _popup, token),
				UIElementGroup.OverlayElement or UIElementGroup.DynamicOverlayElement => EnqueueWithLifetime(
					GetParallelGroup(entry.Settings.Group).Operations,
					transitionToken => ShowParallelAsync(
						entry,
						GetParallelGroup(entry.Settings.Group),
						transitionToken),
					token),
				_ => throw new ArgumentOutOfRangeException(nameof(element), entry.Settings.Group, "Unknown UI element group.")
			};
		}

		public UniTask HideAsync(IUIElement element, CancellationToken token = default)
		{
			var entry = RequireEntry(element);

			return entry.Settings.Group switch
			{
				UIElementGroup.Background or UIElementGroup.FullScreen => EnqueueWithLifetime(
					GetHistoryGroup(entry.Settings.Group).Operations,
					transitionToken => HideHistoryAsync(
						entry,
						GetHistoryGroup(entry.Settings.Group),
						transitionToken),
					token),
				UIElementGroup.Popup => EnqueueWithLifetime(
					_popup.Operations,
					transitionToken => HidePopupAsync(entry, _popup, transitionToken),
					token),
				UIElementGroup.OverlayElement or UIElementGroup.DynamicOverlayElement => EnqueueWithLifetime(
					GetParallelGroup(entry.Settings.Group).Operations,
					transitionToken => HideParallelAsync(
						entry,
						GetParallelGroup(entry.Settings.Group),
						transitionToken),
					token),
				_ => throw new ArgumentOutOfRangeException(nameof(element), entry.Settings.Group, "Unknown UI element group.")
			};
		}

		public UniTask CloseAsync(IUIElement element, CancellationToken token = default)
		{
			var entry = RequireEntry(element);

			return entry.Settings.Group switch
			{
				UIElementGroup.Background or UIElementGroup.FullScreen => EnqueueWithLifetime(
					GetHistoryGroup(entry.Settings.Group).Operations,
					transitionToken => CloseHistoryAsync(
						entry,
						GetHistoryGroup(entry.Settings.Group),
						transitionToken),
					token),
				UIElementGroup.Popup => EnqueueWithLifetime(
					_popup.Operations,
					transitionToken => ClosePopupAsync(entry, _popup, transitionToken),
					token),
				UIElementGroup.OverlayElement or UIElementGroup.DynamicOverlayElement => EnqueueWithLifetime(
					GetParallelGroup(entry.Settings.Group).Operations,
					transitionToken => CloseParallelAsync(
						entry,
						GetParallelGroup(entry.Settings.Group),
						transitionToken),
					token),
				_ => throw new ArgumentOutOfRangeException(nameof(element), entry.Settings.Group, "Unknown UI element group.")
			};
		}

		public void Dispose()
		{
			UIElementEntry[] entries;

			lock (_sync)
			{
				if (_isDisposed)
				{
					return;
				}

				_isDisposed = true;
				_lifetimeCancellation.Cancel();
				entries = new UIElementEntry[_entries.Count];
				_entries.Values.CopyTo(entries, 0);
			}

			CancelPendingPopupRequests();
			ClearGroupState();

			Exception firstError = null;

			foreach (var entry in entries)
			{
				try
				{
					if (entry.State != UIElementState.Closed)
					{
						entry.Element.HideImmediately();
					}

					Release(entry);
				}
				catch (Exception exception)
				{
					firstError ??= exception;
				}
			}

			_lifetimeCancellation.Dispose();

			if (firstError != null)
			{
				throw firstError;
			}
		}

		private async UniTask ShowPopupAsync(
			UIElementEntry entry,
			PopupGroupState group,
			CancellationToken token)
		{
			using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
				token,
				_lifetimeCancellation.Token);

			var transitionToken = linkedCancellation.Token;
			var request = new UIPopupRequest(entry, transitionToken, new UniTaskCompletionSource());

			try
			{
				await group.Operations.Enqueue(
					() => RegisterPopupShowAsync(request, group),
					transitionToken);
				await request.Completion.Task.AttachExternalCancellation(transitionToken);
			}
			catch (OperationCanceledException)
			{
				if (!_isDisposed)
				{
					await group.Operations.Enqueue(
						() => CancelPopupRequestAsync(request, group, transitionToken),
						CancellationToken.None);
				}

				throw;
			}
		}

		private async UniTask RegisterPopupShowAsync(UIPopupRequest request, PopupGroupState group)
		{
			ThrowIfClosed(request.Entry);

			if (group.Active == request.Entry && request.Entry.State == UIElementState.Visible)
			{
				request.Completion.TrySetResult();
				return;
			}

			if (ContainsPopupRequest(group, request.Entry))
			{
				throw new InvalidOperationException(
					$"UI element '{request.Entry.GameObject.name}' is already waiting in the popup queue.");
			}

			group.Queue.AddLast(request);

			if (group.Active == null)
			{
				await ShowNextPopupAsync(group);
			}
		}

		private UniTask CancelPopupRequestAsync(
			UIPopupRequest request,
			PopupGroupState group,
			CancellationToken cancellationToken)
		{
			var pending = FindPopupRequest(group, request.Entry);
			if (pending != null)
			{
				group.Queue.Remove(pending);
				request.Completion.TrySetCanceled(cancellationToken);
			}

			return UniTask.CompletedTask;
		}

		private async UniTask ShowHistoryAsync(
			UIElementEntry entry,
			HistoryGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);
			RemoveFromHistory(group, entry);

			if (group.Active == entry && entry.State == UIElementState.Visible)
			{
				return;
			}

			var previous = group.Active;
			group.Active = null;

			if (previous != null && previous != entry)
			{
				await HideElementAsync(previous, token);

				if (previous.Settings.HideBehavior == UIElementHideBehavior.KeepInQueue)
				{
					AddToHistory(group, previous);
				}
				else
				{
					Release(previous);
				}
			}

			await ShowElementAsync(entry, token);
			group.Active = entry;
		}

		private async UniTask HideHistoryAsync(
			UIElementEntry entry,
			HistoryGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Active != entry)
			{
				if (entry.Settings.HideBehavior == UIElementHideBehavior.Close)
				{
					RemoveFromHistory(group, entry);
					Release(entry);
				}

				return;
			}

			await HideElementAsync(entry, token);
			group.Active = null;

			var next = PopHistory(group, entry);

			if (entry.Settings.HideBehavior == UIElementHideBehavior.KeepInQueue)
			{
				AddToHistory(group, entry);
			}
			else
			{
				Release(entry);
			}

			if (next != null)
			{
				await ShowElementAsync(next, token);
				group.Active = next;
			}
		}

		private async UniTask CloseHistoryAsync(
			UIElementEntry entry,
			HistoryGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);
			RemoveFromHistory(group, entry);

			if (group.Active != entry)
			{
				Release(entry);
				return;
			}

			await HideElementAsync(entry, token);
			group.Active = null;
			Release(entry);

			var next = PopHistory(group, entry);
			if (next != null)
			{
				await ShowElementAsync(next, token);
				group.Active = next;
			}
		}

		private async UniTask HidePopupAsync(
			UIElementEntry entry,
			PopupGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Active != entry)
			{
				var pending = FindPopupRequest(group, entry);
				if (pending != null)
				{
					group.Queue.Remove(pending);
					pending.Value.Completion?.TrySetCanceled(token);

					if (entry.Settings.HideBehavior == UIElementHideBehavior.Close)
					{
						Release(entry);
					}
				}

				return;
			}

			await HideElementAsync(entry, token);
			group.Active = null;

			if (entry.Settings.HideBehavior == UIElementHideBehavior.Close)
			{
				Release(entry);
				await ShowNextPopupAsync(group);
				return;
			}

			await ShowNextPopupAsync(group);
			AddPopupRequest(group, new UIPopupRequest(entry, CancellationToken.None));
		}

		private async UniTask ClosePopupAsync(
			UIElementEntry entry,
			PopupGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Active == entry)
			{
				await HideElementAsync(entry, token);
				group.Active = null;
				Release(entry);
				await ShowNextPopupAsync(group);
				return;
			}

			var pending = FindPopupRequest(group, entry);
			if (pending != null)
			{
				group.Queue.Remove(pending);
				pending.Value.Completion?.TrySetCanceled(token);
			}

			Release(entry);

			if (group.Active == null)
			{
				await ShowNextPopupAsync(group);
			}
		}

		private async UniTask ShowNextPopupAsync(PopupGroupState group)
		{
			while (group.Active == null && group.Queue.First != null)
			{
				var request = group.Queue.First.Value;
				group.Queue.RemoveFirst();
				if (request.Entry.State == UIElementState.Closed)
				{
					request.Completion?.TrySetCanceled();
					continue;
				}

				if (request.Token.IsCancellationRequested)
				{
					request.Completion?.TrySetCanceled(request.Token);
					continue;
				}

				try
				{
					using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
						request.Token,
						_lifetimeCancellation.Token);
					await ShowElementAsync(request.Entry, linkedCancellation.Token);
					group.Active = request.Entry;
					request.Completion?.TrySetResult();
				}
				catch (OperationCanceledException exception)
				{
					request.Completion?.TrySetCanceled(exception.CancellationToken);
				}
				catch (Exception exception)
				{
					request.Completion?.TrySetException(exception);
				}
			}
		}

		private async UniTask ShowParallelAsync(
			UIElementEntry entry,
			ParallelGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Visible.Contains(entry))
			{
				return;
			}

			await ShowElementAsync(entry, token);
			group.Visible.Add(entry);
		}

		private async UniTask HideParallelAsync(
			UIElementEntry entry,
			ParallelGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Visible.Remove(entry))
			{
				await HideElementAsync(entry, token);
			}

			if (entry.Settings.HideBehavior == UIElementHideBehavior.Close)
			{
				Release(entry);
			}
		}

		private async UniTask CloseParallelAsync(
			UIElementEntry entry,
			ParallelGroupState group,
			CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (group.Visible.Remove(entry))
			{
				await HideElementAsync(entry, token);
			}

			Release(entry);
		}

		private async UniTask ShowElementAsync(UIElementEntry entry, CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (entry.State == UIElementState.Visible)
			{
				return;
			}

			entry.State = UIElementState.Showing;

			try
			{
				await entry.Element.ShowAsync(token);
				token.ThrowIfCancellationRequested();

				if (entry.State != UIElementState.Closed)
				{
					entry.State = UIElementState.Visible;
				}
			}
			catch
			{
				if (entry.State != UIElementState.Closed)
				{
					entry.State = UIElementState.Hidden;
				}

				throw;
			}
		}

		private async UniTask HideElementAsync(UIElementEntry entry, CancellationToken token)
		{
			ThrowIfClosed(entry);

			if (entry.State == UIElementState.Hidden)
			{
				return;
			}

			entry.State = UIElementState.Hiding;

			try
			{
				await entry.Element.HideAsync(token);
				token.ThrowIfCancellationRequested();

				if (entry.State != UIElementState.Closed)
				{
					entry.State = UIElementState.Hidden;
				}
			}
			catch
			{
				if (entry.State != UIElementState.Closed)
				{
					entry.State = UIElementState.Visible;
				}

				throw;
			}
		}

		private UniTask EnqueueWithLifetime(
			UIOperationQueue operations,
			Func<CancellationToken, UniTask> operation,
			CancellationToken token)
		{
			ThrowIfDisposed();
			return EnqueueWithLifetimeAsync(operations, operation, token);
		}

		private async UniTask EnqueueWithLifetimeAsync(
			UIOperationQueue operations,
			Func<CancellationToken, UniTask> operation,
			CancellationToken token)
		{
			using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
				token,
				_lifetimeCancellation.Token);
			var transitionToken = linkedCancellation.Token;

			await operations.Enqueue(
				() => operation(transitionToken),
				transitionToken);
		}

		private UIElementEntry RequireEntry(IUIElement element)
		{
			ThrowIfDisposed();

			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			lock (_sync)
			{
				if (!_entries.TryGetValue(element, out var entry))
				{
					throw new InvalidOperationException("The UI element is closed or belongs to another UI service.");
				}

				ThrowIfClosed(entry);
				return entry;
			}
		}

		private void Register(UIElementEntry entry)
		{
			lock (_sync)
			{
				if (_isDisposed)
				{
					throw new ObjectDisposedException(nameof(UIService));
				}

				_entries.Add(entry.Element, entry);
			}
		}

		private void Release(UIElementEntry entry)
		{
			lock (_sync)
			{
				if (entry.State == UIElementState.Closed)
				{
					return;
				}

				entry.State = UIElementState.Closed;
				_entries.Remove(entry.Element);
			}

			DestroyInstance(entry.GameObject);

			_resourceLoader.ReleaseResource(entry.Prefab);
		}

		private void RemoveFromAllGroups(UIElementEntry entry)
		{
			switch (entry.Settings.Group)
			{
				case UIElementGroup.Background:
				case UIElementGroup.FullScreen:
					var historyGroup = GetHistoryGroup(entry.Settings.Group);
					if (historyGroup.Active == entry)
					{
						historyGroup.Active = null;
					}

					RemoveFromHistory(historyGroup, entry);
					break;
				case UIElementGroup.Popup:
					if (_popup.Active == entry)
					{
						_popup.Active = null;
					}

					var popupRequest = FindPopupRequest(_popup, entry);
					if (popupRequest != null)
					{
						_popup.Queue.Remove(popupRequest);
						popupRequest.Value.Completion?.TrySetCanceled();
					}

					break;
				case UIElementGroup.OverlayElement:
				case UIElementGroup.DynamicOverlayElement:
					GetParallelGroup(entry.Settings.Group).Visible.Remove(entry);
					break;
				default:
					throw new ArgumentOutOfRangeException(
						nameof(entry),
						entry.Settings.Group,
						"Unknown UI element group.");
			}
		}

		private void CancelPendingPopupRequests()
		{
			var node = _popup.Queue.First;

			while (node != null)
			{
				var next = node.Next;
				node.Value.Completion?.TrySetCanceled(_lifetimeCancellation.Token);
				node = next;
			}

			_popup.Queue.Clear();
		}

		private void ClearGroupState()
		{
			ClearHistoryGroup(_background);
			ClearHistoryGroup(_fullScreen);
			_popup.Active = null;
			_popup.Queue.Clear();
			_overlayElement.Visible.Clear();
			_dynamicOverlayElement.Visible.Clear();
		}

		private static TUI FindSingleElement<TUI>(GameObject gameObject, string addressableId)
			where TUI : class, IUIElement
		{
			TUI match = null;
			var components = gameObject.GetComponents<MonoBehaviour>();

			foreach (var component in components)
			{
				if (component is not TUI candidate)
				{
					continue;
				}

				if (match != null)
				{
					throw new InvalidOperationException(
						$"UI prefab '{addressableId}' has more than one root component implementing '{typeof(TUI).Name}'.");
				}

				match = candidate;
			}

			return match ?? throw new InvalidOperationException(
				$"UI prefab '{addressableId}' has no root component implementing '{typeof(TUI).Name}'.");
		}

		private static void AddToHistory(HistoryGroupState group, UIElementEntry entry)
		{
			RemoveFromHistory(group, entry);
			group.History.Add(entry);
		}

		private static UIElementEntry PopHistory(HistoryGroupState group, UIElementEntry excluded)
		{
			for (var index = group.History.Count - 1; index >= 0; index--)
			{
				var candidate = group.History[index];
				group.History.RemoveAt(index);

				if (candidate != excluded && candidate.State != UIElementState.Closed)
				{
					return candidate;
				}
			}

			return null;
		}

		private static void RemoveFromHistory(HistoryGroupState group, UIElementEntry entry)
		{
			for (var index = group.History.Count - 1; index >= 0; index--)
			{
				if (group.History[index] == entry)
				{
					group.History.RemoveAt(index);
				}
			}
		}

		private static void AddPopupRequest(PopupGroupState group, UIPopupRequest request)
		{
			group.Queue.AddLast(request);
		}

		private static bool ContainsPopupRequest(PopupGroupState group, UIElementEntry entry)
		{
			return FindPopupRequest(group, entry) != null;
		}

		private static LinkedListNode<UIPopupRequest> FindPopupRequest(
			PopupGroupState group,
			UIElementEntry entry)
		{
			var node = group.Queue.First;
			while (node != null)
			{
				if (node.Value.Entry == entry)
				{
					return node;
				}

				node = node.Next;
			}

			return null;
		}

		private HistoryGroupState GetHistoryGroup(UIElementGroup group)
		{
			return group switch
			{
				UIElementGroup.Background => _background,
				UIElementGroup.FullScreen => _fullScreen,
				_ => throw new ArgumentOutOfRangeException(nameof(group), group, "UI group has no history.")
			};
		}

		private ParallelGroupState GetParallelGroup(UIElementGroup group)
		{
			return group switch
			{
				UIElementGroup.OverlayElement => _overlayElement,
				UIElementGroup.DynamicOverlayElement => _dynamicOverlayElement,
				_ => throw new ArgumentOutOfRangeException(nameof(group), group, "UI group is not parallel.")
			};
		}

		private static void ClearHistoryGroup(HistoryGroupState group)
		{
			group.Active = null;
			group.History.Clear();
		}

		private static void ThrowIfClosed(UIElementEntry entry)
		{
			if (entry.State == UIElementState.Closed)
			{
				throw new ObjectDisposedException(entry.GameObject != null ? entry.GameObject.name : "UI element");
			}
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(UIService));
			}
		}

		private static void DestroyInstance(GameObject instance)
		{
			if (instance == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(instance);
			}
			else
			{
				Object.DestroyImmediate(instance);
			}
		}

		private sealed class HistoryGroupState
		{
			public UIOperationQueue Operations { get; } = new();

			public UIElementEntry Active { get; set; }

			public List<UIElementEntry> History { get; } = new();
		}

		private sealed class PopupGroupState
		{
			public UIOperationQueue Operations { get; } = new();

			public UIElementEntry Active { get; set; }

			public LinkedList<UIPopupRequest> Queue { get; } = new();
		}

		private sealed class ParallelGroupState
		{
			public UIOperationQueue Operations { get; } = new();

			public HashSet<UIElementEntry> Visible { get; } = new();
		}
	}
}
