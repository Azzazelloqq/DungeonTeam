using System;
using UnityEngine;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime
{
    public sealed class AmbientNpcVignetteBinding : MonoBehaviour
    {
        [SerializeField] private string _vignetteId;
        [SerializeField] private string _firstNpcId;
        [SerializeField] private string _secondNpcId;
        [SerializeField] private Transform _firstFacingTarget;
        [SerializeField] private Transform _secondFacingTarget;
        [SerializeField, Min(0.1f)] private float _phaseDuration = 2f;

        public string VignetteId => _vignetteId;
        public string FirstNpcId => _firstNpcId;
        public string SecondNpcId => _secondNpcId;
        public Transform FirstFacingTarget => _firstFacingTarget;
        public Transform SecondFacingTarget => _secondFacingTarget;
        public float PhaseDuration => _phaseDuration;

        public void Validate(int index)
        {
            if (string.IsNullOrWhiteSpace(_vignetteId) || string.IsNullOrWhiteSpace(_firstNpcId) ||
                string.IsNullOrWhiteSpace(_secondNpcId) || _firstNpcId == _secondNpcId ||
                _firstFacingTarget == null || _secondFacingTarget == null || _phaseDuration <= 0f)
            {
                throw new InvalidOperationException($"Ambient NPC vignette at index {index} is invalid.");
            }
        }
    }
}
