using System;
using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public delegate void AnimationStateInfoHandler(AnimatorStateInfo stateInfo);

public delegate void AnimationHashHandler(int stateHash);

public delegate void AnimationTimeHandler(int stateHash, float normalizedTime);

public class ObservableAnimator : Animator, IDisposable
{
	public event AnimationStateInfoHandler AnimationStarted;
	public event AnimationStateInfoHandler AnimationUpdate;
	public event AnimationStateInfoHandler AnimationEnd;

	public event AnimationHashHandler AnimationStartedByHash;
	public event AnimationHashHandler AnimationEndByHash;
	public event AnimationTimeHandler AnimationTimeUpdatedByHash;

	private AnimationStateMachineNotifier[] _cashedNotifiers;
	private bool _isDisposed;
	private bool _isObserving;

	public void StartObserve()
	{
		_cashedNotifiers = GetBehaviours<AnimationStateMachineNotifier>();
		foreach (var animationStateMachineListener in _cashedNotifiers)
		{
			animationStateMachineListener.RegisterListener(this);
		}

		_isObserving = true;
	}

	public void StopObserve()
	{
		UnsubscribeOnNotifierEvents();
		ClearEvents();

		_isObserving = false;
	}

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		UnsubscribeOnNotifierEvents();
		ClearEvents();

		Destroy(this);

		_isDisposed = true;
		_isObserving = false;
	}

	internal void OnAnimationStarted(AnimatorStateInfo stateInfo)
	{
		if (!_isObserving)
		{
			return;
		}

		AnimationStarted?.Invoke(stateInfo);

		var hash = stateInfo.shortNameHash;
		AnimationStartedByHash?.Invoke(hash);
	}

	internal void OnAnimationUpdate(AnimatorStateInfo stateInfo)
	{
		if (!_isObserving)
		{
			return;
		}

		AnimationUpdate?.Invoke(stateInfo);

		var hash = stateInfo.shortNameHash;
		var normalizedTime = stateInfo.normalizedTime;
		AnimationTimeUpdatedByHash?.Invoke(hash, normalizedTime);
	}

	internal void OnAnimationEnd(AnimatorStateInfo stateInfo)
	{
		if (!_isObserving)
		{
			return;
		}

		AnimationEnd?.Invoke(stateInfo);

		var hash = stateInfo.shortNameHash;
		AnimationEndByHash?.Invoke(hash);
	}

	private void ClearEvents()
	{
		AnimationStarted = null;
		AnimationUpdate = null;
		AnimationEnd = null;
	}

	private void UnsubscribeOnNotifierEvents()
	{
		if (_cashedNotifiers == null)
		{
			return;
		}

		foreach (var animationStateMachineListener in _cashedNotifiers)
		{
			animationStateMachineListener.UnregisterListener(this);
		}
	}
}
}