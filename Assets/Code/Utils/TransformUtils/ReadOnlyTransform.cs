using System;
using UnityEngine;

namespace Code.Utils.TransformUtils
{
public readonly struct ReadOnlyTransform : IEquatable<ReadOnlyTransform>
{
	private readonly Transform _transform;

	public Vector3 Position => _transform.position;
	public Quaternion Rotation => _transform.rotation;
	public Vector3 Scale => _transform.lossyScale;

	public Vector3 LocalPosition => _transform.localPosition;
	public Quaternion LocalRotation => _transform.localRotation;
	public Vector3 LocalScale => _transform.localScale;

	public Vector3 Forward => _transform.forward;
	public Vector3 Right => _transform.right;
	public Vector3 Up => _transform.up;

	public Matrix4x4 WorldToLocalMatrix => _transform.worldToLocalMatrix;
	public Matrix4x4 LocalToWorldMatrix => _transform.localToWorldMatrix;

	public int ChildCount => _transform.childCount;
	public bool TransformIsNullOrDestroyed => _transform == null;

	public ReadOnlyTransform(Transform transform)
	{
		_transform = transform ?? throw new ArgumentNullException(nameof(transform));
	}

	public ReadOnlyTransform GetChild(int index)
	{
		if (index < 0 || index >= _transform.childCount)
		{
			throw new ArgumentOutOfRangeException(nameof(index), "Invalid child index.");
		}

		return new ReadOnlyTransform(_transform.GetChild(index));
	}

	public override bool Equals(object obj)
	{
		if (obj is ReadOnlyTransform other)
		{
			return _transform == other._transform;
		}

		return false;
	}

	public override int GetHashCode()
	{
		return _transform != null ? _transform.GetHashCode() : 0;
	}

	public static bool operator ==(ReadOnlyTransform a, ReadOnlyTransform b)
	{
		return a._transform == b._transform;
	}

	public static bool operator !=(ReadOnlyTransform a, ReadOnlyTransform b)
	{
		return !(a == b);
	}

	public override string ToString()
	{
		return _transform != null
			? $"ReadOnlyTransform: Position = {_transform.position}, Rotation = {_transform.rotation}, Scale = {_transform.lossyScale}"
			: "ReadOnlyTransform: null";
	}

	public bool Equals(ReadOnlyTransform other)
	{
		return Equals(_transform, other._transform);
	}
}
}