using System;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime
{
    public readonly struct FeedbackContext
    {
        private FeedbackContext(bool hasWorldPosition, Vector3 worldPosition, float intensity)
        {
            if (intensity < 0f || intensity > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(intensity));
            }

            HasWorldPosition = hasWorldPosition;
            WorldPosition = worldPosition;
            Intensity = intensity;
        }

        public bool HasWorldPosition { get; }

        public Vector3 WorldPosition { get; }

        public float Intensity { get; }

        public static FeedbackContext Global(float intensity = 1f)
        {
            return new FeedbackContext(false, default, intensity);
        }

        public static FeedbackContext At(Vector3 worldPosition, float intensity = 1f)
        {
            return new FeedbackContext(true, worldPosition, intensity);
        }
    }
}
