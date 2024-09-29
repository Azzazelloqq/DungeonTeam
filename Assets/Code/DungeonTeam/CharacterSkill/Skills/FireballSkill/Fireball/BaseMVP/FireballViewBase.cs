using System;
using MVP;
using UnityEngine;

namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill.Fireball.BaseMVP
{
public abstract class FireballViewBase : ViewMonoBehaviour<FireballPresenterBase>
{
	public abstract Vector3 CurrentPosition { get; }
	
	public abstract void ChargeFireball();
	public abstract void ActivateFireball();
	public abstract void UpdatePosition(Vector3 currentPosition, float deltaTime);
	public abstract void BlowUpFireball();
	public abstract void HideFireball();
}
}