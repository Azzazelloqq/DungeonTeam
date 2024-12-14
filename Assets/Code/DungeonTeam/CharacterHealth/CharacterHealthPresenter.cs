using Code.DungeonTeam.CharacterHealth.Base;

namespace Code.DungeonTeam.CharacterHealth
{
public class CharacterHealthPresenter : CharacterHealthPresenterBase
{
	public CharacterHealthPresenter(CharacterHealthViewBase view, CharacterHealthModelBase model) : base(view, model)
	{
	}
	
	public override void TakeDamage(int damage)
	{
		model.TakeDamage(damage);
	}
	
	public override void Heal(int heal)
	{
		model.Heal(heal);
	}

	public override void IncreaseLevel()
	{
		model.IncreaseLevel();
	}

	public override void IncreaseLevel(int count)
	{
		for (var i = 0; i < count; i++)
		{
			model.IncreaseLevel();
		}
	}
}
}