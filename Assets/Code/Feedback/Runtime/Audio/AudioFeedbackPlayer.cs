using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Audio
{
    public sealed class AudioFeedbackPlayer : IFeedbackPlayer
    {
        private readonly GameObject _root;
        private readonly AudioSource[] _sources;
        private readonly VoiceAllocator _allocator;
        private readonly Dictionary<AudioFeedbackPayload, int> _preparationCounts = new();
        private readonly Dictionary<AudioFeedbackPayload, int> _lastClipIndices = new();

        private int _cooldownRejections;
        private int _ownerLimitRejections;
        private int _capacityRejections;
        private int _replacedVoices;
        private bool _isDisposed;

        public AudioFeedbackPlayer(int voiceLimit)
        {
            if (voiceLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(voiceLimit));
            }

            _root = new GameObject("FeedbackAudio");
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            _sources = new AudioSource[voiceLimit];
            _allocator = new VoiceAllocator(voiceLimit);

            for (var index = 0; index < voiceLimit; index++)
            {
                var voiceObject = new GameObject($"SfxVoice_{index:00}");
                voiceObject.transform.SetParent(_root.transform, false);
                var source = voiceObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                _sources[index] = source;
            }
        }

        public AudioFeedbackMetrics Metrics
        {
            get
            {
                RequireNotDisposed();
                ReleaseCompletedVoices();
                return new AudioFeedbackMetrics(
                    _allocator.ActiveCount,
                    _cooldownRejections,
                    _ownerLimitRejections,
                    _capacityRejections,
                    _replacedVoices);
            }
        }

        public async UniTask PrepareAsync(FeedbackCue cue, CancellationToken token)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var preparedInCall = new List<AudioFeedbackPayload>();
            try
            {
                var payloads = cue.Payloads;
                for (var index = 0; index < payloads.Count; index++)
                {
                    if (payloads[index] is not AudioFeedbackPayload payload)
                    {
                        continue;
                    }

                    payload.Validate();
                    if (_preparationCounts.TryGetValue(payload, out var count))
                    {
                        _preparationCounts[payload] = checked(count + 1);
                        preparedInCall.Add(payload);
                        continue;
                    }

                    await AudioClipPreparation.PrepareAsync(payload.Clips, token);
                    _preparationCounts.Add(payload, 1);
                    preparedInCall.Add(payload);
                }
            }
            catch
            {
                for (var index = preparedInCall.Count - 1; index >= 0; index--)
                {
                    ReleasePayload(preparedInCall[index]);
                }

                throw;
            }
        }

        public void Play(FeedbackCue cue, in FeedbackContext context)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (context.Intensity <= 0f)
            {
                return;
            }

            ReleaseCompletedVoices();
            var payloads = cue.Payloads;
            for (var index = 0; index < payloads.Count; index++)
            {
                if (payloads[index] is AudioFeedbackPayload payload)
                {
                    Play(payload, context);
                }
            }
        }

        public void Stop(FeedbackCue cue)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var payloads = cue.Payloads;
            for (var payloadIndex = 0; payloadIndex < payloads.Count; payloadIndex++)
            {
                if (payloads[payloadIndex] is not AudioFeedbackPayload payload)
                {
                    continue;
                }

                for (var voiceIndex = 0; voiceIndex < _sources.Length; voiceIndex++)
                {
                    if (!_allocator.IsOwnedBy(voiceIndex, payload))
                    {
                        continue;
                    }

                    _sources[voiceIndex].Stop();
                    _allocator.Release(voiceIndex);
                }
            }
        }

        public void Release(FeedbackCue cue)
        {
            if (_isDisposed || cue == null)
            {
                return;
            }

            Stop(cue);
            var payloads = cue.Payloads;
            for (var index = 0; index < payloads.Count; index++)
            {
                if (payloads[index] is AudioFeedbackPayload payload)
                {
                    ReleasePayload(payload);
                }
            }
        }

        public void StopAll()
        {
            RequireNotDisposed();
            for (var index = 0; index < _sources.Length; index++)
            {
                _sources[index].Stop();
            }

            _allocator.Clear();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            StopAll();
            _isDisposed = true;
            _preparationCounts.Clear();
            _lastClipIndices.Clear();

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        private void Play(AudioFeedbackPayload payload, in FeedbackContext context)
        {
            if (!_preparationCounts.ContainsKey(payload))
            {
                throw new InvalidOperationException(
                    "Audio feedback payload must be prepared before playback.");
            }

            if (payload.SpatialBlend > 0f && !context.HasWorldPosition)
            {
                throw new InvalidOperationException(
                    "Spatial audio feedback requires a world position.");
            }

            var allocation = _allocator.TryAcquire(
                payload,
                payload.MaximumConcurrentVoices,
                payload.Priority,
                payload.MinimumRetriggerInterval,
                AudioSettings.dspTime);
            if (!allocation.Succeeded)
            {
                RecordRejection(allocation.RejectionReason);
                return;
            }

            if (allocation.Replaced)
            {
                _replacedVoices++;
            }

            var source = _sources[allocation.SlotIndex];
            source.Stop();
            source.clip = SelectClip(payload);
            source.outputAudioMixerGroup = payload.Output;
            source.volume = payload.Volume * context.Intensity;
            source.pitch = UnityEngine.Random.Range(
                payload.MinimumPitch,
                payload.MaximumPitch);
            source.spatialBlend = payload.SpatialBlend;
            source.minDistance = payload.MinimumDistance;
            source.maxDistance = payload.MaximumDistance;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.transform.position = context.HasWorldPosition
                ? context.WorldPosition
                : _root.transform.position;
            source.Play();
        }

        private AudioClip SelectClip(AudioFeedbackPayload payload)
        {
            var clips = payload.Clips;
            if (clips.Length == 1)
            {
                _lastClipIndices[payload] = 0;
                return clips[0];
            }

            if (!_lastClipIndices.TryGetValue(payload, out var previousIndex))
            {
                var firstIndex = UnityEngine.Random.Range(0, clips.Length);
                _lastClipIndices[payload] = firstIndex;
                return clips[firstIndex];
            }

            var randomIndex = UnityEngine.Random.Range(0, clips.Length - 1);
            if (randomIndex >= previousIndex)
            {
                randomIndex++;
            }

            _lastClipIndices[payload] = randomIndex;
            return clips[randomIndex];
        }

        private void ReleaseCompletedVoices()
        {
            for (var index = 0; index < _sources.Length; index++)
            {
                if (!_sources[index].isPlaying)
                {
                    _allocator.Release(index);
                }
            }
        }

        private void ReleasePayload(AudioFeedbackPayload payload)
        {
            if (!_preparationCounts.TryGetValue(payload, out var count))
            {
                return;
            }

            if (count > 1)
            {
                _preparationCounts[payload] = count - 1;
                return;
            }

            _preparationCounts.Remove(payload);
            _lastClipIndices.Remove(payload);
            _allocator.ForgetOwner(payload);
        }

        private void RecordRejection(VoiceRejectionReason reason)
        {
            switch (reason)
            {
                case VoiceRejectionReason.Cooldown:
                    _cooldownRejections++;
                    break;
                case VoiceRejectionReason.OwnerLimit:
                    _ownerLimitRejections++;
                    break;
                case VoiceRejectionReason.Capacity:
                    _capacityRejections++;
                    break;
            }
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AudioFeedbackPlayer));
            }
        }
    }
}
