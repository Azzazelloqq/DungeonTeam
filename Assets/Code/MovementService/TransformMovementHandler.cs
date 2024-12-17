using System.Collections.Generic;
using UnityEngine;

namespace Code.MovementService
{
/// <summary>
/// Handles frame-update (non-physics) movement tasks.
/// </summary>
public class TransformMovementHandler
{
	private readonly List<MovementTask> _tasks = new();

	public void AddTask(MovementTask task)
	{
		_tasks.Add(task);
	}

	public void Update(float deltaTime)
	{
		if (_tasks.Count == 0)
		{
			return;
		}

		for (var i = _tasks.Count - 1; i >= 0; i--)
		{
			var task = _tasks[i];
			if (task.Transform == null || task.Target.TransformIsNullOrDestroyed)
			{
				_tasks.RemoveAt(i);
				continue;
			}

			var direction = task.Target.Position - task.Transform.position;
			var distance = direction.magnitude;
			var isTargetReached = distance <= task.MinDistance;
			if (isTargetReached)
			{
				task.OnReachedTarget?.Invoke();
				_tasks.RemoveAt(i);
			}
			else
			{
				var moveDir = direction.normalized * task.Speed * deltaTime;
				if (moveDir.sqrMagnitude > distance * distance)
				{
					task.Transform.position = task.Target.Position;
				}
				else
				{
					task.Transform.position += moveDir;
				}
			}
		}
	}
}
}