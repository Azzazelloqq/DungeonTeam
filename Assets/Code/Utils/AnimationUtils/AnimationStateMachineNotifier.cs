using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public class AnimationStateMachineNotifier : StateMachineBehaviour
{
	private IAnimationStateMachineListener _listener;
	private IUpdateAnimationListener _updateAnimationListener;

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);
		
		_listener = animator.GetComponent<IAnimationStateMachineListener>();

		if (_listener is IStartAnimationListener startAnimationListener)
		{
			startAnimationListener.OnAnimationStarted(stateInfo);
		}
	}

	public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateUpdate(animator, stateInfo, layerIndex);
		
		if(_listener == null)
		{
			return;
		}

		if (_updateAnimationListener != null)
		{
			_updateAnimationListener.OnAnimationUpdate(stateInfo);
		}
		else if(_listener is IUpdateAnimationListener updateAnimationListener)
		{
			_updateAnimationListener = updateAnimationListener;
			_updateAnimationListener.OnAnimationUpdate(stateInfo);
		}
	}
	
	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateExit(animator, stateInfo, layerIndex);

		if (_listener is IEndAnimationListener endAnimationListener)
		{
			endAnimationListener.OnAnimationEnd(stateInfo);
		}
	}
}
}