using System.Threading;

namespace Code.Timer
{
public readonly struct CallbackEntry
{
	public float NormalizedTime { get; }
	public CancellationTokenSource CancellationTokenSource { get; }

	public CallbackEntry(float normalizedTime, CancellationTokenSource cancellationTokenSource)
	{
		NormalizedTime = normalizedTime;
		CancellationTokenSource = cancellationTokenSource;
	}

	public CallbackEntry(float normalizedTime)
	{
		NormalizedTime = normalizedTime;
		CancellationTokenSource = new CancellationTokenSource();
	}
}
}