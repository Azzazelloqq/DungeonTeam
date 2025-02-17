using System;
using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public class ObservableAnimator : Animator, IEndAnimationListener, IStartAnimationListener, IUpdateAnimationListener
{
	public event Action<AnimatorStateInfo> AnimationStarted;
	public event Action<AnimatorStateInfo> AnimationUpdate;
	public event Action<AnimatorStateInfo> AnimationEnd; 
	
	public void OnAnimationEnd(AnimatorStateInfo stateInfo)
	{
		AnimationEnd?.Invoke(stateInfo);
	}

	public void OnAnimationStarted(AnimatorStateInfo stateInfo)
	{
		AnimationStarted?.Invoke(stateInfo);
	}

	public void OnAnimationUpdate(AnimatorStateInfo stateInfo)
	{
		AnimationUpdate?.Invoke(stateInfo);
	}
}
}