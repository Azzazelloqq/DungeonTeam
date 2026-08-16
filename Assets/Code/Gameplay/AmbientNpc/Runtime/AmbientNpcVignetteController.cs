using System;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime
{
    internal sealed class AmbientNpcVignetteController : IDisposable
    {
        private readonly AmbientNpcVignetteBinding _binding;
        private readonly AmbientNpcPresenterBase _first;
        private readonly AmbientNpcPresenterBase _second;
        private float _elapsed;
        private bool _firstIsSpeaking = true;
        private bool _isDisposed;

        public AmbientNpcVignetteController(
            AmbientNpcVignetteBinding binding,
            AmbientNpcPresenterBase first,
            AmbientNpcPresenterBase second)
        {
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));
            _first = first ?? throw new ArgumentNullException(nameof(first));
            _second = second ?? throw new ArgumentNullException(nameof(second));
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed)
            {
                return;
            }

            _elapsed += deltaTime > 0f ? deltaTime : 0f;
            _first.FaceVignetteTarget(_binding.FirstFacingTarget.position, deltaTime);
            _second.FaceVignetteTarget(_binding.SecondFacingTarget.position, deltaTime);
            if (_elapsed >= _binding.PhaseDuration)
            {
                _elapsed = 0f;
                _firstIsSpeaking = !_firstIsSpeaking;
            }

            _first.SetVignetteActivity(_firstIsSpeaking);
            _second.SetVignetteActivity(!_firstIsSpeaking);
        }

        public void Dispose() => _isDisposed = true;
    }
}
