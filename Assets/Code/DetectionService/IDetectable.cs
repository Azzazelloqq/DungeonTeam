using UnityEngine;

namespace Code.DetectionService
{
/// <summary>
/// Interface for objects that can be detected by the DetectionService.
/// </summary>
public interface IDetectable
{
	/// <summary>
	/// Current position of the object in world space.
	/// </summary>
	public Vector3 Position { get; }

	/// <summary>
	/// Flag indicating whether the object is dead.
	/// </summary>
	public bool IsDead { get; }
}
}