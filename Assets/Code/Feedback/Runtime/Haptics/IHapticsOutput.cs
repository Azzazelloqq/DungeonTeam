namespace DungeonTeam.Feedback.Runtime.Haptics
{
    internal interface IHapticsOutput
    {
        bool IsAvailable { get; }

        void SetMotorSpeeds(float lowFrequency, float highFrequency);

        void Reset();
    }
}
