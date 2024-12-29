using System;
using System.Threading;
using System.Threading.Tasks;
using Code.DungeonTeam.CharacterHealth.Base;
using Code.GameConfig.ScriptableObjectParser.ConfigData.Characters;
using InGameLogger;

namespace Code.DungeonTeam.CharacterHealth
{
public class CharacterHealthModel : CharacterHealthModelBase
{
	private readonly IInGameLogger _logger;
	private readonly CharacterHealthByLevelConfig[] _healthByLevelConfig;
	public override int CurrentLevel { get; protected set; }
	public override int MaxHealth {get; protected set;}
	public override int CurrentHealth {get; protected set;}

	public CharacterHealthModel(
		IInGameLogger logger,
		CharacterHealthByLevelConfig[] healthByLevelConfig,
		int currentLevel,
		int currentHealth)
	{
		_logger = logger;
		_healthByLevelConfig = healthByLevelConfig;
		CurrentLevel = currentLevel;
		CurrentHealth = currentHealth;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();

		UpdateMaxHealth();
	}

	protected override Task OnInitializeAsync(CancellationToken token)
	{
		UpdateMaxHealth();
		
		return base.OnInitializeAsync(token);
	}

	protected override void OnDispose()
	{
		base.OnDispose();

		CurrentHealth = 0;
		CurrentLevel = 0;
	}

	public override void TakeDamage(int damage)
	{
		if (damage <= 0)
		{
			_logger.LogError("Damage must be greater than zero");
			
			return;
		}
		
		var newCurrentHealthValue = CurrentHealth - damage;
		
		CurrentHealth = CurrentHealth <= 0 ? 0 : newCurrentHealthValue;
	}

	public override void Heal(int heal)
	{
		if (heal <= 0)
		{
			_logger.LogError("Heal must be greater than zero");
			
			return;
		}
		
		var newCurrentHealthValue = CurrentHealth + heal;
		
		CurrentHealth = newCurrentHealthValue > MaxHealth ? MaxHealth : newCurrentHealthValue;
	}

	public override void IncreaseLevel()
	{
		var lastLevel = _healthByLevelConfig[^1];
		if (lastLevel.Level == CurrentLevel)
		{
			_logger.LogError("Character has reached the maximum level");
			
			return;
		}
		
		CurrentLevel++;
	}

	private void UpdateMaxHealth()
	{
		foreach (var healthByLevelConfig in _healthByLevelConfig)
		{
			if (healthByLevelConfig.Level != CurrentLevel)
			{
				continue;
			}
			
			MaxHealth = healthByLevelConfig.MaxHealth;
			
			return;
		}
		
		_logger.LogError($"Character level {CurrentLevel} not found in config");
	}
}
}