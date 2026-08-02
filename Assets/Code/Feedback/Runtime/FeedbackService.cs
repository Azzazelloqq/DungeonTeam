using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace DungeonTeam.Feedback.Runtime
{
    public sealed class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackPlayer[] _players;
        private readonly Dictionary<FeedbackCue, int> _preparationCounts = new();
        private bool _isDisposed;

        public FeedbackService(IReadOnlyList<IFeedbackPlayer> players)
        {
            if (players == null)
            {
                throw new ArgumentNullException(nameof(players));
            }

            _players = new IFeedbackPlayer[players.Count];
            var uniquePlayers = new HashSet<IFeedbackPlayer>();
            for (var index = 0; index < players.Count; index++)
            {
                var player = players[index] ?? throw new ArgumentException(
                    $"Feedback player at index {index} is missing.",
                    nameof(players));
                if (!uniquePlayers.Add(player))
                {
                    throw new ArgumentException(
                        $"Feedback player at index {index} is registered more than once.",
                        nameof(players));
                }

                _players[index] = player;
            }
        }

        public async UniTask PrepareAsync(
            IReadOnlyList<FeedbackCue> cues,
            CancellationToken token)
        {
            RequireNotDisposed();
            if (cues == null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            var preparedInCall = new List<FeedbackCue>(cues.Count);
            try
            {
                for (var cueIndex = 0; cueIndex < cues.Count; cueIndex++)
                {
                    token.ThrowIfCancellationRequested();
                    var cue = RequireCue(cues[cueIndex], cueIndex);
                    cue.Validate();

                    if (_preparationCounts.TryGetValue(cue, out var count))
                    {
                        _preparationCounts[cue] = checked(count + 1);
                        preparedInCall.Add(cue);
                        continue;
                    }

                    var preparedPlayerCount = 0;
                    try
                    {
                        for (; preparedPlayerCount < _players.Length; preparedPlayerCount++)
                        {
                            await _players[preparedPlayerCount].PrepareAsync(cue, token);
                        }
                    }
                    catch
                    {
                        for (var playerIndex = preparedPlayerCount - 1;
                             playerIndex >= 0;
                             playerIndex--)
                        {
                            _players[playerIndex].Release(cue);
                        }

                        throw;
                    }

                    _preparationCounts.Add(cue, 1);
                    preparedInCall.Add(cue);
                }
            }
            catch
            {
                ReleasePreparedInCall(preparedInCall);
                throw;
            }
        }

        public void Play(FeedbackCue cue)
        {
            var context = FeedbackContext.Global();
            Play(cue, context);
        }

        public void Play(FeedbackCue cue, in FeedbackContext context)
        {
            RequirePrepared(cue);
            for (var index = 0; index < _players.Length; index++)
            {
                _players[index].Play(cue, context);
            }
        }

        public void Stop(FeedbackCue cue)
        {
            RequirePrepared(cue);
            StopPlayers(cue);
        }

        public void Release(IReadOnlyList<FeedbackCue> cues)
        {
            RequireNotDisposed();
            if (cues == null)
            {
                throw new ArgumentNullException(nameof(cues));
            }

            for (var index = cues.Count - 1; index >= 0; index--)
            {
                ReleasePreparedCue(RequireCue(cues[index], index));
            }
        }

        public void StopAll()
        {
            RequireNotDisposed();
            for (var index = _players.Length - 1; index >= 0; index--)
            {
                _players[index].StopAll();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            List<Exception> errors = null;
            for (var index = _players.Length - 1; index >= 0; index--)
            {
                try
                {
                    _players[index].Dispose();
                }
                catch (Exception exception)
                {
                    errors ??= new List<Exception>();
                    errors.Add(exception);
                }
            }

            _preparationCounts.Clear();
            if (errors != null)
            {
                throw new AggregateException(
                    "One or more feedback players failed to dispose.",
                    errors);
            }
        }

        private static FeedbackCue RequireCue(FeedbackCue cue, int index)
        {
            return cue ?? throw new ArgumentException(
                $"Feedback cue at index {index} is missing.",
                nameof(cue));
        }

        private void RequirePrepared(FeedbackCue cue)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (!_preparationCounts.ContainsKey(cue))
            {
                throw new InvalidOperationException(
                    "Feedback cue must be prepared before playback.");
            }
        }

        private void ReleasePreparedInCall(IReadOnlyList<FeedbackCue> cues)
        {
            for (var index = cues.Count - 1; index >= 0; index--)
            {
                ReleasePreparedCue(cues[index]);
            }
        }

        private void ReleasePreparedCue(FeedbackCue cue)
        {
            if (!_preparationCounts.TryGetValue(cue, out var count))
            {
                throw new InvalidOperationException(
                    "Feedback cue cannot be released because it is not prepared.");
            }

            if (count > 1)
            {
                _preparationCounts[cue] = count - 1;
                return;
            }

            StopPlayers(cue);
            for (var index = _players.Length - 1; index >= 0; index--)
            {
                _players[index].Release(cue);
            }

            _preparationCounts.Remove(cue);
        }

        private void StopPlayers(FeedbackCue cue)
        {
            for (var index = _players.Length - 1; index >= 0; index--)
            {
                _players[index].Stop(cue);
            }
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(FeedbackService));
            }
        }
    }
}
