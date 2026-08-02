namespace DungeonTeam.Feedback.Runtime.Audio
{
    public readonly struct AudioFeedbackMetrics
    {
        internal AudioFeedbackMetrics(
            int activeVoices,
            int cooldownRejections,
            int ownerLimitRejections,
            int capacityRejections,
            int replacedVoices)
        {
            ActiveVoices = activeVoices;
            CooldownRejections = cooldownRejections;
            OwnerLimitRejections = ownerLimitRejections;
            CapacityRejections = capacityRejections;
            ReplacedVoices = replacedVoices;
        }

        public int ActiveVoices { get; }
        public int CooldownRejections { get; }
        public int OwnerLimitRejections { get; }
        public int CapacityRejections { get; }
        public int ReplacedVoices { get; }
    }
}
