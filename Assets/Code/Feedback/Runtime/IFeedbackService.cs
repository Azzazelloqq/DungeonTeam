using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Feedback.Runtime
{
    public interface IFeedbackService : IDisposable
    {
        UniTask PrepareAsync(IReadOnlyList<FeedbackCue> cues, CancellationToken token);

        void Play(FeedbackCue cue);

        void Play(FeedbackCue cue, in FeedbackContext context);

        void Stop(FeedbackCue cue);

        void Release(IReadOnlyList<FeedbackCue> cues);

        void StopAll();
    }
}
