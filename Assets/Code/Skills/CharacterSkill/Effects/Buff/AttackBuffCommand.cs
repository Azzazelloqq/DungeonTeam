using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Timer;

namespace Code.Skills.CharacterSkill.Effects.Buff
{
public class AttackBuffCommand
{
	private readonly IAttackBuffable _buffable;

	private int _attackBuffPercent;

	public AttackBuffCommand(IAttackBuffable buffable)
	{
		_buffable = buffable;
	}

	public void BuffAttack(int attackBuffPercent)
	{
		_attackBuffPercent = attackBuffPercent;
		_buffable.BuffAttack(attackBuffPercent);
	}

	public void UnbuffAttack()
	{
		_buffable.RemoveAttackBuff(_attackBuffPercent);
	}
}
}