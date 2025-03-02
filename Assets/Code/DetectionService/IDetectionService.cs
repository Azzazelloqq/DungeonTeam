using System.Collections.Generic;
using UnityEngine;

namespace Code.DetectionService
{
/// <summary>
/// Service for detecting objects in the game world using spatial partitioning (grid).
/// </summary>
public interface IDetectionService
{
    /// <summary>
    /// Registers an object with the detection service.
    /// </summary>
    /// <param name="detectable">The object implementing IDetectable.</param>
    public void RegisterObject(IDetectable detectable);
    
    /// <summary>
    /// Unregisters an object from the detection service.
    /// </summary>
    /// <param name="detectable">The object implementing IDetectable.</param>
    public void UnregisterObject(IDetectable detectable);
    
    /// <summary>
    /// Updates the object's position in the grid when it moves.
    /// </summary>
    /// <param name="detectable">The object implementing IDetectable.</param>
    /// <param name="oldPosition">The object's previous position.</param>
    public void UpdateObjectPosition(IDetectable detectable, Vector3 oldPosition);
    
    /// <summary>
    /// Detects objects within the observer's field of view.
    /// </summary>
    /// <param name="observerPosition">The observer's position.</param>
    /// <param name="observerForward">The observer's forward direction.</param>
    /// <param name="viewAngle">The observer's view angle in degrees.</param>
    /// <param name="viewDistance">The observer's view distance.</param>
    /// <param name="obstacleLayer">Layer mask for obstacles.</param>
    /// <returns>List of detected objects.</returns>
    public IReadOnlyList<IDetectable> DetectObjectsInView(Vector3 observerPosition, Vector3 observerForward, float viewAngle,
        float viewDistance, LayerMask obstacleLayer);

	/// <summary>
	/// Gets the size of each grid cell in Unity world units.
	/// </summary>
	/// <returns>The size of each grid cell.</returns>
	float GetCellSize();

	/// <summary>
	/// Gets the size of each grid cell in Unity world units.
	/// </summary>
	/// <returns>The size of each grid cell.</returns>
	IReadOnlyDictionary<Vector2Int, List<IDetectable>> GetGrid();
}
}