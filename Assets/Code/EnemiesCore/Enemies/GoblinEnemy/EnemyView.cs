using System;
using System.Collections;
using Code.EnemiesCore.Enemies.Base.BaseEnemy;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.GoblinEnemy
{
public class EnemyView : EnemyViewBase
{
	private const float FLASH_DURATION = 0.1f;
	private const float BURN_DURATION = 0.1f;

	[SerializeField]
	private Renderer _goblinRenderer;

	private Material _goblinMaterial;
	private Color _originalColor;
	private Coroutine _commonTakeDamageEffectRoutine;
	private readonly Color _burnColor = new(1f, 0.3f, 0f);
	private Coroutine _burnEffectRoutine;


	protected override void OnInitialize()
	{
		base.OnInitialize();

		_goblinMaterial = _goblinRenderer.material;
		_originalColor = _goblinMaterial.color;
	}

	public override void TakeCommonAttackDamage()
	{
		if (_commonTakeDamageEffectRoutine != null)
		{
			StopCoroutine(_commonTakeDamageEffectRoutine);
		}

		_commonTakeDamageEffectRoutine = StartCoroutine(TakeDamageEffect(FLASH_DURATION));
	}


	public override void StartDieEffect()
	{
		throw new NotImplementedException();
	}

	private IEnumerator TakeDamageEffect(float duration)
	{
		_goblinMaterial.color = Color.white;

		yield return new WaitForSeconds(duration);

		_goblinMaterial.color = _originalColor;
	}

	private IEnumerator BurnEffect()
	{
		var elapsedTime = 0f;

		while (elapsedTime < BURN_DURATION)
		{
			elapsedTime += Time.deltaTime;
			var lerpFactor = elapsedTime / BURN_DURATION;

			_goblinMaterial.color = Color.Lerp(_originalColor, _burnColor, lerpFactor);

			yield return null;
		}

		_goblinMaterial.color = _originalColor;
	}
}
}