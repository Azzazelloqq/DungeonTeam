using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LightDI.Runtime;
using ResourceLoader;

namespace DungeonTeam.Feedback.Runtime.Banks
{
    public sealed class FeedbackBankLoader
    {
        private readonly IResourceLoader _resourceLoader;
        private readonly IFeedbackService _feedbackService;

        public FeedbackBankLoader(
            [Inject] IResourceLoader resourceLoader,
            [Inject] IFeedbackService feedbackService)
        {
            _resourceLoader = resourceLoader ??
                              throw new ArgumentNullException(nameof(resourceLoader));
            _feedbackService = feedbackService ??
                               throw new ArgumentNullException(nameof(feedbackService));
        }

        public async UniTask<FeedbackBankLease<TBank>> LoadAsync<TBank>(
            string resourceId,
            CancellationToken token)
            where TBank : FeedbackBank
        {
            if (string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ArgumentException(
                    "Feedback bank resource ID cannot be empty.",
                    nameof(resourceId));
            }

            TBank bank = null;
            try
            {
                bank = await _resourceLoader.LoadResourceAsync<TBank>(resourceId, token);
                if (bank == null)
                {
                    throw new InvalidOperationException(
                        $"Feedback bank '{resourceId}' loaded as null.");
                }

                bank.Validate();
                await _feedbackService.PrepareAsync(bank.Cues, token);
                return new FeedbackBankLease<TBank>(
                    bank,
                    _feedbackService,
                    _resourceLoader);
            }
            catch
            {
                if (bank != null)
                {
                    _resourceLoader.ReleaseResource(bank);
                }

                throw;
            }
        }
    }
}
