using System;

namespace Code.ModelStructs
{
/// <summary>
/// A custom Vector2 structure to represent 2D vectors or points without a dependency on UnityEngine.
/// </summary>
public readonly struct ModelVector2 : IEquatable<ModelVector2>
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
	/// Initializes a new instance of the Vector2 struct with specified x and y values.
	/// </summary>
	/// <param name="x">X-coordinate.</param>
	/// <param name="y">Y-coordinate.</param>
	public ModelVector2(float x, float y)
	{
		X = x;
		Y = y;
	}

	/// <summary>
	/// Initializes a new instance of the Vector2 struct based on a UnityEngine.Vector2 object.
	/// </summary>
	/// <param name="unityVector">The UnityEngine.Vector2 instance to copy values from.</param>
	public ModelVector2(UnityEngine.Vector2 unityVector)
	{
		X = unityVector.x;
		Y = unityVector.y;
	}

	/// <summary>
	/// Returns the magnitude (length) of the vector.
	/// </summary>
	public float Magnitude => MathF.Sqrt(X * X + Y * Y);

	/// <summary>
	/// Returns the squared magnitude of the vector, useful for performance optimization.
	/// </summary>
	public float SqrMagnitude => X * X + Y * Y;

	/// <summary>
	/// Returns a normalized (unit) vector.
	/// </summary>
	public ModelVector2 Normalized => this / Magnitude;

	/// <summary>
	/// Represents a vector with all components set to zero.
	/// </summary>
	public static ModelVector2 Zero => new(0f, 0f);

	/// <summary>
	/// Represents a vector with all components set to one.
	/// </summary>
	public static ModelVector2 One => new(1f, 1f);

	/// <summary>
	/// Represents a vector pointing upwards.
	/// </summary>
	public static ModelVector2 Up => new(0f, 1f);

	/// <summary>
	/// Represents a vector pointing downwards.
	/// </summary>
	public static ModelVector2 Down => new(0f, -1f);

	/// <summary>
	/// Represents a vector pointing to the left.
	/// </summary>
	public static ModelVector2 Left => new(-1f, 0f);

	/// <summary>
	/// Represents a vector pointing to the right.
	/// </summary>
	public static ModelVector2 Right => new(1f, 0f);

	/// <summary>
	/// Adds two vectors component-wise.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static ModelVector2 operator +(ModelVector2 a, ModelVector2 b)
	{
		return new ModelVector2(a.X + b.X, a.Y + b.Y);
	}
	
	/// <summary>
	/// Subtracts two vectors component-wise.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static ModelVector2 operator -(ModelVector2 a, ModelVector2 b)
	{
		return new ModelVector2(a.X - b.X, a.Y - b.Y);
	}

	/// <summary>
	/// Negates a vector.
	/// </summary>
	/// <param name="a">The vector to negate.</param>
	public static ModelVector2 operator -(ModelVector2 a)
	{
		return new ModelVector2(-a.X, -a.Y);
	}

	/// <summary>
	/// Multiplies a vector by a scalar.
	/// </summary>
	/// <param name="a">The vector.</param>
	/// <param name="d">The scalar value.</param>
	public static ModelVector2 operator *(ModelVector2 a, float d)
	{
		return new ModelVector2(a.X * d, a.Y * d);
	}

	/// <summary>
	/// Multiplies a scalar by a vector.
	/// </summary>
	/// <param name="d">The scalar value.</param>
	/// <param name="a">The vector.</param>
	public static ModelVector2 operator *(float d, ModelVector2 a)
	{
		return new ModelVector2(a.X * d, a.Y * d);
	}

	/// <summary>
	/// Divides a vector by a scalar.
	/// </summary>
	/// <param name="a">The vector.</param>
	/// <param name="d">The scalar value.</param>
	public static ModelVector2 operator /(ModelVector2 a, float d)
	{
		return new ModelVector2(a.X / d, a.Y / d);
	}
	
