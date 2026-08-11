using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonTeam.Gameplay.Skills.Runtime
{
    [CreateAssetMenu(
        menuName = "DungeonTeam/Gameplay/Skill Presentation Sequence",
        fileName = "SkillPresentationSequence")]
    public sealed class SkillPresentationAsset : ScriptableObject
    {
        [SerializeField] private SkillActorAnimationCue[] _animationCues =
            Array.Empty<SkillActorAnimationCue>();
        [SerializeField] private SkillVfxCue[] _vfxCues = Array.Empty<SkillVfxCue>();

        public SkillPresentationSequence CreateSequence()
        {
            return new SkillPresentationSequence(_animationCues, _vfxCues);
        }

        public void CollectValidationErrors(ICollection<string> errors)
        {
            if (errors == null)
            {
                throw new ArgumentNullException(nameof(errors));
            }

            if (_animationCues == null)
            {
                errors.Add("Animation cues array is missing.");
            }
            else
            {
                for (var index = 0; index < _animationCues.Length; index++)
                {
                    var cue = _animationCues[index];
                    if (cue == null)
                        errors.Add($"Actor animation cue at index {index} is missing.");
                    else
                        cue.CollectValidationErrors($"Actor animation cue at index {index}", errors);
                }
            }

            if (_vfxCues == null)
            {
                errors.Add("VFX cues array is missing.");
            }
            else
            {
                for (var index = 0; index < _vfxCues.Length; index++)
                {
                    var cue = _vfxCues[index];
                    if (cue == null)
                        errors.Add($"VFX cue at index {index} is missing.");
                    else
                        cue.CollectValidationErrors($"VFX cue at index {index}", errors);
                }
            }
        }
    }
}
