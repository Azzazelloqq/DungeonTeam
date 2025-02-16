using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Code.EnemiesCore.Enemies.Base.BaseEnemy;
using UnityEngine;

namespace Code.EnemiesCore.Enemies.TestTeamEnemy
{
public class TestTakeDamageEnemyView : EnemyViewBase
{
	[SerializeField]
	private Renderer[] targetRenderer;

	private readonly Color _damageColor = Color.white;
	private readonly float _flashDuration = 0.1f;
	private Material[] _materialInstance;
	private Color[] _originalColor;
	private Coroutine _damageFlashRoutine;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		
		_materialInstance = new Material[targetRenderer.Length];
		for (var i = 0; i < targetRenderer.Length; i++)
		{
			_materialInstance[i] = targetRenderer[i].material;
		}
		
		_originalColor = new Color[targetRenderer.Length];
		for (var i = 0; i < targetRenderer.Length; i++)
		{
			_originalColor[i] = _materialInstance[i].color;
		}
	}

	protected override async Task OnInitializeAsync(CancellationToken token)
	{
		await base.OnInitializeAsync(token);
		
		_materialInstance = new Material[targetRenderer.Length];
		for (var i = 0; i < targetRenderer.Length; i++)
		{
			_materialInstance[i] = targetRenderer[i].material;
		}
		
		_originalColor = new Color[targetRenderer.Length];
		for (var i = 0; i < targetRenderer.Length; i++)
		{
			_originalColor[i] = _materialInstance[i].color;
		}
	}

	protected override void OnDispose()
	{
		base.OnDispose();
		
		if (_damageFlashRoutine != null)
		{
			StopCoroutine(_damageFlashRoutine);
		}
	}

	public override void TakeCommonAttackDamage()
	{
		TriggerDamageEffect();
	}

	public override void StartDieEffect()
	{
		Debug.Log("[Test enemy] DIE EFFECT");
	}
	
	public void TriggerDamageEffect()
	{
		if (_damageFlashRoutine != null)
		{
			StopCoroutine(_damageFlashRoutine);
		}
		
		_damageFlashRoutine = StartCoroutine(DamageFlash());
	}

	private IEnumerator DamageFlash()
	{
		foreach (var material in _materialInstance)
		{
			material.color = _damageColor;
		}

		yield return new WaitForSeconds(_flashDuration);

		for (var i = 0; i < _materialInstance.Length; i++)
		{
			_materialInstance[i].color = _originalColor[i];
		}
	}
}
}