	/// <summary>
	/// Adds a ModelVector2 vector and a ModelVector3 vector component-wise.
	/// </summary>
	/// <param name="a">The ModelVector2 vector.</param>
	/// <param name="b">The ModelVector3 vector.</param>
	/// <returns>A new ModelVector2 vector that is the sum of the two input vectors.</returns>
	public static ModelVector2 operator +(ModelVector2 a, ModelVector3 b)
	{
		return new ModelVector2(a.X + b.X, a.Y + b.Y);
	}

	/// <summary>
	/// Subtracts a ModelVector3 vector from a ModelVector2 vector component-wise.
	/// </summary>
	/// <param name="a">The ModelVector2 vector.</param>
	/// <param name="b">The ModelVector3 vector to subtract.</param>
	/// <returns>A new ModelVector2 vector that is the difference of the two input vectors.</returns>
	public static ModelVector2 operator -(ModelVector2 a, ModelVector3 b)
	{
		return new ModelVector2(a.X - b.X, a.Y - b.Y);
	}

	/// <summary>
	/// Returns the dot product of two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Dot(ModelVector2 a, ModelVector2 b)
	{
		return a.X * b.X + a.Y * b.Y;
	}

	/// <summary>
	/// Returns the distance between two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Distance(ModelVector2 a, ModelVector2 b)
	{
		return (a - b).Magnitude;
	}

	/// <summary>
	/// Linearly interpolates between two vectors by a factor t.
	/// </summary>
	/// <param name="a">Starting vector.</param>
	/// <param name="b">Ending vector.</param>
	/// <param name="t">Interpolation factor, clamped between 0 and 1.</param>
	public static ModelVector2 Lerp(ModelVector2 a, ModelVector2 b, float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return a + (b - a) * t;
	}

	/// <summary>
	/// Returns the angle in degrees between two vectors.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static float Angle(ModelVector2 a, ModelVector2 b)
	{
		var dot = Dot(a.Normalized, b.Normalized);
		return MathF.Acos(Math.Clamp(dot, -1f, 1f)) * (180f / MathF.PI);
	}

	/// <summary>
	/// Projects a vector onto another vector.
	/// </summary>
	/// <param name="vector">The vector to project.</param>
	/// <param name="onNormal">The vector to project onto.</param>
	public static ModelVector2 Project(ModelVector2 vector, ModelVector2 onNormal)
	{
		return onNormal * (Dot(vector, onNormal) / onNormal.SqrMagnitude);
	}

	/// <summary>
	/// Returns a string representation of the vector in (X, Y) format.
	/// </summary>
	public override string ToString()
	{
		return $"({X}, {Y})";
	}

	/// <summary>
	/// Determines whether the specified object is equal to the current vector.
	/// </summary>
	/// <param name="obj">The object to compare with.</param>
	public override bool Equals(object obj)
	{
		return obj is ModelVector2 vector && Equals(vector);
	}

	/// <summary>
	/// Determines whether the specified Vector2 is equal to the current vector.
	/// </summary>
	/// <param name="other">The vector to compare with.</param>
	public bool Equals(ModelVector2 other)
	{
		return Approximately(X, other.X) && Approximately(Y, other.Y);
	}

	/// <summary>
	/// Returns the hash code for this vector.
	/// </summary>
	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	/// <summary>
	/// Determines whether two vectors are equal.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static bool operator ==(ModelVector2 a, ModelVector2 b)
	{
		return a.Equals(b);
	}

	/// <summary>
	/// Determines whether two vectors are not equal.
	/// </summary>
	/// <param name="a">First vector.</param>
	/// <param name="b">Second vector.</param>
	public static bool operator !=(ModelVector2 a, ModelVector2 b)
	{
		return !a.Equals(b);
	}

	/// <summary>
	/// Determines whether two floating-point numbers are approximately equal.
	/// </summary>
	/// <param name="a">First value.</param>
	/// <param name="b">Second value.</param>
	/// <param name="epsilon">The maximum difference for which the numbers are considered equal.</param>
	private static bool Approximately(float a, float b, float epsilon = 1e-6f)
	{
		return Math.Abs(a - b) < epsilon;
	}
}
}