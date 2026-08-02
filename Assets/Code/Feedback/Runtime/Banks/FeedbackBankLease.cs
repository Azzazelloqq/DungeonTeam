using System;
using ResourceLoader;

namespace DungeonTeam.Feedback.Runtime.Banks
{
    public sealed class FeedbackBankLease<TBank> : IDisposable
        where TBank : FeedbackBank
    {
        private readonly IFeedbackService _feedbackService;
        private readonly IResourceLoader _resourceLoader;
        private TBank _bank;

        internal FeedbackBankLease(
            TBank bank,
            IFeedbackService feedbackService,
            IResourceLoader resourceLoader)
        {
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            _feedbackService = feedbackService ??
                               throw new ArgumentNullException(nameof(feedbackService));
            _resourceLoader = resourceLoader ??
                              throw new ArgumentNullException(nameof(resourceLoader));
        }

        public TBank Bank => _bank ?? throw new ObjectDisposedException(
            typeof(FeedbackBankLease<TBank>).Name);

        public void Dispose()
        {
            var bank = _bank;
            if (bank == null)
            {
                return;
            }

            _bank = null;
            try
            {
                _feedbackService.Release(bank.Cues);
            }
            finally
            {
                _resourceLoader.ReleaseResource(bank);
            }
        }
    }
}
