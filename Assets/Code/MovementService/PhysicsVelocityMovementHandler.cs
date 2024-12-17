using System.Collections.Generic;
using UnityEngine;

namespace Code.MovementService
{
/// <summary>
/// Handles physics-based movement tasks using velocity. 
/// For simplicity, we assume the object should move in direction of target at a given speed 
/// until minDistance is reached, using Rigidbody velocity. 
/// Another approach could be to directly set velocity each frame.
/// Here we will directly set the Rigidbody's velocity.
/// </summary>
internal class PhysicsVelocityMovementHandler
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
				task.Rigidbody.linearVelocity = Vector3.zero;
				_tasks.RemoveAt(i);
			}
			else
			{
				var velocity = direction.normalized * task.Speed;
				if ((task.Rigidbody.position + velocity * deltaTime - task.Target.Position).sqrMagnitude <
					task.MinDistance * task.MinDistance)
				{
					task.Rigidbody.position = task.Target.Position;
					task.Rigidbody.linearVelocity = Vector3.zero;
					task.OnReachedTarget?.Invoke();
					_tasks.RemoveAt(i);
				}
				else
				{
					task.Rigidbody.linearVelocity = velocity;
				}
			}
		}
	}
}
}