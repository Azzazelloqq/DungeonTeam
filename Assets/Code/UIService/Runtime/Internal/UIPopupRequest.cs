using System.Threading;
using Cysharp.Threading.Tasks;

namespace Code.UIService
{
    internal readonly struct UIPopupRequest
    {
        public UIPopupRequest(
            UIElementEntry entry,
            CancellationToken token,
            UniTaskCompletionSource completion = null)
        {
            Entry = entry;
            Token = token;
            Completion = completion;
        }

        public UIElementEntry Entry { get; }

        public CancellationToken Token { get; }

        public UniTaskCompletionSource Completion { get; }
    }
}
