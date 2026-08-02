using System;

namespace DungeonTeam.Feedback.Runtime
{
    [Serializable]
    public abstract class FeedbackPayload
    {
        public abstract void Validate();
    }
}
