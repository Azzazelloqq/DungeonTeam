using MVP;

namespace Code.DungeonTeam.CharacterHealth.Base
{
public abstract class CharacterHealthModelBase : Model
{
	public abstract void TakeDamage(int damage);
	public abstract void Heal(int heal);
	public abstract void IncreaseLevel();
}
}