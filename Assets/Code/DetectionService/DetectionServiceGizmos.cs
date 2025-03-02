#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Code.DetectionService
{
[ExecuteAlways]
public class DetectionServiceGizmos : MonoBehaviour
{
	private IDetectionService _detectionService;

	[Header("Gizmos Settings")]
	[SerializeField]
	private Color _gridColor = Color.yellow;

	[SerializeField]
	private bool _drawCellContent = true;

	public void Initialize(IDetectionService detectionService)
	{
		_detectionService = detectionService;
	}

	private void OnDrawGizmos()
	{
		if (_detectionService == null)
		{
			return;
		}

		DrawGridGizmos();
	}

	/// <summary>
	/// Draws the grid cells from the DetectionService.
	/// </summary>
	private void DrawGridGizmos()
	{
		// Get access to the underlying private fields using reflection or by exposing them.
		// For this example, let's assume we have public methods or properties to retrieve the data.

		var cellSize = _detectionService.GetCellSize();
		var grid = _detectionService.GetGrid();

		if (grid == null)
		{
			return;
		}

		Gizmos.color = _gridColor;

		// Iterate over each cell in the grid
		foreach (var kvp in grid)
		{
			var cellCoords = kvp.Key;
			var objectsInCell = kvp.Value;

			// Calculate the world position of the cell
			// The "bottom-left" corner of the cell:
			var cellWorldPos = CellCoordsToWorld(cellCoords, cellSize);

			// Draw the cell as a wireframe square
			DrawCell(cellWorldPos, cellSize);

			// Optionally draw the objects inside this cell
			if (_drawCellContent && objectsInCell != null)
			{
				DrawCellContent(objectsInCell);
			}
		}
	}

	/// <summary>
	/// Converts cell coordinates to a world position for visualization.
	/// Note: This assumes y=0 is the ground and cells are oriented on the XZ plane.
	/// </summary>
	/// <param name="cellCoords">The integer grid coordinates.</param>
	/// <param name="cellSize">The size of the cell in world units.</param>
	/// <returns>The world-space position of the bottom-left corner of the cell.</returns>
	private Vector3 CellCoordsToWorld(Vector2Int cellCoords, float cellSize)
	{
		var worldX = cellCoords.x * cellSize;
		var worldZ = cellCoords.y * cellSize;
		return new Vector3(worldX, 0f, worldZ);
	}

	/// <summary>
	/// Draws a wire square representing the cell.
	/// </summary>
	/// <param name="bottomLeftPos">Bottom-left corner of the cell in world space.</param>
	/// <param name="cellSize">The cell size.</param>
	private void DrawCell(Vector3 bottomLeftPos, float cellSize)
	{
		var bottomRightPos = bottomLeftPos + new Vector3(cellSize, 0f, 0f);
		var topLeftPos = bottomLeftPos + new Vector3(0f, 0f, cellSize);
		var topRightPos = bottomLeftPos + new Vector3(cellSize, 0f, cellSize);

		// Draw lines for the four edges of the cell
		Gizmos.DrawLine(bottomLeftPos, bottomRightPos);
		Gizmos.DrawLine(bottomRightPos, topRightPos);
		Gizmos.DrawLine(topRightPos, topLeftPos);
		Gizmos.DrawLine(topLeftPos, bottomLeftPos);
	}

	/// <summary>
	/// Draws the detectable objects within the cell for debugging.
	/// </summary>
	/// <param name="objectsInCell">List of detectables in this cell.</param>
	private void DrawCellContent(List<IDetectable> objectsInCell)
	{
		foreach (var obj in objectsInCell)
		{
			if (obj == null || obj.IsDead)
			{
				continue;
			}

			// You can draw a small sphere at each object's position
			Gizmos.DrawSphere(obj.Position, 0.2f);
		}
	}
}
}
#endif