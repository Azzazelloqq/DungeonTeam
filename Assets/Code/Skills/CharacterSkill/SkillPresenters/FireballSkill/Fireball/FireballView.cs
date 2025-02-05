using System.Collections;
using Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball.BaseMVP;
using DG.Tweening;
using UnityEngine;

namespace Code.Skills.CharacterSkill.SkillPresenters.FireballSkill.Fireball
{
public class FireballView : FireballViewBase
{
	private const float ChargeDuration = 1;

	[field: SerializeField]
	public override Rigidbody Rigidbody { get; protected set; }

	[SerializeField]
	private float _blowUpDuration;

	[SerializeField]
	private ParticleSystem _blowUpEffect;

	[SerializeField]
	private GameObject _sphere;
	
	public override Vector3 CurrentPosition => transform.position;

	private Coroutine _blowUpEffectRoutine;
	private WaitForSeconds _waitBlowUp;
	private Tween _chargeSphereTween;

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
		_blowUpEffect.Stop();
		_sphere.gameObject.SetActive(true);
		_sphere.transform.localScale = new Vector3(1, 1, 1);
	}

	private void PlayChargeEffect()
	{
		_sphere.transform.localScale = Vector3.zero;
		
		_chargeSphereTween?.Kill();
		_chargeSphereTween = _sphere.transform.DOScale(new Vector3(1, 1, 1), ChargeDuration);
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
		_blowUpEffect.Play();
		_sphere.gameObject.SetActive(false);
		
		yield return _waitBlowUp;

		presenter.OnBlowUpEffectCompleted();
	}
}
}