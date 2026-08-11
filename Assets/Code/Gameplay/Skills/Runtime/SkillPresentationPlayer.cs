using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    internal sealed class SkillPresentationPlayer : IDisposable
    {
        private const float TimeEpsilon = 0.000001f;

        private readonly Transform _effectsParent;
        private readonly List<ScheduledAnimation> _scheduledAnimations = new();
        private readonly List<ScheduledVfx> _scheduledVfx = new();
        private readonly List<ActiveVfx> _activeVfx = new();
        private int _nextGroupId = 1;
        private bool _isDisposed;

        public SkillPresentationPlayer(Transform effectsParent)
        {
            _effectsParent = effectsParent != null
                ? effectsParent
                : throw new ArgumentNullException(nameof(effectsParent));
        }

        public int ActiveVfxCount => _activeVfx.Count;

        public int Begin(
            SkillPresentationSequence sequence,
            ActorInstance source,
            ActorInstance target)
        {
            RequireActive();
            var groupId = _nextGroupId++;
            PlayPhase(groupId, sequence, SkillPresentationPhase.Start, source, target);
            return groupId;
        }

        public void PlayPhase(
            int groupId,
            SkillPresentationSequence sequence,
            SkillPresentationPhase phase,
            ActorInstance source,
            ActorInstance target,
            Vector3? impactPosition = null)
        {
            RequireActive();
            if (groupId <= 0)
                throw new ArgumentOutOfRangeException(nameof(groupId));
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (phase == SkillPresentationPhase.Impact && !impactPosition.HasValue)
            {
                throw new ArgumentException(
                    "Impact presentation phase requires an impact position.",
                    nameof(impactPosition));
            }

            for (var index = 0; index < sequence.AnimationCues.Count; index++)
            {
                var cue = sequence.AnimationCues[index];
                if (cue.Phase != phase)
                    continue;

                if (cue.Delay <= 0f)
                    source.PlaySkillFeedback(cue.Cue);
                else
                    _scheduledAnimations.Add(
                        new ScheduledAnimation(groupId, cue.Delay, source, cue.Cue));
            }

            for (var index = 0; index < sequence.VfxCues.Count; index++)
            {
                var cue = sequence.VfxCues[index];
                if (cue.Phase != phase)
                    continue;

                var scheduled = new ScheduledVfx(
                    groupId,
                    cue.Delay,
                    source,
                    target,
                    cue,
                    impactPosition);
                if (cue.Delay <= 0f)
                    Spawn(scheduled);
                else
                    _scheduledVfx.Add(scheduled);
            }
        }

        public void Cancel(
            int groupId,
            SkillPresentationSequence sequence,
            ActorInstance source,
            ActorInstance target)
        {
            RequireActive();
            RemoveGroup(groupId);
            PlayPhase(groupId, sequence, SkillPresentationPhase.Cancel, source, target);
        }

        public void Tick(float deltaTime)
        {
            RequireActive();
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            for (var index = _scheduledAnimations.Count - 1; index >= 0; index--)
            {
                var scheduled = _scheduledAnimations[index];
                scheduled.Remaining -= deltaTime;
                if (scheduled.Remaining > TimeEpsilon)
                    continue;

                scheduled.Source.PlaySkillFeedback(scheduled.Cue);
                _scheduledAnimations.RemoveAt(index);
            }

            for (var index = _scheduledVfx.Count - 1; index >= 0; index--)
            {
                var scheduled = _scheduledVfx[index];
                scheduled.Remaining -= deltaTime;
                if (scheduled.Remaining > TimeEpsilon)
                    continue;

                Spawn(scheduled);
                _scheduledVfx.RemoveAt(index);
            }

            for (var index = _activeVfx.Count - 1; index >= 0; index--)
            {
                var active = _activeVfx[index];
                active.Remaining -= deltaTime;
                if (active.Remaining > TimeEpsilon)
                    continue;

                Destroy(active.GameObject);
                _activeVfx.RemoveAt(index);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _scheduledAnimations.Clear();
            _scheduledVfx.Clear();
            for (var index = _activeVfx.Count - 1; index >= 0; index--)
            {
                Destroy(_activeVfx[index].GameObject);
            }

            _activeVfx.Clear();
        }

        private void Spawn(ScheduledVfx scheduled)
        {
            var cue = scheduled.Cue;
            var anchor = ResolveAnchor(scheduled, out var position);
            GameObject instance;
            if (cue.FollowAnchor)
            {
                if (anchor == null)
                {
                    throw new InvalidOperationException(
                        $"VFX cue anchored to '{cue.Anchor}' cannot follow a world position.");
                }

                instance = UnityEngine.Object.Instantiate(cue.Prefab, anchor, false);
                instance.transform.localPosition = Vector3.zero;
            }
            else
            {
                instance = UnityEngine.Object.Instantiate(
                    cue.Prefab,
                    position,
                    cue.Prefab.transform.rotation,
                    _effectsParent);
            }

            instance.name = $"SkillVfx_{cue.Phase}_{cue.Prefab.name}";
            instance.SetActive(true);
            _activeVfx.Add(new ActiveVfx(scheduled.GroupId, cue.Lifetime, instance));
        }

        private static Transform ResolveAnchor(ScheduledVfx scheduled, out Vector3 position)
        {
            switch (scheduled.Cue.Anchor)
            {
                case SkillVfxAnchor.SourceOrigin:
                    var sourceAnchor = scheduled.Source.SkillOriginAnchor ?? throw new InvalidOperationException(
                        $"Actor '{scheduled.Source.ActorId}' has no Skill Origin Anchor.");
                    position = sourceAnchor.position;
                    return sourceAnchor;
                case SkillVfxAnchor.TargetHit:
                    var targetAnchor = scheduled.Target.HitVfxAnchor;
                    position = targetAnchor != null
                        ? targetAnchor.position
                        : scheduled.Target.Position + Vector3.up;
                    return targetAnchor;
                case SkillVfxAnchor.ImpactPosition:
                    position = scheduled.ImpactPosition ?? throw new InvalidOperationException(
                        "Impact-position VFX cue requires an impact position.");
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void RemoveGroup(int groupId)
        {
            for (var index = _scheduledAnimations.Count - 1; index >= 0; index--)
            {
                if (_scheduledAnimations[index].GroupId == groupId)
                    _scheduledAnimations.RemoveAt(index);
            }

            for (var index = _scheduledVfx.Count - 1; index >= 0; index--)
            {
                if (_scheduledVfx[index].GroupId == groupId)
                    _scheduledVfx.RemoveAt(index);
            }

            for (var index = _activeVfx.Count - 1; index >= 0; index--)
            {
                if (_activeVfx[index].GroupId != groupId)
                    continue;

                Destroy(_activeVfx[index].GameObject);
                _activeVfx.RemoveAt(index);
            }
        }

        private void RequireActive()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SkillPresentationPlayer));
        }

        private static void Destroy(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        private sealed class ScheduledAnimation
        {
            public ScheduledAnimation(
                int groupId,
                float remaining,
                ActorInstance source,
                ActorSkillAnimationCue cue)
            {
                GroupId = groupId;
                Remaining = remaining;
                Source = source;
                Cue = cue;
            }

            public int GroupId { get; }
            public float Remaining { get; set; }
            public ActorInstance Source { get; }
            public ActorSkillAnimationCue Cue { get; }
        }

        private sealed class ScheduledVfx
        {
            public ScheduledVfx(
                int groupId,
                float remaining,
                ActorInstance source,
                ActorInstance target,
                SkillVfxCue cue,
                Vector3? impactPosition)
            {
                GroupId = groupId;
                Remaining = remaining;
                Source = source;
                Target = target;
                Cue = cue;
                ImpactPosition = impactPosition;
            }

            public int GroupId { get; }
            public float Remaining { get; set; }
            public ActorInstance Source { get; }
            public ActorInstance Target { get; }
            public SkillVfxCue Cue { get; }
            public Vector3? ImpactPosition { get; }
        }

        private sealed class ActiveVfx
        {
            public ActiveVfx(int groupId, float remaining, GameObject gameObject)
            {
                GroupId = groupId;
                Remaining = remaining;
                GameObject = gameObject;
            }

            public int GroupId { get; }
            public float Remaining { get; set; }
            public GameObject GameObject { get; }
        }
    }
}
