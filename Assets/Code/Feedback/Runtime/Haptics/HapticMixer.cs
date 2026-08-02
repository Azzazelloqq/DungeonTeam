using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Haptics
{
    internal enum HapticRejectionReason
    {
        None,
        Cooldown,
        OwnerLimit,
        Capacity
    }

    internal sealed class HapticMixer
    {
        private readonly HapticImpulse[] _impulses;
        private readonly Dictionary<HapticFeedbackPayload, float> _lastStartedAt = new();
        private float _time;

        public HapticMixer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _impulses = new HapticImpulse[capacity];
        }

        public int ActiveCount { get; private set; }

        public HapticRejectionReason TryAdd(
            HapticFeedbackPayload payload,
            float contextIntensity)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (contextIntensity < 0f || contextIntensity > 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(contextIntensity));
            }

            if (_lastStartedAt.TryGetValue(payload, out var lastStartedAt) &&
                _time - lastStartedAt < payload.MinimumRetriggerInterval)
            {
                return HapticRejectionReason.Cooldown;
            }

            var ownedCount = 0;
            var freeSlot = -1;
            for (var index = 0; index < _impulses.Length; index++)
            {
                ref var impulse = ref _impulses[index];
                if (!impulse.IsActive)
                {
                    freeSlot = freeSlot < 0 ? index : freeSlot;
                    continue;
                }

                if (ReferenceEquals(impulse.Payload, payload))
                {
                    ownedCount++;
                }
            }

            if (ownedCount >= payload.MaximumConcurrentImpulses)
            {
                return HapticRejectionReason.OwnerLimit;
            }

            if (freeSlot < 0)
            {
                freeSlot = FindVictim(payload.Priority);
                if (freeSlot < 0)
                {
                    return HapticRejectionReason.Capacity;
                }
            }
            else
            {
                ActiveCount++;
            }

            _impulses[freeSlot] = new HapticImpulse(
                payload,
                contextIntensity,
                _time);
            _lastStartedAt[payload] = _time;
            return HapticRejectionReason.None;
        }

        public void Advance(float deltaTime, out float lowFrequency, out float highFrequency)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            _time += deltaTime;
            lowFrequency = 0f;
            highFrequency = 0f;
            for (var index = 0; index < _impulses.Length; index++)
            {
                ref var impulse = ref _impulses[index];
                if (!impulse.IsActive)
                {
                    continue;
                }

                var elapsed = _time - impulse.StartedAt;
                if (elapsed >= impulse.Payload.Duration)
                {
                    impulse = default;
                    ActiveCount--;
                    continue;
                }

                var normalizedTime = Mathf.Clamp01(elapsed / impulse.Payload.Duration);
                var intensity = impulse.Payload.Intensity * impulse.ContextIntensity;
                lowFrequency = Mathf.Max(
                    lowFrequency,
                    Mathf.Clamp01(impulse.Payload.LowFrequency.Evaluate(normalizedTime)) *
                    intensity);
                highFrequency = Mathf.Max(
                    highFrequency,
                    Mathf.Clamp01(impulse.Payload.HighFrequency.Evaluate(normalizedTime)) *
                    intensity);
            }
        }

        public void Stop(HapticFeedbackPayload payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            for (var index = 0; index < _impulses.Length; index++)
            {
                if (!ReferenceEquals(_impulses[index].Payload, payload))
                {
                    continue;
                }

                _impulses[index] = default;
                ActiveCount--;
            }
        }

        public void Forget(HapticFeedbackPayload payload)
        {
            Stop(payload);
            _lastStartedAt.Remove(payload);
        }

        public void Clear()
        {
            Array.Clear(_impulses, 0, _impulses.Length);
            _lastStartedAt.Clear();
            ActiveCount = 0;
        }

        private int FindVictim(int requestedPriority)
        {
            var victim = -1;
            for (var index = 0; index < _impulses.Length; index++)
            {
                ref var impulse = ref _impulses[index];
                if (!impulse.IsActive || impulse.Payload.Priority >= requestedPriority)
                {
                    continue;
                }

                if (victim < 0 ||
                    impulse.Payload.Priority < _impulses[victim].Payload.Priority ||
                    impulse.Payload.Priority == _impulses[victim].Payload.Priority &&
                    impulse.StartedAt < _impulses[victim].StartedAt)
                {
                    victim = index;
                }
            }

            return victim;
        }

        private readonly struct HapticImpulse
        {
            public HapticImpulse(
                HapticFeedbackPayload payload,
                float contextIntensity,
                float startedAt)
            {
                Payload = payload;
                ContextIntensity = contextIntensity;
                StartedAt = startedAt;
            }

            public HapticFeedbackPayload Payload { get; }
            public float ContextIntensity { get; }
            public float StartedAt { get; }
            public bool IsActive => Payload != null;
        }
    }
}
