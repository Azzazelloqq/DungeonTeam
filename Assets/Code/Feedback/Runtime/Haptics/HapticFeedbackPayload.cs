using System;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Haptics
{
    [Serializable]
    public sealed class HapticFeedbackPayload : FeedbackPayload
    {
        [SerializeField, Min(0.01f)]
        private float _duration = 0.1f;

        [SerializeField]
        private AnimationCurve _lowFrequency = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField]
        private AnimationCurve _highFrequency = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        [SerializeField, Range(0f, 1f)]
        private float _intensity = 1f;

        [SerializeField, Min(1)]
        private int _maximumConcurrentImpulses = 1;

        [SerializeField, Min(0f)]
        private float _minimumRetriggerInterval;

        [SerializeField]
        private int _priority;

        public HapticFeedbackPayload()
        {
        }

        public HapticFeedbackPayload(
            float duration,
            AnimationCurve lowFrequency,
            AnimationCurve highFrequency,
            float intensity = 1f,
            int maximumConcurrentImpulses = 1,
            float minimumRetriggerInterval = 0f,
            int priority = 0)
        {
            _duration = duration;
            _lowFrequency = lowFrequency ?? throw new ArgumentNullException(nameof(lowFrequency));
            _highFrequency = highFrequency ?? throw new ArgumentNullException(nameof(highFrequency));
            _intensity = intensity;
            _maximumConcurrentImpulses = maximumConcurrentImpulses;
            _minimumRetriggerInterval = minimumRetriggerInterval;
            _priority = priority;
        }

        public float Duration => _duration;
        public AnimationCurve LowFrequency => _lowFrequency;
        public AnimationCurve HighFrequency => _highFrequency;
        public float Intensity => _intensity;
        public int MaximumConcurrentImpulses => _maximumConcurrentImpulses;
        public float MinimumRetriggerInterval => _minimumRetriggerInterval;
        public int Priority => _priority;

        public override void Validate()
        {
            if (_duration <= 0f)
            {
                throw new InvalidOperationException(
                    "Haptic feedback duration must be positive.");
            }

            if (_lowFrequency == null || _highFrequency == null)
            {
                throw new InvalidOperationException(
                    "Haptic feedback requires low and high frequency curves.");
            }

            if (_intensity < 0f || _intensity > 1f)
            {
                throw new InvalidOperationException(
                    "Haptic feedback intensity must be between zero and one.");
            }

            if (_maximumConcurrentImpulses <= 0)
            {
                throw new InvalidOperationException(
                    "Haptic feedback maximum concurrent impulses must be positive.");
            }

            if (_minimumRetriggerInterval < 0f)
            {
                throw new InvalidOperationException(
                    "Haptic feedback retrigger interval cannot be negative.");
            }
        }
    }
}
