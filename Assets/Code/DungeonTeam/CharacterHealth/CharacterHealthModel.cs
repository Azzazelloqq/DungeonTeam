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
	private int _currentLevel;
	private int _currentHealth;
	private int _maxHealth;

	public CharacterHealthModel(IInGameLogger logger, CharacterHealthByLevelConfig[] healthByLevelConfig, int currentLevel, int currentHealth)
	{
		_logger = logger;
		_healthByLevelConfig = healthByLevelConfig;
		_currentLevel = currentLevel;
		_currentHealth = currentHealth;
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

		_currentHealth = 0;
		_currentLevel = 0;
	}

	public override void TakeDamage(int damage)
	{
		if (damage <= 0)
		{
			_logger.LogError("Damage must be greater than zero");
			
			return;
		}
		
		_currentHealth -= damage;
	}

	public override void Heal(int heal)
	{
		if (heal <= 0)
		{
			_logger.LogError("Heal must be greater than zero");
			
			return;
		}
		
		_currentHealth += heal;
	}

	public override void IncreaseLevel()
	{
		var lastLevel = _healthByLevelConfig[^1];
		if (lastLevel.Level == _currentLevel)
		{
			_logger.LogError("Character has reached the maximum level");
			
			return;
		}
		
		_currentLevel++;
	}

	private void UpdateMaxHealth()
	{
		foreach (var healthByLevelConfig in _healthByLevelConfig)
		{
			if (healthByLevelConfig.Level != _currentLevel)
			{
				continue;
			}
			
			_maxHealth = healthByLevelConfig.MaxHealth;
			
			return;
		}
		
		_logger.LogError($"Character level {_currentLevel} not found in config");
	}
}
}