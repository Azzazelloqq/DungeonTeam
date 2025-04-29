using Code.DungeonTeam.CharacterHealth.Base;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Code.DungeonTeam.CharacterHealth
{
public class CharacterHealthView : CharacterHealthViewBase
{
	private const float ChangeHpMaxDuration = 1.5f;
	private const float ChangeHpMinDuration = 0f;

	[SerializeField]
	private Image _healthBar;

	[SerializeField]
	private Image _healthBarFillBackground;

	private Sequence _healthBarAnimationSequence;

	protected override void DisposeUnmanagedResources()
	{
		_healthBarAnimationSequence?.Kill();
	}

	public override void PlayTakeDamageEffect()
	{
		UpdateHealthBarFill(true);
	}

	public override void PlayHealEffect()
	{
		UpdateHealthBarFill(false);
	}

	public override void PlayIncreaseLevelEffect()
	{
	}

	private void UpdateHealthBarFill(bool isNegative)
	{
		_healthBarFillBackground.fillAmount = _healthBar.fillAmount;

		var currentHealth = presenter.CurrentHealth;
		var maxHealth = presenter.MaxHealth;

		var healthPercent = (float)currentHealth / maxHealth;
		var changeHpDuration = Mathf.Lerp(ChangeHpMinDuration, ChangeHpMaxDuration,
			Mathf.Abs(_healthBar.fillAmount - healthPercent));
		_healthBarFillBackground.color = isNegative ? Color.red : Color.green;

		_healthBarAnimationSequence = DOTween.Sequence();

		_healthBarAnimationSequence.Append(_healthBar.DOFillAmount(healthPercent, changeHpDuration));
		_healthBarAnimationSequence.Append(_healthBarFillBackground.DOFillAmount(healthPercent, changeHpDuration));

		_healthBarAnimationSequence.Play();
	}
}
}