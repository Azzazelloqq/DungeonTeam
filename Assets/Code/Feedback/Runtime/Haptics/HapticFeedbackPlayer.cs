using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TickHandler;

namespace DungeonTeam.Feedback.Runtime.Haptics
{
    public sealed class HapticFeedbackPlayer : IFeedbackPlayer
    {
        private readonly ITickHandler _tickHandler;
        private readonly IHapticsOutput _output;
        private readonly HapticMixer _mixer;
        private readonly Dictionary<HapticFeedbackPayload, int> _preparationCounts = new();

        private int _cooldownRejections;
        private int _ownerLimitRejections;
        private int _capacityRejections;
        private bool _hasOutput;
        private bool _isDisposed;

        public HapticFeedbackPlayer(ITickHandler tickHandler, int impulseLimit)
            : this(tickHandler, new GamepadHapticsOutput(), impulseLimit)
        {
        }

        internal HapticFeedbackPlayer(
            ITickHandler tickHandler,
            IHapticsOutput output,
            int impulseLimit)
        {
            _tickHandler = tickHandler ?? throw new ArgumentNullException(nameof(tickHandler));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _mixer = new HapticMixer(impulseLimit);
            _tickHandler.SubscribeOnFrameUpdate(OnFrameUpdate);
        }

        public HapticFeedbackMetrics Metrics => new(
            _mixer.ActiveCount,
            _cooldownRejections,
            _ownerLimitRejections,
            _capacityRejections);

        public UniTask PrepareAsync(FeedbackCue cue, CancellationToken token)
        {
            RequireNotDisposed();
            token.ThrowIfCancellationRequested();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var preparedInCall = new List<HapticFeedbackPayload>();
            try
            {
                var payloads = cue.Payloads;
                for (var index = 0; index < payloads.Count; index++)
                {
                    if (payloads[index] is not HapticFeedbackPayload payload)
                    {
                        continue;
                    }

                    payload.Validate();
                    _preparationCounts.TryGetValue(payload, out var count);
                    _preparationCounts[payload] = checked(count + 1);
                    preparedInCall.Add(payload);
                }

                return UniTask.CompletedTask;
            }
            catch
            {
                for (var index = preparedInCall.Count - 1; index >= 0; index--)
                {
                    ReleasePayload(preparedInCall[index]);
                }

                throw;
            }
        }

        public void Play(FeedbackCue cue, in FeedbackContext context)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            if (context.Intensity <= 0f || !_output.IsAvailable)
            {
                return;
            }

            var payloads = cue.Payloads;
            for (var index = 0; index < payloads.Count; index++)
            {
                if (payloads[index] is not HapticFeedbackPayload payload)
                {
                    continue;
                }

                if (!_preparationCounts.ContainsKey(payload))
                {
                    throw new InvalidOperationException(
                        "Haptic feedback payload must be prepared before playback.");
                }

                RecordRejection(_mixer.TryAdd(payload, context.Intensity));
            }

            UpdateOutput(deltaTime: 0f);
        }

        public void Stop(FeedbackCue cue)
        {
            RequireNotDisposed();
            if (cue == null)
            {
                throw new ArgumentNullException(nameof(cue));
            }

            var payloads = cue.Payloads;
            for (var index = 0; index < payloads.Count; index++)
            {
                if (payloads[index] is HapticFeedbackPayload payload)
                {
                    _mixer.Stop(payload);
                }
            }

            UpdateOutput(deltaTime: 0f);
        }

        public void Release(FeedbackCue cue)
        {
            if (_isDisposed || cue == null)
            {
                return;
            }

            Stop(cue);
            var payloads = cue.Payloads;
            for (var index = 0; index < payloads.Count; index++)
            {
                if (payloads[index] is HapticFeedbackPayload payload)
                {
                    ReleasePayload(payload);
                }
            }
        }

        public void StopAll()
        {
            RequireNotDisposed();
            _mixer.Clear();
            ResetOutput();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _tickHandler.UnsubscribeOnFrameUpdate(OnFrameUpdate);
            StopAll();
            _isDisposed = true;
            _preparationCounts.Clear();
        }

        private void OnFrameUpdate(float deltaTime)
        {
            UpdateOutput(deltaTime);
        }

        private void UpdateOutput(float deltaTime)
        {
            _mixer.Advance(deltaTime, out var lowFrequency, out var highFrequency);
            if (_mixer.ActiveCount == 0 || !_output.IsAvailable)
            {
                ResetOutput();
                return;
            }

            _output.SetMotorSpeeds(lowFrequency, highFrequency);
            _hasOutput = true;
        }

        private void ResetOutput()
        {
            if (!_hasOutput)
            {
                return;
            }

            _output.Reset();
            _hasOutput = false;
        }

        private void ReleasePayload(HapticFeedbackPayload payload)
        {
            if (!_preparationCounts.TryGetValue(payload, out var count))
            {
                return;
            }

            if (count > 1)
            {
                _preparationCounts[payload] = count - 1;
                return;
            }

            _preparationCounts.Remove(payload);
            _mixer.Forget(payload);
        }

        private void RecordRejection(HapticRejectionReason reason)
        {
            switch (reason)
            {
                case HapticRejectionReason.Cooldown:
                    _cooldownRejections++;
                    break;
                case HapticRejectionReason.OwnerLimit:
                    _ownerLimitRejections++;
                    break;
                case HapticRejectionReason.Capacity:
                    _capacityRejections++;
                    break;
            }
        }

        private void RequireNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(HapticFeedbackPlayer));
            }
        }
    }
}
