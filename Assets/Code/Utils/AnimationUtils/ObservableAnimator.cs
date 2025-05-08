using System;
using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public delegate void AnimationStateInfoHandler(AnimatorStateInfo stateInfo);

public delegate void AnimationHashHandler(int stateHash);

public delegate void AnimationTimeHandler(int stateHash, float normalizedTime);

[RequireComponent(typeof(Animator))]
public class ObservableAnimator : MonoBehaviour, IDisposable
{
	private static readonly int AttackSpeedMultiplayer = Animator.StringToHash("AttackSpeedMultiplayer");
	public event AnimationStateInfoHandler AnimationStarted;
	public event AnimationStateInfoHandler AnimationUpdate;
	public event AnimationStateInfoHandler AnimationEnd;
	public event AnimationHashHandler AnimationStartedByHash;
	public event AnimationHashHandler AnimationEndByHash;
	public event AnimationTimeHandler AnimationTimeUpdatedByHash;
	
	[SerializeField]
	private Animator _animator;
	
	private AnimationStateMachineNotifier[] _cashedNotifiers;
	private bool _isDisposed;
	private bool _isObserving;

	#if UNITY_EDITOR
	private void OnValidate()
	{
		if (_animator == null)
		{
			_animator = GetComponent<Animator>();
		}
	}
	#endif

	public void StartObserve()
	{
		_cashedNotifiers = _animator.GetBehaviours<AnimationStateMachineNotifier>();
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

	public void SetTrigger(int attackAnimationName)
	{
		_animator.SetTrigger(attackAnimationName);
	}

	public void SetTrigger(string triggerName)
	{
		_animator.SetTrigger(triggerName);
	}

	public void SetBool(int stateHash, bool value)
	{
		_animator.SetBool(stateHash, value);
	}

	public void SetBool(string triggerName, bool value)
	{
		_animator.SetBool(triggerName, value);
	}

	public void SetFloat(int stateHash, float value)
	{
		_animator.SetFloat(stateHash, value);
	}

	public void SetFloat(string triggerName, float value)
	{
		_animator.SetFloat(triggerName, value);
	}

	public void SetInt(int stateHash, int value)
	{
		_animator.SetInteger(stateHash, value);
	}

	public void SetInt(string triggerName, int value)
	{
		_animator.SetInteger(triggerName, value);
	}

	public void ChangeCurrentAnimationDuration(float animationDuration)
	{
		var clips = _animator.GetCurrentAnimatorClipInfo(0);
		if (clips.Length == 0)
		{
			return;
		}

		var currentClip = clips[0].clip;
		var multiplier = currentClip.length / animationDuration;
		_animator.SetFloat(AttackSpeedMultiplayer, multiplier);
	}
}
}