namespace DungeonTeam.Feedback.Runtime.Haptics
{
    public readonly struct HapticFeedbackMetrics
    {
        internal HapticFeedbackMetrics(
            int activeImpulses,
            int cooldownRejections,
            int ownerLimitRejections,
            int capacityRejections)
        {
            ActiveImpulses = activeImpulses;
            CooldownRejections = cooldownRejections;
            OwnerLimitRejections = ownerLimitRejections;
            CapacityRejections = capacityRejections;
        }

        public int ActiveImpulses { get; }
        public int CooldownRejections { get; }
        public int OwnerLimitRejections { get; }
        public int CapacityRejections { get; }
    }
}
