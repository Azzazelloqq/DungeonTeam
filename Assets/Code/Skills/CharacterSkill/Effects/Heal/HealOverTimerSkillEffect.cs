using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Utils.AsyncUtils;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Effects.Heal
{
public class HealOverTimerSkillEffect : IHealSkillEffect
{
	public event Action EffectApplied;

	public string EffectId { get; }
	public int TotalHealAmount { get; }

	private readonly IInGameLogger _logger;
	private readonly int _timeBetweenHeal;
	private readonly int _healByTick;
	private readonly List<CancellationTokenSource> _healOverTimeTokens = new();

	public HealOverTimerSkillEffect(string effectId, IInGameLogger logger, int durationInMilliseconds, int totalTotalHeal, int timeBetweenHeal)
	{
		_logger = logger;
		_timeBetweenHeal = timeBetweenHeal;
		EffectId = effectId;
		TotalHealAmount = totalTotalHeal;
		_healByTick = totalTotalHeal / (durationInMilliseconds / timeBetweenHeal);
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
		StartHealOverTime(healable, cancellationTokenSource.Token);
		
		return true;
	}

	public void Dispose()
	{
		_healOverTimeTokens.CancelAndDispose();
		_healOverTimeTokens.Clear();
	}

	private async void StartHealOverTime(IHealable healable, CancellationToken token)
	{
		try
		{
			if (_healByTick == 0)
			{
				healable.Heal(TotalHealAmount);
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
				
				if(healAmount >= TotalHealAmount)
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