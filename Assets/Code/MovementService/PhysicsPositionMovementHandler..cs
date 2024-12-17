using System.Collections.Generic;

namespace Code.MovementService
{
internal class PhysicsPositionMovementHandler
{
	private readonly List<PhysicsMovementTask> _tasks = new();

	public void AddTask(PhysicsMovementTask task)
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
			if (task.Rigidbody == null || task.Target.TransformIsNullOrDestroyed)
			{
				_tasks.RemoveAt(i);
				continue;
			}

			var direction = task.Target.Position - task.Rigidbody.position;
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
					task.Rigidbody.MovePosition(task.Target.Position);
				}
				else
				{
					task.Rigidbody.MovePosition(task.Rigidbody.position + moveDir);
				}
			}
		}
	}
}
}