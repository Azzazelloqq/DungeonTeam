using System;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime
{
    [Serializable]
    public sealed class FeedbackRuntimeSettings
    {
        [SerializeField, Min(1)]
        private int _sfxVoiceLimit = 24;

        [SerializeField, Min(1)]
        private int _hapticImpulseLimit = 8;

        public int SfxVoiceLimit => _sfxVoiceLimit;
        public int HapticImpulseLimit => _hapticImpulseLimit;

        public void Validate()
        {
            if (_sfxVoiceLimit <= 0)
            {
                throw new InvalidOperationException(
                    "Feedback SFX voice limit must be positive.");
            }

            if (_hapticImpulseLimit <= 0)
            {
                throw new InvalidOperationException(
                    "Feedback haptic impulse limit must be positive.");
            }
        }
    }
}
