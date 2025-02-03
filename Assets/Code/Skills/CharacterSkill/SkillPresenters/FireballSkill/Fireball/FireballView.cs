using System.Collections;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;
using UnityEngine;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball
{
public class FireballView : FireballViewBase
{
	[field: SerializeField]
	public override Rigidbody Rigidbody { get; protected set; }

	[SerializeField]
	private float _blowUpDuration;

	public override Vector3 CurrentPosition => transform.position;

	private Coroutine _blowUpEffectRoutine;
	private WaitForSeconds _waitBlowUp;

	protected override void OnInitialize()
	{
		base.OnInitialize();

		_waitBlowUp = new WaitForSeconds(_blowUpDuration);
	}

	protected override void OnDispose()
	{
		base.OnDispose();

		if (_blowUpEffectRoutine != null)
		{
			StopCoroutine(_blowUpEffectRoutine);
		}
	}

	public override void ChargeFireball()
	{
		gameObject.SetActive(true);

		PlayChargeEffect();
	}

	public override void ActivateFireball()
	{
	}

	public override void UpdatePosition(Vector3 currentPosition, float deltaTime)
	{
		transform.position = currentPosition;
	}

	public override void BlowUpFireball()
	{
		PlayBlowUpEffect();
	}

	public override void HideFireball()
	{
		gameObject.SetActive(false);
	}

	private void PlayChargeEffect()
	{
	}

	private void PlayBlowUpEffect()
	{
		if (_blowUpEffectRoutine != null)
		{
			StopCoroutine(_blowUpEffectRoutine);
		}

		_blowUpEffectRoutine = StartCoroutine(BlowUpEffectWaiter());
	}

	private IEnumerator BlowUpEffectWaiter()
	{
		yield return _waitBlowUp;

		presenter.OnBlowUpEffectCompleted();
	}
}
}