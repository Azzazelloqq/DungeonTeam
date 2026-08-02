using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Feedback.Runtime
{
    public interface IFeedbackPlayer : IDisposable
    {
        // Preparation must be atomic: on failure the player rolls back its own partial work.
        UniTask PrepareAsync(FeedbackCue cue, CancellationToken token);

        void Play(FeedbackCue cue, in FeedbackContext context);

        void Stop(FeedbackCue cue);

        void Release(FeedbackCue cue);

        void StopAll();
    }
}
