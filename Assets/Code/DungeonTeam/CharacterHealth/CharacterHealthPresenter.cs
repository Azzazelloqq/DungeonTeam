using Code.DungeonTeam.CharacterHealth.Base;

namespace Code.DungeonTeam.CharacterHealth
{
public class CharacterHealthPresenter : CharacterHealthPresenterBase
{
	public override int MaxHealth => model.MaxHealth;
	public override int CurrentHealth => model.CurrentHealth;
	public override int CurrentLevel => model.CurrentLevel;
	public override bool IsNeedHeal => model.IsNeedHeal;

	public CharacterHealthPresenter(CharacterHealthViewBase view, CharacterHealthModelBase model) : base(view, model)
	{
	}

	public override void TakeDamage(int damage)
	{
		model.TakeDamage(damage);
		view.PlayTakeDamageEffect();
	}
	
	public override void Heal(int heal)
	{
		model.Heal(heal);
		view.PlayHealEffect();
	}

	public override void IncreaseLevel()
	{
		model.IncreaseLevel();
		view.PlayIncreaseLevelEffect();
	}

	public override void IncreaseLevel(int count)
	{
		for (var i = 0; i < count; i++)
		{
			model.IncreaseLevel();
			view.PlayIncreaseLevelEffect();
		}
	}
}
}