using MVP;

namespace Code.DungeonTeam.CharacterHealth.Base
{
public abstract class CharacterHealthPresenterBase : Presenter<CharacterHealthViewBase, CharacterHealthModelBase>
{
	public abstract void TakeDamage(int damage);
	public abstract void Heal(int heal);
	public abstract void IncreaseLevel();
	public abstract void IncreaseLevel(int count);

	protected CharacterHealthPresenterBase(CharacterHealthViewBase view, CharacterHealthModelBase model) : base(view, model)
	{
	}
}
}