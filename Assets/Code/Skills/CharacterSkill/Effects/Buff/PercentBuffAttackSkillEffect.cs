using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Skills.CharacterSkill.Core.EffectsCore;
using Code.Skills.CharacterSkill.Core.SkillAffectable;
using Code.Skills.CharacterSkill.Core.SkillAffectable.Base;
using Code.Utils.AsyncUtils;

namespace Code.Skills.CharacterSkill.Effects.Buff
{
public class PercentBuffAttackSkillEffect : IBuffSkillEffect
{
	public event Action EffectApplied;

	public string EffectId { get; }
	public int BuffDuration { get; }
	public int BuffAmount { get; }

	private readonly List<CancellationTokenSource> _buffCancellationTokenSources = new();

	public PercentBuffAttackSkillEffect(string effectId, int buffDuration, int buffAmount)
	{
		EffectId = effectId;
		BuffDuration = buffDuration;
		BuffAmount = buffAmount;
	}

	public void Dispose()
	{
		EffectApplied = null;

		foreach (var buffCancellationTokenSource in _buffCancellationTokenSources)
		{
			buffCancellationTokenSource.CancelAndDispose();
		}
	}

	public bool TryApplyEffect(ISkillAffectable target)
	{
		if (target.IsDead)
		{
			return false;
		}

		if (target is not IAttackBuffable attackBuffable)
		{
			return false;
		}

		var attackBuffCommand = new AttackBuffCommand(attackBuffable);
		attackBuffCommand.BuffAttack(BuffAmount);

		var buffCancellationTokenSource = new CancellationTokenSource();
		_buffCancellationTokenSources.Add(buffCancellationTokenSource);

		StartTrackBuffEnd(attackBuffCommand, BuffDuration, buffCancellationTokenSource.Token);

		return true;
	}

	private async void StartTrackBuffEnd(AttackBuffCommand attackBuffCommand, int buffDuration, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return;
		}

		if (buffDuration <= 0)
		{
			attackBuffCommand.UnbuffAttack();
		}

		await Task.Delay(buffDuration, token);

		if (token.IsCancellationRequested)
		{
			return;
		}

		attackBuffCommand.UnbuffAttack();
	}
}
}