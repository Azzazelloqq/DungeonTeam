using System.Collections.Generic;
using UnityEngine;

namespace Code.Utils.AnimationUtils
{
internal class AnimationStateMachineNotifier : StateMachineBehaviour
{
	private List<ObservableAnimator> _observableAnimators;

	internal void RegisterListener(ObservableAnimator observableAnimator)
	{
		_observableAnimators.Add(observableAnimator);
	}

	internal void UnregisterListener(ObservableAnimator listener)
	{
		_observableAnimators.Remove(listener);
	}

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);

		if (_observableAnimators == null)
		{
			return;
		}

		foreach (var startAnimationListener in _observableAnimators)
		{
			startAnimationListener.OnAnimationStarted(stateInfo);
		}
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateUpdate(animator, stateInfo, layerIndex);

		foreach (var updateAnimationListener in _observableAnimators)
		{
			updateAnimationListener.OnAnimationUpdate(stateInfo);
		}
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateExit(animator, stateInfo, layerIndex);

		foreach (var endAnimationListener in _observableAnimators)
		{
			endAnimationListener.OnAnimationEnd(stateInfo);
		}
	}
}
}