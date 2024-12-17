using System;
using Code.Utils.TransformUtils;
using UnityEngine;

namespace Code.MovementService
{
public interface IMovementService : IDisposable
{
	/// <summary>
	/// Start a transform-based movement task.
	/// </summary>
	public void StartMoveTowardsTarget(
		Transform transform,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null);

	/// <summary>
	/// Start a physics-based movement task using MovePosition.
	/// </summary>
	public void StartMoveRigidbodyTowardsTarget(
		Rigidbody rigidbody,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null);

	/// <summary>
	/// Start a physics-based movement task using velocity.
	/// </summary>
	public void StartMoveRigidbodyWithVelocity(
		Rigidbody rigidbody,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget = null);

	/// <summary>
	/// Moves a transform towards a target position at a given speed.
	/// This does not use Unity's physics and simply changes the Transform's position.
	/// Typically called in Update().
	/// </summary>
	public void MoveTowards(Transform transform, Vector3 targetPosition, float speed, float deltaTime);

	/// <summary>
	/// Moves a transform in a given direction at a specified speed.
	/// The direction should be a normalized vector.
	/// </summary>
	public void MoveInDirection(Transform transform, Vector3 direction, float speed, float deltaTime);

	/// <summary>
	/// Linearly interpolates the position between current and target, controlled by t.
	/// Useful for smooth transitions.
	/// </summary>
	public Vector3 LerpPosition(Vector3 currentPosition, Vector3 targetPosition, float t);

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
		float deltaTime);

	/// <summary>
	/// Moves a rigidbody by applying a velocity for a given time step.
	/// Uses Rigidbody.MovePosition for physics-based movement.
	/// </summary>
	public void MoveWithVelocity(Rigidbody rigidbody, Vector3 velocity, float deltaTime);

	/// <summary>
	/// Moves a rigidbody towards a target position at a given speed.
	/// Using physics-based movement ensures collisions are considered.
	/// </summary>
	public void MoveRigidbodyTowards(Rigidbody rigidbody, Vector3 targetPosition, float speed, float deltaTime);
}
}