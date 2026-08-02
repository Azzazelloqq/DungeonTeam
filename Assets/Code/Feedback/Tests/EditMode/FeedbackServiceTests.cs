using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Feedback.Runtime;
using NUnit.Framework;

namespace DungeonTeam.Feedback.Tests
{
    public sealed class FeedbackServiceTests
    {
        [Test]
        public void Play_BeforePreparation_Throws()
        {
            using var service = new FeedbackService(new[] { new RecordingPlayer() });
            var cue = CreateCue();

            Assert.Throws<System.InvalidOperationException>(() =>
                service.Play(cue, FeedbackContext.Global()));
        }

        [Test]
        public async System.Threading.Tasks.Task PrepareAndPlay_WithTwoPlayers_ForwardsToBothInOrder()
        {
            var calls = new List<string>();
            using var service = new FeedbackService(new IFeedbackPlayer[]
            {
                new RecordingPlayer("first", calls),
                new RecordingPlayer("second", calls)
            });
            var cue = CreateCue();

            await service.PrepareAsync(new[] { cue }, default);
            service.Play(cue, FeedbackContext.Global());

            Assert.That(calls, Is.EqualTo(new[]
            {
                "first.prepare",
                "second.prepare",
                "first.play",
                "second.play"
            }));
        }

        [Test]
        public async System.Threading.Tasks.Task Release_WhenCuePreparedTwice_ReleasesAfterLastOwner()
        {
            var player = new RecordingPlayer();
            using var service = new FeedbackService(new[] { player });
            var cue = CreateCue();

            await service.PrepareAsync(new[] { cue }, default);
            await service.PrepareAsync(new[] { cue }, default);
            service.Release(new[] { cue });
            service.Play(cue, FeedbackContext.Global());

            Assert.That(player.PrepareCount, Is.EqualTo(1));
            Assert.That(player.ReleaseCount, Is.Zero);

            service.Release(new[] { cue });

            Assert.That(player.ReleaseCount, Is.EqualTo(1));
            Assert.Throws<System.InvalidOperationException>(() =>
                service.Play(cue, FeedbackContext.Global()));
        }

        [Test]
        public void Prepare_WhenLaterPlayerFails_ReleasesEarlierPlayers()
        {
            var first = new RecordingPlayer();
            var failing = new RecordingPlayer(failDuringPreparation: true);
            using var service = new FeedbackService(new IFeedbackPlayer[] { first, failing });

            Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
                await service.PrepareAsync(new[] { CreateCue() }, default));

            Assert.That(first.ReleaseCount, Is.EqualTo(1));
            Assert.That(failing.ReleaseCount, Is.Zero);
        }

        [Test]
        public void Dispose_CalledTwice_DisposesPlayersOnceInReverseOrder()
        {
            var calls = new List<string>();
            var service = new FeedbackService(new IFeedbackPlayer[]
            {
                new RecordingPlayer("first", calls),
                new RecordingPlayer("second", calls)
            });

            service.Dispose();
            service.Dispose();

            Assert.That(calls, Is.EqualTo(new[] { "second.dispose", "first.dispose" }));
        }

        private static FeedbackCue CreateCue()
        {
            return new FeedbackCue(new TestPayload());
        }

        private sealed class TestPayload : FeedbackPayload
        {
            public override void Validate()
            {
            }
        }

        private sealed class RecordingPlayer : IFeedbackPlayer
        {
            private readonly string _name;
            private readonly List<string> _calls;
            private readonly bool _failDuringPreparation;

            public RecordingPlayer(
                string name = "player",
                List<string> calls = null,
                bool failDuringPreparation = false)
            {
                _name = name;
                _calls = calls;
                _failDuringPreparation = failDuringPreparation;
            }

            public int PrepareCount { get; private set; }
            public int ReleaseCount { get; private set; }

            public UniTask PrepareAsync(FeedbackCue cue, CancellationToken token)
            {
                token.ThrowIfCancellationRequested();
                if (_failDuringPreparation)
                {
                    throw new System.InvalidOperationException("Preparation failed.");
                }

                PrepareCount++;
                _calls?.Add($"{_name}.prepare");
                return UniTask.CompletedTask;
            }

            public void Play(FeedbackCue cue, in FeedbackContext context)
            {
                _calls?.Add($"{_name}.play");
            }

            public void Stop(FeedbackCue cue)
            {
            }

            public void Release(FeedbackCue cue)
            {
                ReleaseCount++;
            }

            public void StopAll()
            {
            }

            public void Dispose()
            {
                _calls?.Add($"{_name}.dispose");
            }
        }
    }
}
