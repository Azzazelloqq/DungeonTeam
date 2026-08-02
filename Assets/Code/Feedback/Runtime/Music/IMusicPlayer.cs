using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Feedback.Runtime.Music
{
    public interface IMusicPlayer : IDisposable
    {
        UniTask PrepareAsync(MusicTrack track, CancellationToken token);

        void Play(MusicTrack track);

        void Stop();

        void Release(MusicTrack track);
    }
}
