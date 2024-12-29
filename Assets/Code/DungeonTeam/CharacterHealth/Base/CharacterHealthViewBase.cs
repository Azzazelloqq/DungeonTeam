using MVP;

namespace Code.DungeonTeam.CharacterHealth.Base
{
public abstract class CharacterHealthViewBase : ViewMonoBehaviour<CharacterHealthPresenterBase>
{
	public abstract void PlayTakeDamageEffect();
	public abstract void PlayHealEffect();
	public abstract void PlayIncreaseLevelEffect();
}
}