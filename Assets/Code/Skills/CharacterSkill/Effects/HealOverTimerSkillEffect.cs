using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Utils.AsyncUtils;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Effects
{
public class HealOverTimerSkillEffect : ISkillEffect
{
	public event Action EffectApplied;
	
	private readonly IInGameLogger _logger;
	private readonly int _timeBetweenHeal;
	private readonly int _healByTick;
	private readonly List<CancellationTokenSource> _healOverTimeTokens = new();
	private readonly int _totalHeal;

	public HealOverTimerSkillEffect(IInGameLogger logger, int durationInMilliseconds, int totalHeal, int timeBetweenHeal)
	{
		_logger = logger;
		_timeBetweenHeal = timeBetweenHeal;
		_totalHeal = totalHeal;
		_healByTick = totalHeal / (durationInMilliseconds / timeBetweenHeal);
	}

	public bool TryApplyEffect(ISkillAffectable target)
	{
		if (target.IsDead)
		{
			return false;
		}
		
		if(target is not IHealable healable)
		{
			return false;
		}

		var cancellationTokenSource = new CancellationTokenSource();
		StartDamageOverTime(healable, cancellationTokenSource.Token);
		
		return true;
	}

	public void Dispose()
	{
		_healOverTimeTokens.CancelAndDispose();
		_healOverTimeTokens.Clear();
	}

	private async void StartDamageOverTime(IHealable healable, CancellationToken token)
	{
		try
		{
			if (_healByTick == 0)
			{
				healable.Heal(_totalHeal);
				return;
			}
			
			var healAmount = 0;
		
			while (!healable.IsDead)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}
			
				healable.Heal(_healByTick);
				healAmount += _healByTick;
				
				EffectApplied?.Invoke();
				
				if(healAmount >= _totalHeal)
				{
					break;
				}
				
				await Task.Delay(_timeBetweenHeal, token);
			}
		}
		catch (Exception e)
		{
			if (e is not OperationCanceledException)
			{
				_logger.LogError("Error while applying heal over time effect" + e.Message);
			}
		}
	}
}
}