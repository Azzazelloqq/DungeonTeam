using System;

namespace DungeonTeam.Gameplay.Skills.Domain
{
    public readonly struct SkillUseTiming
    {
        public SkillUseTiming(float commitDelay, float recoveryDuration)
        {
            CommitDelay = RequireDuration(commitDelay, nameof(commitDelay));
            RecoveryDuration = RequireDuration(recoveryDuration, nameof(recoveryDuration));
        }

        public float CommitDelay { get; }
        public float RecoveryDuration { get; }

        private static float RequireDuration(float value, string parameterName)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }
    }

    public enum SkillUsePhase
    {
        Preparing = 0,
        Recovering = 1,
        Completed = 2,
        Cancelled = 3
    }

    public readonly struct SkillUseAdvanceResult
    {
        internal SkillUseAdvanceResult(bool committed, bool completed)
        {
            Committed = committed;
            Completed = completed;
        }

        public bool Committed { get; }
        public bool Completed { get; }
    }

    public sealed class SkillUseTimeline
    {
        private const float TimeEpsilon = 0.000001f;
        private readonly SkillUseTiming _timing;
        private float _phaseElapsed;

        public SkillUseTimeline(SkillUseTiming timing)
        {
            _timing = timing;
            Phase = SkillUsePhase.Preparing;
        }

        public SkillUsePhase Phase { get; private set; }
        public bool HasCommitted { get; private set; }
        public bool IsActive =>
            Phase is SkillUsePhase.Preparing or SkillUsePhase.Recovering;

        public SkillUseAdvanceResult Advance(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            if (!IsActive)
            {
                return default;
            }

            var remaining = deltaTime;
            var committed = false;
            if (Phase == SkillUsePhase.Preparing)
            {
                var untilCommit = Math.Max(0f, _timing.CommitDelay - _phaseElapsed);
                if (remaining + TimeEpsilon < untilCommit)
                {
                    _phaseElapsed += remaining;
                    return default;
                }

                remaining = Math.Max(0f, remaining - untilCommit);
                _phaseElapsed = 0f;
                Phase = SkillUsePhase.Recovering;
                HasCommitted = true;
                committed = true;
            }

            var untilComplete = Math.Max(0f, _timing.RecoveryDuration - _phaseElapsed);
            if (remaining + TimeEpsilon < untilComplete)
            {
                _phaseElapsed += remaining;
                return new SkillUseAdvanceResult(committed, completed: false);
            }

            Phase = SkillUsePhase.Completed;
            _phaseElapsed = 0f;
            return new SkillUseAdvanceResult(committed, completed: true);
        }

        public bool TryCancel()
        {
            if (!IsActive || HasCommitted)
            {
                return false;
            }

            Phase = SkillUsePhase.Cancelled;
            _phaseElapsed = 0f;
            return true;
        }
    }
}
