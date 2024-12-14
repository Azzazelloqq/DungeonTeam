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
}
}