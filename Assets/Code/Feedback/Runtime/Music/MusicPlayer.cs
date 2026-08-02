using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Feedback.Runtime.Audio;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Music
{
    public sealed class MusicPlayer : IMusicPlayer
    {
        private readonly GameObject _root;
        private readonly AudioSource _source;
        private readonly Dictionary<MusicTrack, int> _preparationCounts = new();
        private MusicTrack _currentTrack;
        private bool _isDisposed;

        public MusicPlayer()
        {
            _root = new GameObject("FeedbackMusic");
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            _source = _root.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public async UniTask PrepareAsync(MusicTrack track, CancellationToken token)
        {
            RequireNotDisposed();
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            track.Validate();
            if (_preparationCounts.TryGetValue(track, out var count))
            {
                _preparationCounts[track] = checked(count + 1);
                return;
            }

            await AudioClipPreparation.PrepareAsync(new[] { track.Clip }, token);
            _preparationCounts.Add(track, 1);
        }

        public void Play(MusicTrack track)
        {
            RequireNotDisposed();
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (!_preparationCounts.ContainsKey(track))
            {
                throw new InvalidOperationException(
                    "Music track must be prepared before playback.");
            }

            if (ReferenceEquals(_currentTrack, track) && _source.isPlaying)
            {
                return;
            }

            _source.Stop();
            _source.clip = track.Clip;
            _source.outputAudioMixerGroup = track.Output;
            _source.volume = track.Volume;
            _source.loop = track.Loop;
            _currentTrack = track;
            _source.Play();
        }

        public void Stop()
        {
            RequireNotDisposed();
            _source.Stop();
            _source.clip = null;
            _currentTrack = null;
        }

        public void Release(MusicTrack track)
        {
            RequireNotDisposed();
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            if (!_preparationCounts.TryGetValue(track, out var count))
            {
                throw new InvalidOperationException(
                    "Music track cannot be released because it is not prepared.");
            }

            if (count > 1)
            {
                _preparationCounts[track] = count - 1;
                return;
            }

            if (ReferenceEquals(_currentTrack, track))
            {
                Stop();
            }

            _preparationCounts.Remove(track);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Stop();
            _isDisposed = true;
            _preparationCounts.Clear();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_root);
            }
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(MusicPlayer));
            }
        }
    }
}
