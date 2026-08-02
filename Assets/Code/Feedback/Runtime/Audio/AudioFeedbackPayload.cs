using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DungeonTeam.Feedback.Runtime.Audio
{
    [Serializable]
    public sealed class AudioFeedbackPayload : FeedbackPayload
    {
        [SerializeField]
        private AudioClip[] _clips = Array.Empty<AudioClip>();

        [SerializeField]
        private AudioMixerGroup _output;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField, Range(-3f, 3f)]
        private float _minimumPitch = 1f;

        [SerializeField, Range(-3f, 3f)]
        private float _maximumPitch = 1f;

        [SerializeField, Range(0f, 1f)]
        private float _spatialBlend;

        [SerializeField, Min(0.01f)]
        private float _minimumDistance = 1f;

        [SerializeField, Min(0.01f)]
        private float _maximumDistance = 25f;

        [SerializeField, Min(1)]
        private int _maximumConcurrentVoices = 1;

        [SerializeField, Min(0f)]
        private float _minimumRetriggerInterval;

        [SerializeField]
        private int _priority;

        public AudioFeedbackPayload()
        {
        }

        public AudioFeedbackPayload(
            AudioClip[] clips,
            float volume = 1f,
            float minimumPitch = 1f,
            float maximumPitch = 1f,
            float spatialBlend = 0f,
            int maximumConcurrentVoices = 1,
            float minimumRetriggerInterval = 0f,
            int priority = 0)
        {
            _clips = clips != null
                ? (AudioClip[])clips.Clone()
                : throw new ArgumentNullException(nameof(clips));
            _volume = volume;
            _minimumPitch = minimumPitch;
            _maximumPitch = maximumPitch;
            _spatialBlend = spatialBlend;
            _maximumConcurrentVoices = maximumConcurrentVoices;
            _minimumRetriggerInterval = minimumRetriggerInterval;
            _priority = priority;
        }

        public AudioClip[] Clips => _clips;
        public AudioMixerGroup Output => _output;
        public float Volume => _volume;
        public float MinimumPitch => _minimumPitch;
        public float MaximumPitch => _maximumPitch;
        public float SpatialBlend => _spatialBlend;
        public float MinimumDistance => _minimumDistance;
        public float MaximumDistance => _maximumDistance;
        public int MaximumConcurrentVoices => _maximumConcurrentVoices;
        public float MinimumRetriggerInterval => _minimumRetriggerInterval;
        public int Priority => _priority;

        public override void Validate()
        {
            if (_clips == null || _clips.Length == 0)
            {
                throw new InvalidOperationException(
                    "Audio feedback payload requires at least one clip.");
            }

            for (var index = 0; index < _clips.Length; index++)
            {
                if (_clips[index] == null)
                {
                    throw new InvalidOperationException(
                        $"Audio feedback payload contains an empty clip at index {index}.");
                }
            }

            if (_volume < 0f || _volume > 1f)
            {
                throw new InvalidOperationException(
                    "Audio feedback volume must be between zero and one.");
            }

            if (_minimumPitch < -3f || _minimumPitch > 3f ||
                _maximumPitch < -3f || _maximumPitch > 3f ||
                _minimumPitch > _maximumPitch ||
                Mathf.Approximately(_minimumPitch, 0f) ||
                Mathf.Approximately(_maximumPitch, 0f) ||
                _minimumPitch < 0f && _maximumPitch > 0f)
            {
                throw new InvalidOperationException(
                    "Audio feedback pitch range must be ordered, non-zero and inside [-3, 3].");
            }

            if (_spatialBlend < 0f || _spatialBlend > 1f)
            {
                throw new InvalidOperationException(
                    "Audio feedback spatial blend must be between zero and one.");
            }

            if (_minimumDistance <= 0f || _maximumDistance < _minimumDistance)
            {
                throw new InvalidOperationException(
                    "Audio feedback distances must be positive and ordered.");
            }

            if (_maximumConcurrentVoices <= 0)
            {
                throw new InvalidOperationException(
                    "Audio feedback maximum concurrent voices must be positive.");
            }

            if (_minimumRetriggerInterval < 0f)
            {
                throw new InvalidOperationException(
                    "Audio feedback retrigger interval cannot be negative.");
            }
        }
    }
}
