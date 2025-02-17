using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public interface IUpdateAnimationListener : IAnimationStateMachineListener
{
	public void OnAnimationUpdate(AnimatorStateInfo stateInfo);
}
}