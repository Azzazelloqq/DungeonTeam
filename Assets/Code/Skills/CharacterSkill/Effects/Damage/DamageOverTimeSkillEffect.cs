using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Utils.AsyncUtils;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Effects
{
public class DamageOverTimeSkillEffect : IDamageSkillEffect
{
	public event Action EffectApplied;
	
	public string EffectId { get; }
	public int TotalDamageAmount { get; }

	private readonly IInGameLogger _logger;
	private readonly int _timeBetweenDamage;
	private readonly int _damageByTick;
	private readonly List<CancellationTokenSource> _damageOverTimeTokens = new();

	public DamageOverTimeSkillEffect(string effectId, IInGameLogger logger, int durationInMilliseconds, int totalTotalDamage, int timeBetweenDamage)
	{
		EffectId = effectId;
		_logger = logger;
		_timeBetweenDamage = timeBetweenDamage;
		TotalDamageAmount = totalTotalDamage;
		_damageByTick = totalTotalDamage / (durationInMilliseconds / timeBetweenDamage);
	}
	
	public bool TryApplyEffect(ISkillAffectable target)
	{
		if (target.IsDead)
		{
			return false;
		}
		
		if(target is not IDamageable damageable)
		{
			return false;
		}

		var cancellationTokenSource = new CancellationTokenSource();
		StartDamageOverTime(damageable, cancellationTokenSource.Token);
		
		return true;
	}

	public void Dispose()
	{
		_damageOverTimeTokens.CancelAndDispose();
		_damageOverTimeTokens.Clear();

		EffectApplied = null;
	}

	private async void StartDamageOverTime(IDamageable damageable, CancellationToken token)
	{
		try
		{
			if (_damageByTick == 0)
			{
				damageable.TakeDamage(TotalDamageAmount);
				return;
			}
			
			var takenDamage = 0;
		
			while (!damageable.IsDead)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}
			
				damageable.TakeDamage(_damageByTick);
				takenDamage += _damageByTick;
				
				EffectApplied?.Invoke();
				
				if(takenDamage >= TotalDamageAmount)
				{
					break;
				}
				
				await Task.Delay(_timeBetweenDamage, token);
			}
		}
		catch (Exception e)
		{
			if (e is not OperationCanceledException)
			{
				_logger.LogError("Error while applying damage over time effect" + e.Message);
			}
		}
	}
}
}