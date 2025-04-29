using System.Collections.Generic;
using UnityEngine;

namespace Code.DetectionService
{
public class DetectionService : IDetectionService
{
	/// <summary>
	/// Size of each grid cell in Unity world units.
	/// </summary>
	private readonly float _cellSize;

	/// <summary>
	/// Dictionary mapping grid cell coordinates to a list of objects in that cell.
	/// </summary>
	private readonly Dictionary<Vector2Int, List<IDetectable>> _grid = new();

	private List<IDetectable> _detectedObjectsCash;

	/// <summary>
	/// Constructs a new instance of the DetectionService.
	/// </summary>
	/// <param name="cellSize">Size of each grid cell in Unity units.</param>
	public DetectionService(float cellSize)
	{
		_cellSize = cellSize;
	}

	/// <inheritdoc/>
	public void RegisterObject(IDetectable detectable)
	{
		var cellCoords = GetCellCoords(detectable.Position);
		AddObjectToCell(cellCoords, detectable);
	}

	/// <inheritdoc/>
	public void UnregisterObject(IDetectable detectable)
	{
		var cellCoords = GetCellCoords(detectable.Position);
		RemoveObjectFromCell(cellCoords, detectable);
	}

	/// <inheritdoc/>
	public void UpdateObjectPosition(IDetectable detectable, Vector3 oldPosition)
	{
		var oldCellCoords = GetCellCoords(oldPosition);
		var newCellCoords = GetCellCoords(detectable.Position);

		if (oldCellCoords == newCellCoords)
		{
			return;
		}

		RemoveObjectFromCell(oldCellCoords, detectable);
		AddObjectToCell(newCellCoords, detectable);
	}

	/// <inheritdoc/>
	public IReadOnlyList<IDetectable> DetectObjectsInView(
		Vector3 observerPosition,
		Vector3 observerForward,
		float viewAngle,
		float viewDistance,
		LayerMask obstacleLayer)
	{
		if (_detectedObjectsCash == null)
		{
			_detectedObjectsCash = new List<IDetectable>();
		}
		else
		{
			_detectedObjectsCash.Clear();
		}

		var centerCell = GetCellCoords(observerPosition);
		var cellsRange = Mathf.CeilToInt(viewDistance / _cellSize);

		for (var x = centerCell.x - cellsRange; x <= centerCell.x + cellsRange; x++)
		{
			for (var y = centerCell.y - cellsRange; y <= centerCell.y + cellsRange; y++)
			{
				var cellCoords = new Vector2Int(x, y);
				if (!_grid.TryGetValue(cellCoords, out var cellObjects))
				{
					continue;
				}

				foreach (var obj in cellObjects)
				{
					if (obj.IsDead)
					{
						continue;
					}

					var directionToObject = obj.Position - observerPosition;
					var distanceToObject = directionToObject.magnitude;

					if (!(distanceToObject <= viewDistance))
					{
						continue;
					}

					var angle = Vector3.Angle(observerForward, directionToObject);
					if (!(angle <= viewAngle / 2))
					{
						continue;
					}

					if (!Physics.Raycast(observerPosition, directionToObject.normalized,
							distanceToObject, obstacleLayer))
					{
						_detectedObjectsCash.Add(obj);
					}
				}
			}
		}

		return _detectedObjectsCash;
	}

	/// <summary>
	/// Gets the size of each grid cell in Unity world units.
	/// </summary>
	/// <returns>The size of each grid cell.</returns>
	public float GetCellSize()
	{
		return _cellSize;
	}

	/// <summary>
	/// Gets the size of each grid cell in Unity world units.
	/// </summary>
	/// <returns>The size of each grid cell.</returns>
	public IReadOnlyDictionary<Vector2Int, List<IDetectable>> GetGrid()
	{
		return _grid;
	}

	/// <summary>
	/// Calculates the grid cell coordinates based on a world position.
	/// </summary>
	/// <param name="position">World position.</param>
	/// <returns>Grid cell coordinates as Vector2Int.</returns>
	private Vector2Int GetCellCoords(Vector3 position)
	{
		var x = Mathf.FloorToInt(position.x / _cellSize);
		var y = Mathf.FloorToInt(position.z / _cellSize);

		return new Vector2Int(x, y);
	}

	/// <summary>
	/// Adds an object to a specific cell in the grid.
	/// </summary>
	/// <param name="cellCoords">Grid cell coordinates.</param>
	/// <param name="obj">The object to add.</param>
	private void AddObjectToCell(Vector2Int cellCoords, IDetectable obj)
	{
		if (!_grid.TryGetValue(cellCoords, out var cellObjects))
		{
			cellObjects = new List<IDetectable>();
			_grid[cellCoords] = cellObjects;
		}

		cellObjects.Add(obj);
	}

	/// <summary>
	/// Removes an object from a specific cell in the grid.
	/// </summary>
	/// <param name="cellCoords">Grid cell coordinates.</param>
	/// <param name="obj">The object to remove.</param>
	private void RemoveObjectFromCell(Vector2Int cellCoords, IDetectable obj)
	{
		if (!_grid.TryGetValue(cellCoords, out var cellObjects))
		{
			return;
		}

		cellObjects.Remove(obj);
		if (cellObjects.Count == 0)
		{
			_grid.Remove(cellCoords);
		}
	}
}
}