using System;
using Code.DetectionService;
using TickHandler;
using UnityEngine;

namespace Code.DungeonTeam.TeamCharacter.Components
{
public class CharacterDetectionUpdater : IDisposable
{
	private readonly IDetectionService _detectionService;
	private readonly IDetectable _detectable;
	private readonly ITickHandler _tickHandler;
	private readonly float _updateInterval;
	private readonly float _distanceThreshold;
	private readonly Func<bool> _needUpdateCondition;
	private Vector3 _detectableLastPosition;
	private float _timeSinceLastUpdate;

	public CharacterDetectionUpdater(
		IDetectionService detectionService,
		IDetectable detectable,
		ITickHandler tickHandler,
		float updateInterval,
		float distanceThreshold = 1,
		Func<bool> needUpdateCondition = null)
	{
		_detectionService = detectionService;
		_detectable = detectable;
		_tickHandler = tickHandler;
		_updateInterval = updateInterval;
		_distanceThreshold = distanceThreshold;
		_needUpdateCondition = needUpdateCondition;
		_detectableLastPosition = detectable.Position;
	}

	public void Initialize()
	{
		_detectionService.RegisterObject(_detectable);
		_tickHandler.FrameUpdate += StartUpdateDetection;
	}

	public void Dispose()
	{
		_tickHandler.FrameUpdate -= StartUpdateDetection;
		_detectionService.UnregisterObject(_detectable);
	}
	
	public void ForceUpdate()
	{
		var currentPos = _detectable.Position;
		_detectionService.UpdateObjectPosition(_detectable, _detectableLastPosition);
		_detectableLastPosition = currentPos;
		_timeSinceLastUpdate = 0f;
	}

	private void StartUpdateDetection(float deltaTime)
	{
		if (_needUpdateCondition != null && !_needUpdateCondition())
		{
			return;
		}
		
		_timeSinceLastUpdate += deltaTime;
		var timeToForceUpdate = _timeSinceLastUpdate >= _updateInterval;

		var currentPos = _detectable.Position;
		var movedDistance = Vector3.Distance(currentPos, _detectableLastPosition);

		if (timeToForceUpdate || movedDistance >= _distanceThreshold)
		{
			_detectionService.UpdateObjectPosition(_detectable, _detectableLastPosition);

			_detectableLastPosition = currentPos;
		}

		if (timeToForceUpdate)
		{
			_timeSinceLastUpdate = 0f;
		}
	}
}
}