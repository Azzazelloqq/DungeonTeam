using System;
using UnityEngine;

namespace Code.ModelStructs
{
/// <summary>
/// A custom Vector3 structure to represent 3D vectors or points without a dependency on UnityEngine.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
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
	public Vector3(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Initializes a new instance of the Vector3 struct based on a UnityEngine.Vector3 object.
	/// </summary>
	/// <param name="unityVector">The UnityEngine.Vector3 instance to copy values from.</param>
	public Vector3(UnityEngine.Vector3 unityVector)
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
    public Vector3 Normalized => this / Magnitude;

	/// <summary>
	/// Represents a vector with all components set to zero.
	/// </summary>
    public static Vector3 Zero => new Vector3(0, 0, 0);
	
	/// <summary>
	/// Represents a vector with all components set to one.
	/// </summary>
    public static Vector3 One => new Vector3(1, 1, 1);
	
	/// <summary>
	/// Represents a vector pointing upwards.
	/// </summary>
    public static Vector3 Up => new Vector3(0, 1, 0);
	
	/// <summary>
	/// Represents a vector pointing downwards.
	/// </summary>
    public static Vector3 Down => new Vector3(0, -1, 0);
	
	/// <summary>
	/// Represents a vector pointing to the left.
	/// </summary>
    public static Vector3 Left => new Vector3(-1, 0, 0);
	
	/// <summary>
	/// Represents a vector pointing to the right.
	/// </summary>
    public static Vector3 Right => new Vector3(1, 0, 0);
	
	/// <summary>
	/// Represents a vector pointing forward.
	/// </summary>
    public static Vector3 Forward => new Vector3(0, 0, 1);
	
	/// <summary>
	/// Represents a vector pointing backward.
	/// </summary>
    public static Vector3 Back => new Vector3(0, 0, -1);

    public static Vector3 operator +(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public static Vector3 operator -(Vector3 a, Vector3 b)
	{
		return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	public static Vector3 operator *(Vector3 a, float d)
	{
		return new Vector3(a.X * d, a.Y * d, a.Z * d);
	}

	public static Vector3 operator /(Vector3 a, float d)
	{
		return new Vector3(a.X / d, a.Y / d, a.Z / d);
	}

	/// <summary>
	/// Returns the dot product of two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
    public static float Dot(Vector3 a, Vector3 b)
	{
		return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
	}

	/// <summary>
	/// Returns the cross product of two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
    public static Vector3 Cross(Vector3 a, Vector3 b)
	{
		return new Vector3(
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
	public static float Distance(Vector3 a, Vector3 b)
	{
		return (a - b).Magnitude;
	}

	/// <summary>
	/// Linearly interpolates between two vectors by a factor t.
	/// </summary>
	/// <param name="a">Starting vector.</param>
	/// <param name="b">Ending vector.</param>
	/// <param name="t">Interpolation factor, clamped between 0 and 1.</param>
	public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
	{
		return a + (b - a) * Math.Clamp(t, 0, 1);
	}

	/// <summary>
	/// Returns the angle in degrees between two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Angle(Vector3 a, Vector3 b)
	{
		return MathF.Acos(Math.Clamp(Dot(a.Normalized, b.Normalized), -1f, 1f)) * (180f / MathF.PI);
	}

	/// <summary>
	/// Returns a reflection vector given an incident direction and a surface normal.
	/// </summary>
	/// <param name="direction">Incident vector.</param>
	/// <param name="normal">Normal vector of the surface.</param>
	public static Vector3 Reflect(Vector3 direction, Vector3 normal)
	{
		return  direction - normal * (2 * Dot(direction, normal));
	}

	/// <summary>
	/// Projects a vector onto another vector.
	/// </summary>
	/// <param name="vector">The vector to project.</param>
	/// <param name="onNormal">The vector to project onto.</param>
	public static Vector3 Project(Vector3 vector, Vector3 onNormal)
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
		return obj is Vector3 vector &&
				Mathf.Approximately(X, vector.X) &&
				Mathf.Approximately(Y, vector.Y) &&
				Mathf.Approximately(Z, vector.Z);
	}

	/// <summary>
	/// Determines if two Vector3 instances are equal.
	/// </summary>
	public bool Equals(Vector3 other)
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
	public static bool operator ==(Vector3 a, Vector3 b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Determines if two Vector3 instances are not equal.
	/// </summary>
	public static bool operator !=(Vector3 a, Vector3 b)
	{
		return !a.Equals(b);
	}
}
}