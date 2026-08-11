using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.Actors.Runtime;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    public enum SkillPresentationPhase
    {
        Start = 0,
        Commit = 1,
        Impact = 2,
        Complete = 3,
        Cancel = 4
    }

    public enum SkillVfxAnchor
    {
        SourceOrigin = 0,
        TargetHit = 1,
        ImpactPosition = 2
    }

    [Serializable]
    public sealed class SkillActorAnimationCue
    {
        [SerializeField] private SkillPresentationPhase _phase;
        [SerializeField, Min(0f)] private float _delay;
        [SerializeField] private ActorSkillAnimationCue _cue;

        public SkillActorAnimationCue(
            SkillPresentationPhase phase,
            float delay,
            ActorSkillAnimationCue cue)
        {
            _phase = phase;
            _delay = delay;
            _cue = cue;
            Validate("Actor animation cue");
        }

        public SkillPresentationPhase Phase => _phase;
        public float Delay => _delay;
        public ActorSkillAnimationCue Cue => _cue;

        internal void Validate(string label)
        {
            RequirePhase(_phase, label);
            RequireDuration(_delay, $"{label} delay");
            if (!Enum.IsDefined(typeof(ActorSkillAnimationCue), _cue))
            {
                throw new ArgumentOutOfRangeException(nameof(_cue));
            }
        }

        internal void CollectValidationErrors(string label, ICollection<string> errors)
        {
            try
            {
                Validate(label);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }

        internal static void RequirePhase(SkillPresentationPhase phase, string label)
        {
            if (!Enum.IsDefined(typeof(SkillPresentationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(label, phase, "Unknown presentation phase.");
            }
        }

        internal static float RequireDuration(float value, string label)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            {
                throw new ArgumentOutOfRangeException(label);
            }

            return value;
        }
    }

    [Serializable]
    public sealed class SkillVfxCue
    {
        [SerializeField] private SkillPresentationPhase _phase;
        [SerializeField, Min(0f)] private float _delay;
        [SerializeField, Min(0.01f)] private float _lifetime = 0.25f;
        [SerializeField] private SkillVfxAnchor _anchor = SkillVfxAnchor.SourceOrigin;
        [SerializeField] private bool _followAnchor;
        [SerializeField] private GameObject _prefab;

        public SkillVfxCue(
            SkillPresentationPhase phase,
            float delay,
            float lifetime,
            SkillVfxAnchor anchor,
            bool followAnchor,
            GameObject prefab)
        {
            _phase = phase;
            _delay = delay;
            _lifetime = lifetime;
            _anchor = anchor;
            _followAnchor = followAnchor;
            _prefab = prefab;
            Validate("VFX cue");
        }

        public SkillPresentationPhase Phase => _phase;
        public float Delay => _delay;
        public float Lifetime => _lifetime;
        public SkillVfxAnchor Anchor => _anchor;
        public bool FollowAnchor => _followAnchor;
        public GameObject Prefab => _prefab;

        internal void Validate(string label)
        {
            SkillActorAnimationCue.RequirePhase(_phase, label);
            SkillActorAnimationCue.RequireDuration(_delay, $"{label} delay");
            if (float.IsNaN(_lifetime) || float.IsInfinity(_lifetime) || _lifetime <= 0f)
            {
                throw new ArgumentOutOfRangeException($"{label} lifetime");
            }

            if (!Enum.IsDefined(typeof(SkillVfxAnchor), _anchor))
            {
                throw new ArgumentOutOfRangeException($"{label} anchor");
            }

            if (_phase == SkillPresentationPhase.Impact &&
                _anchor != SkillVfxAnchor.ImpactPosition)
            {
                throw new ArgumentException(
                    $"{label} in Impact phase must use ImpactPosition anchor.");
            }

            if (_anchor == SkillVfxAnchor.ImpactPosition &&
                _phase != SkillPresentationPhase.Impact)
            {
                throw new ArgumentException(
                    $"{label} can use ImpactPosition only in Impact phase.");
            }

            if (_prefab == null)
            {
                throw new ArgumentException($"{label} requires a prefab.");
            }
        }

        internal void CollectValidationErrors(string label, ICollection<string> errors)
        {
            try
            {
                Validate(label);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }
    }

    public sealed class SkillPresentationSequence
    {
        private readonly SkillActorAnimationCue[] _animationCues;
        private readonly SkillVfxCue[] _vfxCues;

        public SkillPresentationSequence(
            IReadOnlyList<SkillActorAnimationCue> animationCues,
            IReadOnlyList<SkillVfxCue> vfxCues)
        {
            if (animationCues == null)
                throw new ArgumentNullException(nameof(animationCues));
            if (vfxCues == null)
                throw new ArgumentNullException(nameof(vfxCues));

            _animationCues = new SkillActorAnimationCue[animationCues.Count];
            for (var index = 0; index < animationCues.Count; index++)
            {
                var cue = animationCues[index] ?? throw new ArgumentException(
                    $"Actor animation cue at index {index} is missing.",
                    nameof(animationCues));
                cue.Validate($"Actor animation cue at index {index}");
                _animationCues[index] = cue;
            }

            _vfxCues = new SkillVfxCue[vfxCues.Count];
            for (var index = 0; index < vfxCues.Count; index++)
            {
                var cue = vfxCues[index] ?? throw new ArgumentException(
                    $"VFX cue at index {index} is missing.",
                    nameof(vfxCues));
                cue.Validate($"VFX cue at index {index}");
                _vfxCues[index] = cue;
            }
        }

        public IReadOnlyList<SkillActorAnimationCue> AnimationCues => _animationCues;
        public IReadOnlyList<SkillVfxCue> VfxCues => _vfxCues;

    }

}
