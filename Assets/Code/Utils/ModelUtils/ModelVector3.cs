using System;
using UnityEngine;

namespace Code.Utils.ModelUtils
{
/// <summary>
/// A custom Vector3 structure to represent 3D vectors or points without a dependency on UnityEngine.
/// </summary>
public readonly struct ModelVector3 : IEquatable<ModelVector3>
{
	/// <summary>
	/// X-coordinate of the vector.
	/// </summary>
	public float X { get; }

	/// <summary>
	/// Y-coordinate of the vector.
	/// </summary>
	public float Y { get; }

	/// <summary>
	/// Z-coordinate of the vector.
	/// </summary>
	public float Z { get; }

	/// <summary>
	/// Initializes a new instance of the Vector3 struct with specified x, y, and z values.
	/// </summary>
	/// <param name="x">X-coordinate.</param>
	/// <param name="y">Y-coordinate.</param>
	/// <param name="z">Z-coordinate.</param>
	public ModelVector3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Initializes a new instance of the Vector3 struct based on a UnityEngine.Vector3 object.
	/// </summary>
	/// <param name="unityVector">The UnityEngine.Vector3 instance to copy values from.</param>
	public ModelVector3(Vector3 unityVector)
	{
		X = unityVector.x;
		Y = unityVector.y;
		Z = unityVector.z;
	}

	/// <summary>
	/// Returns the magnitude (length) of the vector.
	/// </summary>
	public float Magnitude => MathF.Sqrt(X * X + Y * Y + Z * Z);

	/// <summary>
	/// Returns the squared magnitude of the vector, useful for performance optimization.
	/// </summary>
	public float SqrMagnitude => X * X + Y * Y + Z * Z;

	/// <summary>
	/// Returns a normalized (unit) vector.
	/// </summary>
	public ModelVector3 Normalized => this / Magnitude;

	/// <summary>
	/// Represents a vector with all components set to zero.
	/// </summary>
	public static ModelVector3 Zero => new(0, 0, 0);

	/// <summary>
	/// Represents a vector with all components set to one.
	/// </summary>
	public static ModelVector3 One => new(1, 1, 1);

	/// <summary>
	/// Represents a vector pointing upwards.
	/// </summary>
	public static ModelVector3 Up => new(0, 1, 0);

	/// <summary>
	/// Represents a vector pointing downwards.
	/// </summary>
	public static ModelVector3 Down => new(0, -1, 0);

	/// <summary>
	/// Represents a vector pointing to the left.
	/// </summary>
	public static ModelVector3 Left => new(-1, 0, 0);

	/// <summary>
	/// Represents a vector pointing to the right.
	/// </summary>
	public static ModelVector3 Right => new(1, 0, 0);

	/// <summary>
	/// Represents a vector pointing forward.
	/// </summary>
	public static ModelVector3 Forward => new(0, 0, 1);

	/// <summary>
	/// Represents a vector pointing backward.
	/// </summary>
	public static ModelVector3 Back => new(0, 0, -1);

	// <summary>
	/// Adds two ModelVector3 vectors component-wise.
	/// </summary>
	/// <param name="a">The first vector.</param>
	/// <param name="b">The second vector.</param>
	/// <returns>A new vector that is the sum of the two input vectors.</returns>
	public static ModelVector3 operator +(ModelVector3 a, ModelVector3 b)
	{
		return new ModelVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	/// <summary>
	/// Adds a scalar value to each component of the ModelVector3 vector.
	/// </summary>
	/// <param name="a">The vector.</param>
	/// <param name="b">The scalar value to add.</param>
	/// <returns>A new vector with the scalar value added to each component.</returns>
	public static ModelVector3 operator -(ModelVector3 a, ModelVector3 b)
	{
		return new ModelVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	/// <summary>
	/// Subtracts one ModelVector3 vector from another component-wise.
	/// </summary>
	/// <param name="a">The first vector.</param>
	/// <param name="b">The second vector to subtract from the first vector.</param>
	/// <returns>A new vector that is the difference of the two input vectors.</returns>
	public static ModelVector3 operator *(ModelVector3 a, float d)
	{
		return new ModelVector3(a.X * d, a.Y * d, a.Z * d);
	}

	/// <summary>
	/// Multiplies each component of the ModelVector3 vector by a scalar value.
	/// </summary>
	/// <param name="a">The vector.</param>
	/// <param name="d">The scalar value to multiply by.</param>
	/// <returns>A new vector with each component multiplied by the scalar value.</returns>
	public static ModelVector3 operator /(ModelVector3 a, float d)
	{
		return new ModelVector3(a.X / d, a.Y / d, a.Z / d);
	}

	/// <summary>
	/// Adds a ModelVector3 vector and a ModelVector2 vector component-wise.
	/// </summary>
	/// <param name="a">The ModelVector3 vector.</param>
	/// <param name="b">The ModelVector2 vector.</param>
	/// <returns>A new ModelVector3 vector that is the sum of the two input vectors.</returns>
	public static ModelVector3 operator +(ModelVector3 a, ModelVector2 b)
	{
		return new ModelVector3(a.X + b.X, a.Y + b.Y, a.Z);
	}

	/// <summary>
	/// Subtracts a ModelVector2 vector from a ModelVector3 vector component-wise.
	/// </summary>
	/// <param name="a">The ModelVector3 vector.</param>
	/// <param name="b">The ModelVector2 vector to subtract.</param>
	/// <returns>A new ModelVector3 vector that is the difference of the two input vectors.</returns>
	public static ModelVector3 operator -(ModelVector3 a, ModelVector2 b)
	{
		return new ModelVector3(a.X - b.X, a.Y - b.Y, a.Z);
	}

	/// <summary>
	/// Returns the dot product of two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Dot(ModelVector3 a, ModelVector3 b)
	{
		return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
	}

	/// <summary>
	/// Returns the cross product of two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static ModelVector3 Cross(ModelVector3 a, ModelVector3 b)
	{
		return new ModelVector3(
			a.Y * b.Z - a.Z * b.Y,
			a.Z * b.X - a.X * b.Z,
			a.X * b.Y - a.Y * b.X
		);
	}

	/// <summary>
	/// Returns the distance between two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Distance(ModelVector3 a, ModelVector3 b)
	{
		return (a - b).Magnitude;
	}

	/// <summary>
	/// Linearly interpolates between two vectors by a factor t.
	/// </summary>
	/// <param name="a">Starting vector.</param>
	/// <param name="b">Ending vector.</param>
	/// <param name="t">Interpolation factor, clamped between 0 and 1.</param>
	public static ModelVector3 Lerp(ModelVector3 a, ModelVector3 b, float t)
	{
		return a + (b - a) * Math.Clamp(t, 0, 1);
	}

	/// <summary>
	/// Returns the angle in degrees between two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Angle(ModelVector3 a, ModelVector3 b)
	{
		return MathF.Acos(Math.Clamp(Dot(a.Normalized, b.Normalized), -1f, 1f)) * (180f / MathF.PI);
	}

	/// <summary>
	/// Returns a reflection vector given an incident direction and a surface normal.
	/// </summary>
	/// <param name="direction">Incident vector.</param>
	/// <param name="normal">Normal vector of the surface.</param>
	public static ModelVector3 Reflect(ModelVector3 direction, ModelVector3 normal)
	{
		return direction - normal * (2 * Dot(direction, normal));
	}

	/// <summary>
	/// Projects a vector onto another vector.
	/// </summary>
	/// <param name="vector">The vector to project.</param>
	/// <param name="onNormal">The vector to project onto.</param>
	public static ModelVector3 Project(ModelVector3 vector, ModelVector3 onNormal)
	{
		return onNormal * (Dot(vector, onNormal) / onNormal.SqrMagnitude);
	}

	/// <summary>
	/// Returns a string representation of the vector in (X, Y, Z) format.
	/// </summary>
	public override string ToString()
	{
		return $"({X}, {Y}, {Z})";
	}

	/// <summary>
	/// Determines if two Vector3 instances are equal.
	/// </summary>
	public override bool Equals(object obj)
	{
		return obj is ModelVector3 vector &&
				Mathf.Approximately(X, vector.X) &&
				Mathf.Approximately(Y, vector.Y) &&
				Mathf.Approximately(Z, vector.Z);
	}

	/// <summary>
	/// Determines if two Vector3 instances are equal.
	/// </summary>
	public bool Equals(ModelVector3 other)
	{
		return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
	}

	/// <summary>
	/// Returns the hash code for the vector.
	/// </summary>
	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y, Z);
	}

	/// <summary>
	/// Determines if two Vector3 instances are equal.
	/// </summary>
	public static bool operator ==(ModelVector3 a, ModelVector3 b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Determines if two Vector3 instances are not equal.
	/// </summary>
	public static bool operator !=(ModelVector3 a, ModelVector3 b)
	{
		return !a.Equals(b);
	}
}
}