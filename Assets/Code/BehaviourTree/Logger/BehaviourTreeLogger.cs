using System;
using System.Reflection;

namespace Code.BehaviourTree.Logger
{
public class BehaviourTreeLogger : IDisposable
{
	private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
	private const string ChildrenFieldName = "_children";

	private readonly LoggerSettings _loggerSettings;

	public BehaviourTreeLogger(LoggerSettings loggerSettings)
	{
		_loggerSettings = loggerSettings;
	}

	/// <summary>
	/// Wraps a behavior tree node with a logging decorator and recursively applies logging to all child nodes.
	/// </summary>
	/// <param name="root">The root behavior tree node to be wrapped with logging.</param>
	/// <returns>The behavior tree node wrapped in a logging decorator.</returns>
	/// <remarks>
	/// This method recursively traverses the behavior tree structure, starting from the specified root node,
	/// and wraps each node in a <see cref="LoggingNodeDecorator"/>, which adds logging functionality
	/// when each node's Tick() method is executed.
	/// 
	/// The logging includes: the node name, its execution result (Success, Failure, or Running),
	/// and customizable prefix and postfix text specified in the <see cref="LoggerSettings"/>.
	/// 
	/// The method first applies logging to child nodes using <see cref="WrapChildrenIfComposite"/>,
	/// and then wraps the node itself.
	/// </remarks>
	/// <example>
	/// <code>
	/// var loggerSettings = new LoggerSettings("[BehaviourTree]", logAction);
	/// var logger = new BehaviourTreeLogger(loggerSettings);
	/// var rootNode = new SelectorNode(new[] { childNode1, childNode2 });
	/// var loggingRoot = logger.WrapWithLogging(rootNode);
	/// </code>
	/// </example>
	public IBehaviourTreeNode WrapWithLogging(IBehaviourTreeNode root)
	{
		WrapChildrenIfComposite(root);
		return new LoggingNodeDecorator(root, _loggerSettings);
	}

	private void WrapChildrenIfComposite(IBehaviourTreeNode node)
	{
		var field = node.GetType().GetField(ChildrenFieldName, Flags);
		if (field == null)
		{
			return;
		}

		if (field.GetValue(node) is not IBehaviourTreeNode[] children)
		{
			return;
		}

		for (var i = 0; i < children.Length; i++)
		{
			children[i] = WrapWithLogging(children[i]);
		}

		field.SetValue(node, children);
	}

	public void Dispose()
	{
	}
}
}