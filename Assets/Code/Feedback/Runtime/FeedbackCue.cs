using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonTeam.Feedback.Runtime
{
    [Serializable]
    public sealed class FeedbackCue
    {
        [SerializeReference]
        private FeedbackPayload[] _payloads = Array.Empty<FeedbackPayload>();

        public FeedbackCue()
        {
        }

        public FeedbackCue(params FeedbackPayload[] payloads)
        {
            _payloads = payloads != null
                ? (FeedbackPayload[])payloads.Clone()
                : throw new ArgumentNullException(nameof(payloads));
        }

        public IReadOnlyList<FeedbackPayload> Payloads => _payloads;

        public void Validate()
        {
            if (_payloads == null || _payloads.Length == 0)
            {
                throw new InvalidOperationException("Feedback cue requires at least one payload.");
            }

            for (var index = 0; index < _payloads.Length; index++)
            {
                var payload = _payloads[index] ?? throw new InvalidOperationException(
                    $"Feedback cue contains an empty payload at index {index}.");
                payload.Validate();
            }
        }
    }
}
