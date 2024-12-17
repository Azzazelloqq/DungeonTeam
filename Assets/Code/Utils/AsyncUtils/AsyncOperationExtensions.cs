using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Code.Utils.AsyncUtils
{
public static class AsyncOperationExtensions
{
	public static async Task<T[]> AsTask<T>(this AsyncInstantiateOperation<T> asyncInstantiate)
	{
		while (!asyncInstantiate.isDone)
		{
			await Task.Yield();
		}

		return asyncInstantiate.Result;
	}

	public static void CancelAndDispose(this CancellationTokenSource cancellationTokenSource)
	{
		if (!cancellationTokenSource.IsCancellationRequested)
		{
			cancellationTokenSource.Cancel();
		}
		
		cancellationTokenSource.Dispose();
	}
	
	public static void CancelAndDispose(this IEnumerable<CancellationTokenSource> cancellationTokenSource)
	{
		foreach (var tokenSource in cancellationTokenSource)
		{
			tokenSource.CancelAndDispose();
		}
	}
}
}