using UnityEngine;

namespace Code.Utils.AnimationUtils
{
public interface IStartAnimationListener : IAnimationStateMachineListener
{
	public void OnAnimationStarted(AnimatorStateInfo stateInfo);
}
}