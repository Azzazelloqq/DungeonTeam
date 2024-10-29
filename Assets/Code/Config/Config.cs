using System;

namespace Code.Config
{
public class Config : IConfig
{
	private readonly IConfigParser _configParser;

	private readonly IConfigData[] _configDataContainer;
	
	public Config(IConfigParser configParser)
	{
		_configParser = configParser;
		_configDataContainer = _configParser.Parse();
	}
	
	public T GetData<T>() where T : IConfigData
	{
		foreach (var configData in _configDataContainer)
		{
			if (configData is T concreteData)
			{
				return concreteData;
			}
		}

		throw new Exception($"Need add data {typeof(T).Name} in {_configParser.GetType().Name}");
	}
}
}