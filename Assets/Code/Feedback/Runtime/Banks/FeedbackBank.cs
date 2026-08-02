using System.Collections.Generic;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime.Banks
{
    public abstract class FeedbackBank : ScriptableObject
    {
        public abstract IReadOnlyList<FeedbackCue> Cues { get; }

        public void Validate()
        {
            var cues = Cues;
            if (cues == null)
            {
                throw new System.InvalidOperationException(
                    $"Feedback bank '{name}' returned no cue collection.");
            }

            var uniqueCues = new HashSet<FeedbackCue>();
            for (var index = 0; index < cues.Count; index++)
            {
                var cue = cues[index] ?? throw new System.InvalidOperationException(
                    $"Feedback bank '{name}' contains an empty cue at index {index}.");
                if (!uniqueCues.Add(cue))
                {
                    throw new System.InvalidOperationException(
                        $"Feedback bank '{name}' contains the same cue more than once.");
                }

                cue.Validate();
            }
        }
    }
}
