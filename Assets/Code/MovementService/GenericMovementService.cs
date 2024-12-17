using System;
using Code.Utils.TransformUtils;
using InGameLogger;
using TickHandler;
using UnityEngine;

namespace Code.MovementService
{
/// <summary>
/// A versatile movement service designed to handle various movement scenarios in Unity.
/// You can use it to move a game object towards a target, move it in a certain direction,
/// or apply velocities and accelerations. The service is stateless and doesn't hold internal state, 
/// so you can call its methods from anywhere in your code (e.g., Update methods, coroutines).
/// </summary>
public class GenericMovementService : IMovementService
{
	private readonly ITickHandler _tickHandler;
	private readonly IInGameLogger _logger;
	private readonly TransformMovementHandler _transformHandler = new();
	private readonly PhysicsPositionMovementHandler _physicsPositionHandler = new();
	private readonly PhysicsVelocityMovementHandler _physicsVelocityHandler = new();

	public GenericMovementService(ITickHandler tickHandler, IInGameLogger logger)
	{
		_tickHandler = tickHandler;
		_logger = logger;
		_tickHandler.FrameUpdate += OnFrameUpdate;
		_tickHandler.PhysicUpdate += OnPhysicUpdate;
	}

	public void Dispose()
	{
		_tickHandler.FrameUpdate -= OnFrameUpdate;
		_tickHandler.PhysicUpdate -= OnPhysicUpdate;
	}

	/// <summary>
	/// Start a transform-based movement task.
	/// </summary>
	public void StartMoveTowardsTarget(
		Transform transform,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null)
	{
		if (transform == null || target.TransformIsNullOrDestroyed)
		{
			_logger.LogError("Invalid arguments for StartMoveTowardsTarget");
			return;
		}

		_transformHandler.AddTask(new MovementTask(transform, target, speed, minDistance, onReachedTarget));
	}

	/// <summary>
	/// Start a physics-based movement task using MovePosition.
	/// </summary>
	public void StartMoveRigidbodyTowardsTarget(
		Rigidbody rigidbody,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null)
	{
		if (rigidbody == null || target.TransformIsNullOrDestroyed)
		{
			_logger.LogError("Invalid arguments for StartMoveRigidbodyTowardsTarget");
			return;
		}

		_physicsPositionHandler.AddTask(new PhysicsMovementTask(rigidbody, target, speed, minDistance, onReachedTarget));
	}

	/// <summary>
	/// Start a physics-based movement task using velocity.
	/// </summary>
	public void StartMoveRigidbodyWithVelocity(
		Rigidbody rigidbody,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null)
	{
		if (rigidbody == null || target.TransformIsNullOrDestroyed)
		{
			_logger.LogError("Invalid arguments for StartMoveRigidbodyWithVelocity");
			return;
		}

		_physicsVelocityHandler.AddTask(new PhysicsMovementTask(rigidbody, target, speed, minDistance, onReachedTarget));
	}

	/// <summary>
	/// Moves a transform towards a target position at a given speed.
	/// This does not use Unity's physics and simply changes the Transform's position.
	/// Typically called in Update().
	/// </summary>
	public void MoveTowards(Transform transform, Vector3 targetPosition, float speed, float deltaTime)
	{
		var direction = (targetPosition - transform.position).normalized;
		var distance = speed * deltaTime;
		if ((transform.position - targetPosition).sqrMagnitude <= distance * distance)
		{
			transform.position = targetPosition;
		}
		else
		{
			transform.position += direction * distance;
		}
	}

	/// <summary>
	/// Moves a transform in a given direction at a specified speed.
	/// The direction should be a normalized vector.
	/// </summary>
	public void MoveInDirection(Transform transform, Vector3 direction, float speed, float deltaTime)
	{
		transform.position += direction * speed * deltaTime;
	}

	/// <summary>
	/// Linearly interpolates the position between current and target, controlled by t.
	/// Useful for smooth transitions.
	/// </summary>
	public Vector3 LerpPosition(Vector3 currentPosition, Vector3 targetPosition, float t)
	{
		return Vector3.Lerp(currentPosition, targetPosition, t);
	}

	/// <summary>
	/// Smoothly dampes towards a target. Wraps Vector3.SmoothDamp.
	/// Keep a persistent 'velocity' vector for this to work properly across multiple calls.
	/// </summary>
	public Vector3 SmoothDamp(
		Vector3 current,
		Vector3 target,
		ref Vector3 currentVelocity,
		float smoothTime,
		float maxSpeed,
		float deltaTime)
	{
		return Vector3.SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
	}

	/// <summary>
	/// Moves a rigidbody by applying a velocity for a given time step.
	/// Uses Rigidbody.MovePosition for physics-based movement.
	/// </summary>
	public void MoveWithVelocity(Rigidbody rigidbody, Vector3 velocity, float deltaTime)
	{
		rigidbody.MovePosition(rigidbody.position + velocity * deltaTime);
	}

	/// <summary>
	/// Moves a rigidbody towards a target position at a given speed.
	/// Using physics-based movement ensures collisions are considered.
	/// </summary>
	public void MoveRigidbodyTowards(Rigidbody rigidbody, Vector3 targetPosition, float speed, float deltaTime)
	{
		var direction = (targetPosition - rigidbody.position).normalized;
		var move = direction * speed * deltaTime;
		if ((rigidbody.position - targetPosition).sqrMagnitude <= move.sqrMagnitude)
		{
			rigidbody.MovePosition(targetPosition);
		}
		else
		{
			rigidbody.MovePosition(rigidbody.position + move);
		}
	}

	private void OnFrameUpdate(float deltaTime)
	{
		_transformHandler.Update(deltaTime);
	}

	private void OnPhysicUpdate(float deltaTime)
	{
		_physicsPositionHandler.Update(deltaTime);
		_physicsVelocityHandler.Update(deltaTime);
	}
}
}