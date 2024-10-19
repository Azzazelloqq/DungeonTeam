using System;
using System.Threading;
using System.Threading.Tasks;

namespace Code.Timer
{
public class ActionTimer : IDisposable
{
	private CancellationTokenSource _cancelTimerTokenSource;
    public bool IsInProgress { get; private set; }

    public void Dispose()
	{
		if (!_cancelTimerTokenSource.IsCancellationRequested)
		{
			_cancelTimerTokenSource.Cancel();
		}
		
		_cancelTimerTokenSource.Dispose();
        
        IsInProgress = false;
    }

	public void StartTimer(int timerMilliseconds, Action onTimerCompleted = null)
    {
        StartTimerPerMillisecond(timerMilliseconds, onTimerCompleted);
	}

    public void StartTimer(float seconds, Action onTimerCompleted = null)
    {
        var milliseconds = (int)(seconds * 1000);
        
        StartTimerPerMillisecond(milliseconds, onTimerCompleted);
    }

	public void StopTimer()
	{
		_cancelTimerTokenSource?.Cancel();
        IsInProgress = false;
    }

    private void StartTimerPerMillisecond(int timePerMillisecond, Action onTimerCompleted)
    {
        IsInProgress = true;
        _cancelTimerTokenSource = new CancellationTokenSource();
        var token = _cancelTimerTokenSource.Token;
		
        Task.Delay(timePerMillisecond, token).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                return;
            }

            IsInProgress = false;
                
            onTimerCompleted?.Invoke();
        }, token);
    }
}
}