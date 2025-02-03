using MVP;

namespace Code.DungeonTeam.CharacterHealth.Base
{
public abstract class CharacterHealthModelBase : Model
{
	public abstract int CurrentLevel { get; protected set; }
	public abstract int MaxHealth { get; protected set; }
	public abstract int CurrentHealth { get; protected set; }
	public abstract bool IsNeedHeal { get; }

	public abstract void TakeDamage(int damage);
	public abstract void IncreaseLevel();
	public abstract void Heal(int heal);
}
}