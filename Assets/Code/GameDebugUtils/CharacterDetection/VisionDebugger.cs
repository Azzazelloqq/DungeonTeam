using UnityEditor;
using UnityEngine;

namespace Code.GameDebugUtils.CharacterDetection
{
#if UNITY_EDITOR
public class VisionDebugger : MonoBehaviour
{
	private float _viewAngel;
	private float _viewDistance;
	private Color _visionColor;
	private Vector3 _observerPosition;
	private Vector3 _visionDirection;

	public void Initialize(float viewAngel, float viewDistance, Color visionColor)
	{
		_viewAngel = viewAngel;
		_viewDistance = viewDistance;
		_visionColor = visionColor;
	}

	public void UpdateDirection(Vector3 observerPosition, Vector3 visionDirection)
	{
		_observerPosition = observerPosition;
		_visionDirection = visionDirection;
	}

	private void OnDrawGizmos()
	{
		DrawVisionGizmos();
	}

	private void DrawVisionGizmos()
	{
		// Draw the vision radius
		Gizmos.color = _visionColor;
		Gizmos.DrawWireSphere(transform.position + _observerPosition, 0.2f);

		// Convert angle to radians and half of the angle
		var halfAngle = _viewAngel * 0.5f * Mathf.Deg2Rad;

		// We'll use a circle or arc to illustrate the vision range
		var startDirection = Quaternion.Euler(0, -_viewAngel * 0.5f, 0) * _visionDirection;
		var center = transform.position + _observerPosition;


		Handles.color = _visionColor;
		Handles.DrawWireArc(center, Vector3.up, startDirection, _viewAngel, _viewDistance);

		// Option B: Draw lines to represent the edges of the vision cone
		var leftBoundary = Quaternion.Euler(0, -_viewAngel * 0.5f, 0) * _visionDirection * _viewDistance;
		var rightBoundary = Quaternion.Euler(0, _viewAngel * 0.5f, 0) * _visionDirection * _viewDistance;

		// Draw lines from center to the boundaries
		Gizmos.DrawLine(center, center + leftBoundary);
		Gizmos.DrawLine(center, center + rightBoundary);


		const int segments = 30;
		var deltaAngle = _viewAngel / segments;
		var previousPoint = center + leftBoundary;
		for (var i = 1; i <= segments; i++)
		{
			var currentAngle = -_viewAngel * 0.5f + deltaAngle * i;
			var segmentDirection = Quaternion.Euler(0, currentAngle, 0) * _visionDirection * _viewDistance;
			var currentPoint = center + segmentDirection;

			Gizmos.DrawLine(previousPoint, currentPoint);
			previousPoint = currentPoint;
		}
	}
}
#endif
}