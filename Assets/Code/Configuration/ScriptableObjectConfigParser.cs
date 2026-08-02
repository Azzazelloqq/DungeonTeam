using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.Config;

namespace Code.Configuration
{
	public sealed class ScriptableObjectConfigParser : IConfigParser
	{
		private readonly ConfigCatalog _catalog;

		public ScriptableObjectConfigParser(ConfigCatalog catalog)
		{
			_catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
		}

		public IConfigPage[] Parse()
		{
			return _catalog.GetPages();
		}

		public Task<IConfigPage[]> ParseAsync(CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			return Task.FromResult(Parse());
		}

		public Task<IConfigPage[]> ParseAsync(IProgress<ParseProgress> progress, CancellationToken token)
		{
			return ParseAsync(token);
		}

		public void ParseAsync(
			Action<ParseProgress> progress,
			Action<IConfigPage[]> onParsed,
			CancellationToken token)
		{
			token.ThrowIfCancellationRequested();
			onParsed?.Invoke(Parse());
		}
	}
}
