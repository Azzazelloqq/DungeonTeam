using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using DungeonTeam.Feedback.Runtime;
using DungeonTeam.Feedback.Runtime.Banks;
using NUnit.Framework;
using ResourceLoader;
using UnityEngine;

namespace DungeonTeam.Feedback.Tests
{
    public sealed class FeedbackBankLoaderTests
    {
        [Test]
        public async Task DisposeLease_AfterSuccessfulLoad_ReleasesFeedbackBeforeResource()
        {
            var calls = new List<string>();
            var cue = new FeedbackCue(new TestPayload());
            var bank = ScriptableObject.CreateInstance<TestBank>();
            bank.SetCues(cue);
            var resourceLoader = new FakeResourceLoader(bank, calls);
            var player = new RecordingPlayer(calls);
            using var service = new FeedbackService(new[] { player });
            var loader = new FeedbackBankLoader(resourceLoader, service);

            var lease = await loader.LoadAsync<TestBank>("generated.bank.id", default);
            lease.Dispose();

            Assert.That(calls, Is.EqualTo(new[]
            {
                "resource.load",
                "player.prepare",
                "player.stop",
                "player.release",
                "resource.release"
            }));
            UnityEngine.Object.DestroyImmediate(bank);
        }

        private sealed class TestBank : FeedbackBank
        {
            private FeedbackCue[] _cues = Array.Empty<FeedbackCue>();

            public override IReadOnlyList<FeedbackCue> Cues => _cues;

            public void SetCues(params FeedbackCue[] cues)
            {
                _cues = cues;
            }
        }

        private sealed class TestPayload : FeedbackPayload
        {
            public override void Validate()
            {
            }
        }

        private sealed class RecordingPlayer : IFeedbackPlayer
        {
            private readonly List<string> _calls;

            public RecordingPlayer(List<string> calls)
            {
                _calls = calls;
            }

            public UniTask PrepareAsync(FeedbackCue cue, CancellationToken token)
            {
                _calls.Add("player.prepare");
                return UniTask.CompletedTask;
            }

            public void Play(FeedbackCue cue, in FeedbackContext context)
            {
            }

            public void Stop(FeedbackCue cue)
            {
                _calls.Add("player.stop");
            }

            public void Release(FeedbackCue cue)
            {
                _calls.Add("player.release");
            }

            public void StopAll()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class FakeResourceLoader : IResourceLoader
        {
            private readonly TestBank _bank;
            private readonly List<string> _calls;

            public FakeResourceLoader(TestBank bank, List<string> calls)
            {
                _bank = bank;
                _calls = calls;
            }

            public Task PreloadInCacheAsync<TResource>(string resourceId, CancellationToken token)
            {
                return Task.CompletedTask;
            }

            public TResource LoadResource<TResource>(string resourceId)
            {
                return (TResource)(object)_bank;
            }

            public void LoadResource<TResource>(
                string resourceId,
                Action<TResource> onResourceLoaded,
                CancellationToken token)
            {
                onResourceLoaded((TResource)(object)_bank);
            }

            public Task<TResource> LoadResourceAsync<TResource>(
                string resourceId,
                CancellationToken token)
            {
                _calls.Add("resource.load");
                return Task.FromResult((TResource)(object)_bank);
            }

            public Task<TComponent> LoadAndCreateAsync<TComponent, TParent>(
                string resourceId,
                TParent parent,
                CancellationToken token = default)
            {
                throw new NotSupportedException();
            }

            public void ReleaseResource<TResource>(TResource resource)
            {
                _calls.Add("resource.release");
            }

            public void ReleaseAllResources()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
