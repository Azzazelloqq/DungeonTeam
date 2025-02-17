using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public interface IEndAnimationListener : IAnimationStateMachineListener
{
	public void OnAnimationEnd(AnimatorStateInfo stateInfo);
}
}