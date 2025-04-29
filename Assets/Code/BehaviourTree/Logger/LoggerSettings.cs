using System;

namespace Code.BehaviourTree.Logger
{
public readonly struct LoggerSettings
{
	public string Prefix { get; }
	public string Postfix { get; }
	public Action<string> LogAction { get; }

	public LoggerSettings(string prefix, string postfix, Action<string> logAction)
	{
		Prefix = prefix;
		Postfix = postfix;
		LogAction = logAction;
	}

	public LoggerSettings(string prefix, Action<string> logAction)
	{
		Prefix = prefix;
		Postfix = string.Empty;
		;
		LogAction = logAction;
	}

	public LoggerSettings(Action<string> logAction)
	{
		LogAction = logAction;
		Prefix = string.Empty;
		Postfix = string.Empty;
	}
}
}