using System;
using DungeonTeam.Gameplay.Actors.Runtime;
using DungeonTeam.Gameplay.Skills.Domain;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public sealed class SkillUseHandle
    {
        private readonly SkillUseExecution _execution;

        internal SkillUseHandle(SkillUseExecution execution)
        {
            _execution = execution ?? throw new ArgumentNullException(nameof(execution));
        }

        public SkillUsePhase Phase => _execution.Phase;
        public bool IsActive => _execution.IsActive;
        public bool HasCommitted => _execution.HasCommitted;

        public bool TryCancel()
        {
            return _execution.TryCancel();
        }
    }

    internal sealed class SkillUseExecution : IDisposable
    {
        private readonly SkillUseTimeline _timeline;
        private readonly SkillPresentationPlayer _presentation;
        private readonly SkillPresentationSequence _sequence;
        private int _presentationGroupId;
        private bool _isStarted;
        private bool _isDisposed;

        public SkillUseExecution(
            ActorInstance source,
            ActorInstance target,
            SkillDefinition skill,
            SkillLevelDefinition level,
            SkillPresentationSequence sequence,
            SkillPresentationPlayer presentation)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Skill = skill ?? throw new ArgumentNullException(nameof(skill));
            Level = level ?? throw new ArgumentNullException(nameof(level));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _timeline = new SkillUseTimeline(level.UseTiming);
        }

        public ActorInstance Source { get; }
        public ActorInstance Target { get; }
        public SkillDefinition Skill { get; }
        public SkillLevelDefinition Level { get; }
        public SkillUsePhase Phase => _timeline.Phase;
        public bool IsActive => !_isDisposed && _timeline.IsActive;
        public bool HasCommitted => _timeline.HasCommitted;
        public int PresentationGroupId => _presentationGroupId;
        public SkillPresentationSequence Sequence => _sequence;

        public SkillUseAdvanceResult Start()
        {
            RequireNotDisposed();
            if (_isStarted)
                throw new InvalidOperationException("Skill use execution is already started.");

            _isStarted = true;
            _presentationGroupId = _presentation.Begin(_sequence, Source, Target);
            return _timeline.Advance(0f);
        }

        public SkillUseAdvanceResult Advance(float deltaTime)
        {
            RequireStarted();
            if (!_timeline.HasCommitted && (!Source.IsAlive || !Target.IsAlive))
            {
                TryCancel();
                return default;
            }

            return _timeline.Advance(deltaTime);
        }

        public void PlayPhase(SkillPresentationPhase phase, UnityEngine.Vector3? impactPosition = null)
        {
            RequireStarted();
            _presentation.PlayPhase(
                _presentationGroupId,
                _sequence,
                phase,
                Source,
                Target,
                impactPosition);
        }

        public bool TryCancel()
        {
            if (_isDisposed || !_isStarted || !_timeline.TryCancel())
                return false;

            _presentation.Cancel(
                _presentationGroupId,
                _sequence,
                Source,
                Target);
            return true;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (_isStarted && _timeline.IsActive)
            {
                _presentation.Cancel(
                    _presentationGroupId,
                    _sequence,
                    Source,
                    Target);
                _timeline.TryCancel();
            }

            _isDisposed = true;
        }

        private void RequireStarted()
        {
            RequireNotDisposed();
            if (!_isStarted)
                throw new InvalidOperationException("Skill use execution is not started.");
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SkillUseExecution));
        }
    }
}
