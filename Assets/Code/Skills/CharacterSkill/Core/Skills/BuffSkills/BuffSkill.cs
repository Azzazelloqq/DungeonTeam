using System;
using System.Collections.Generic;
using Code.Skills.CharacterSkill.Core.EffectsCore.Base;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Skills.CharacterSkill.Core.Skills.Base;
using Code.Timer;
using Disposable.Utils;
using InGameLogger;

namespace Code.Skills.CharacterSkill.Core.Skills.BuffSkills
{
public class BuffSkill : ISkill
{
	public event Action ChargeCompleted;

	public string SkillId { get; }
	public bool IsReadyToActivate => !_cooldownTimer.IsInProgress && !_chargeTimer.IsInProgress;
	public bool IsOnCooldown => _cooldownTimer.IsInProgress;
	public bool IsCharging => _chargeTimer.IsInProgress;

	private readonly int _chargeInMilliseconds;
	private readonly int _cooldownInMilliseconds;
	private readonly List<ISkillEffect> _effects;
	private readonly ActionTimer _chargeTimer;
	private readonly ActionTimer _cooldownTimer;
	private readonly IInGameLogger _logger;

	public BuffSkill(
		string skillId,
		IInGameLogger logger,
		ISkillEffect[] effects,
		int chargeInMilliseconds,
		int cooldownInMilliseconds)
	{
		SkillId = skillId;
		_chargeInMilliseconds = chargeInMilliseconds;
		_cooldownInMilliseconds = cooldownInMilliseconds;
		_effects = new List<ISkillEffect>(effects);
		_logger = logger;
	}

	public void Dispose()
	{
		_chargeTimer.Dispose();
		_cooldownTimer.Dispose();
		_effects.DisposeAll();
		_effects.Clear();
	}

	public void StartChargeSkill()
	{
		if (_chargeTimer.IsInProgress)
		{
			_chargeTimer.StopTimer();
		}

		_chargeTimer.StartTimer(_chargeInMilliseconds, OnChargeCompleted);
	}

	private void OnChargeCompleted()
	{
		ChargeCompleted?.Invoke();
	}

	public void Activate(ISkillAffectable skillAffectable)
	{
		if (!IsReadyToActivate)
		{
			_logger.LogError("Skill is not ready to activate");

			return;
		}

		foreach (var effect in _effects)
		{
			effect.TryApplyEffect(skillAffectable);
		}

		_cooldownTimer.StartTimer(_cooldownInMilliseconds);
	}

	public void AddEffect(ISkillEffect skillEffect)
	{
		_effects.Add(skillEffect);
	}
}
}