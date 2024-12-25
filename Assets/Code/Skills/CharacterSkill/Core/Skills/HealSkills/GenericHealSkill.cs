using System;
using System.Collections.Generic;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Timer;
using Disposable.Utils;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Core.Skills.HealSkills
{
public class GenericHealSkill : ISkill
{
	public event Action ChargeCompleted;
	
	public string SkillId { get; }
	
	public bool IsReadyToActivate => !_cooldownTimer.IsInProgress && !_chargeTimer.IsInProgress;
	public bool IsOnCooldown => _cooldownTimer.IsInProgress;
	public bool IsCharging => _chargeTimer.IsInProgress;

	private readonly IInGameLogger _logger;
	private readonly int _chargeInMilliseconds;
	private readonly int _cooldownInMilliseconds;
	private readonly List<ISkillEffect> _effects;
	private readonly ActionTimer _chargeTimer;
	private readonly ActionTimer _cooldownTimer;

	public GenericHealSkill(
		IInGameLogger logger,
		ISkillEffect[] effects,
		string skillId,
		int chargeInMilliseconds,
		int cooldownInMilliseconds)
	{
		SkillId = skillId;
		_logger = logger;
		_chargeInMilliseconds = chargeInMilliseconds;
		_cooldownInMilliseconds = cooldownInMilliseconds;
		_effects = new List<ISkillEffect>(effects);
		_chargeTimer = new ActionTimer();
		_cooldownTimer = new ActionTimer();
	}

	public void Dispose()
	{
		_chargeTimer?.Dispose();
		_cooldownTimer?.Dispose();
		_effects.DisposeAll();
		ChargeCompleted = null;
	}

	public void StartChargeSkill() {
		if (_chargeTimer.IsInProgress)
		{
			_chargeTimer.StopTimer();
		}
		
		_chargeTimer.StartTimer(_chargeInMilliseconds, OnChargeCompleted);
	}

	public void Activate(ISkillAffectable skillAffectable)
	{
		if (_chargeTimer.IsInProgress)
		{
			_chargeTimer.StopTimer();
		}
		
		if (_cooldownTimer.IsInProgress)
		{
			_logger.LogError("Skill is on cooldown");
			return;
		}
		
		foreach (var effect in _effects)
		{
			effect.TryApplyEffect(skillAffectable);
		}
		
		_cooldownTimer.StartTimer(_cooldownInMilliseconds);
	}

	private void OnChargeCompleted()
	{
		ChargeCompleted?.Invoke();
	}

	public void AddEffect(ISkillEffect skillEffect)
	{
		_effects.Add(skillEffect);
	}
}
}