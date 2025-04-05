using System;
using UnityEngine;

namespace Code.DungeonTeam.MoveController.Base
{
public interface IMoveController : IDisposable
{
	public event Action<Vector2> DirectionChanged; 
	public event Action MoveStarted; 
	public event Action MoveEnded; 
	
	public Vector2 Direction { get; }
}
}