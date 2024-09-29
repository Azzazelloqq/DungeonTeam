using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code.Timer
{
public class ActionTimer : IDisposable
{
	private CancellationTokenSource _cancelTimerTokenSource;
	
	public void Dispose()
	{
		if (!_cancelTimerTokenSource.IsCancellationRequested)
		{
			_cancelTimerTokenSource.Cancel();
		}
		
		_cancelTimerTokenSource.Dispose();
	}

	public void StartTimer(int timerMilliseconds, Action onTimerCompleted)
	{
		_cancelTimerTokenSource = new CancellationTokenSource();
        var token = _cancelTimerTokenSource.Token;
		
        Task.Delay(timerMilliseconds, token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
            {
                onTimerCompleted?.Invoke();
            }
        }, token);
	}

	public void StopTimer()
	{
		_cancelTimerTokenSource?.Cancel();
	}
}
}