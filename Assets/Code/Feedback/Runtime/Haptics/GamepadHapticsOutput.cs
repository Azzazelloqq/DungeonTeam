using UnityEngine.InputSystem;

namespace DungeonTeam.Feedback.Runtime.Haptics
{
    internal sealed class GamepadHapticsOutput : IHapticsOutput
    {
        private Gamepad _activeGamepad;

        public bool IsAvailable => Gamepad.current != null;

        public void SetMotorSpeeds(float lowFrequency, float highFrequency)
        {
            var gamepad = Gamepad.current;
            if (gamepad == null)
            {
                Reset();
                return;
            }

            if (_activeGamepad != null && _activeGamepad != gamepad)
            {
                _activeGamepad.ResetHaptics();
            }

            _activeGamepad = gamepad;
            _activeGamepad.SetMotorSpeeds(lowFrequency, highFrequency);
        }

        public void Reset()
        {
            if (_activeGamepad != null)
            {
                _activeGamepad.ResetHaptics();
                _activeGamepad = null;
            }
        }
    }
}
