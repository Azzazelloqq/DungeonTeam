namespace Code.DungeonTeam.CharacterSkill.Skills.FireballSkill
{
public struct FireballData
{
	public float FireballSpeed { get; }
	public int Damage { get; }
	public float Cooldown { get; }
	public int ChargeTimeMilliseconds { get; }
	
	public FireballData(float fireballSpeed, int damage, float cooldown, int chargeTimeMilliseconds)
	{
		FireballSpeed = fireballSpeed;
		Damage = damage;
		Cooldown = cooldown;
		ChargeTimeMilliseconds = chargeTimeMilliseconds;
	}
}
}