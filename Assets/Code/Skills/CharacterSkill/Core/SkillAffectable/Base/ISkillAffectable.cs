using Code.Utils.TransformUtils;
using UnityEngine;

namespace Code.Skills.CharacterSkill.Core.SkillAffectable.Base
{
public interface ISkillAffectable
{
	public bool IsDead { get; }

	public Vector3 GetPosition();
	public ReadOnlyTransform GetTransform();
}
}