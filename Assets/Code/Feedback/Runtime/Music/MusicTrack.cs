using System;
using UnityEngine;
using UnityEngine.Audio;

namespace DungeonTeam.Feedback.Runtime.Music
{
    [Serializable]
    public sealed class MusicTrack
    {
        [SerializeField]
        private AudioClip _clip;

        [SerializeField]
        private AudioMixerGroup _output;

        [SerializeField, Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField]
        private bool _loop = true;

        public MusicTrack(AudioClip clip, float volume = 1f, bool loop = true)
        {
            _clip = clip != null ? clip : throw new ArgumentNullException(nameof(clip));
            _volume = volume;
            _loop = loop;
        }

        public AudioClip Clip => _clip;
        public AudioMixerGroup Output => _output;
        public float Volume => _volume;
        public bool Loop => _loop;

        public void Validate()
        {
            if (_clip == null)
            {
                throw new InvalidOperationException("Music track requires an audio clip.");
            }

            if (_volume < 0f || _volume > 1f)
            {
                throw new InvalidOperationException(
                    "Music track volume must be between zero and one.");
            }
        }
    }
}
