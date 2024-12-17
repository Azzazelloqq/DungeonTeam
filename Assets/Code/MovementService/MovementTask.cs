using System;
using Code.Utils.TransformUtils;
using UnityEngine;

namespace Code.MovementService
{
/// <summary>
/// Represents a single movement task with all necessary data to perform the movement each frame.
/// </summary>
public struct MovementTask
{
	public Transform Transform { get; }
	public ReadOnlyTransform Target { get; }
	public float Speed { get; }
	public float MinDistance { get; }
	public Action OnReachedTarget { get; }

	public MovementTask(
		Transform transform,
		ReadOnlyTransform target,
		float speed,
		float minDistance,
		Action onReachedTarget)
	{
		Transform = transform;
		Target = target;
		Speed = speed;
		MinDistance = minDistance;
		OnReachedTarget = onReachedTarget;
	}
}
}