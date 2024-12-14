namespace Code.Skills.CharacterSkill.Skills.FireballSkill
{
public struct FireballData
{
	public int Damage { get; }
	public float CooldownPerMilliseconds { get; }
	public int ChargeTimeMilliseconds { get; }
	
	public FireballData(int damage, int cooldownPerMilliseconds, int chargeTimeMilliseconds)
	{
		Damage = damage;
		CooldownPerMilliseconds = cooldownPerMilliseconds;
		ChargeTimeMilliseconds = chargeTimeMilliseconds;
	}
}
}