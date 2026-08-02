using System;

namespace DungeonTeam.Gameplay.Team.Domain
{
    public enum CompanionFollowState
    {
        Holding,
        Following
    }

    public sealed class CompanionFollowBrain
    {
        private readonly float _startFollowingDistance;
        private readonly float _stopFollowingDistance;

        public CompanionFollowBrain(
            float startFollowingDistance,
            float stopFollowingDistance)
        {
            if (stopFollowingDistance < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(stopFollowingDistance));
            }

            if (startFollowingDistance <= stopFollowingDistance)
            {
                throw new ArgumentException(
                    "Start following distance must exceed stop following distance.",
                    nameof(startFollowingDistance));
            }

            _startFollowingDistance = startFollowingDistance;
            _stopFollowingDistance = stopFollowingDistance;
        }

        public CompanionFollowState State { get; private set; }

        public CompanionFollowState Evaluate(float distanceToLeader)
        {
            if (distanceToLeader < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(distanceToLeader));
            }

            if (State == CompanionFollowState.Holding &&
                distanceToLeader > _startFollowingDistance)
            {
                State = CompanionFollowState.Following;
            }
            else if (State == CompanionFollowState.Following &&
                     distanceToLeader <= _stopFollowingDistance)
            {
                State = CompanionFollowState.Holding;
            }

            return State;
        }
    }
}
