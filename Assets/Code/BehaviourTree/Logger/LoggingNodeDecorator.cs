namespace Code.BehaviourTree.Logger
{
public class LoggingNodeDecorator : IBehaviourTreeNode
{
	private readonly IBehaviourTreeNode _inner;
	private readonly LoggerSettings _loggerSettings;
	private NodeState _previousState;

	public LoggingNodeDecorator(IBehaviourTreeNode inner, LoggerSettings loggerSettings)
	{
		_inner = inner;
		_loggerSettings = loggerSettings;
	}

	public NodeState Tick()
	{
		var state = _inner.Tick();

		if (_previousState == state)
		{
			return state;
		}

		_previousState = state;
		var logPostfix = _loggerSettings.Postfix;
		var logPrefix = _loggerSettings.Prefix;
		var logAction = _loggerSettings.LogAction;
		var nodeName = _inner.GetType().Name;

		logAction.Invoke($"{logPrefix} {nodeName} → {state} {logPostfix}" + $"Node hash code: {_inner.GetHashCode()}");

		return state;
	}

	public void Dispose()
	{
		_inner.Dispose();
	}
}
}