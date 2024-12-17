using System;
using Code.Utils.TransformUtils;
using UnityEngine;

namespace Code.MovementService
{
internal class PhysicsMovementTask
{
	public Rigidbody Rigidbody { get; }
	public ReadOnlyTransform Target { get; }
	public float Speed { get; }
	public float MinDistance { get; }
	public Action OnReachedTarget { get; }

	public PhysicsMovementTask(
		Rigidbody rigidbody,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget)
	{
		Rigidbody = rigidbody;
		Target = target;
		Speed = speed;
		MinDistance = minDistance;
		OnReachedTarget = onReachedTarget;
	}
}
}