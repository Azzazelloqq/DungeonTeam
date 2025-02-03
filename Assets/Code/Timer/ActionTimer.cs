using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Utils.FloatUtils;
using InGameLogger;

namespace Code.Timer
{
public class ActionTimer : IDisposable
{
	private readonly IInGameLogger _logger;
	private CancellationTokenSource _cancelTimerTokenSource;
    public bool IsInProgress { get; private set; }

	public ActionTimer(IInGameLogger logger)
	{
		_logger = logger;
	}
	
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
		var milliseconds = seconds.ToMilliseconds();
        
        StartTimerPerMillisecond(milliseconds, onTimerCompleted);
    }

	public void StartLoopTickTimer(int tickPeriodPerMilliseconds, Action onTick)
	{
		StartTickTimerPerMillisecond(tickPeriodPerMilliseconds, onTick);
	}

	public void StopTimer()
	{
		_cancelTimerTokenSource?.Cancel();
        IsInProgress = false;
    }

	private async void StartTimerPerMillisecond(int timePerMillisecond, Action onTimerCompleted)
	{
		try
		{
			IsInProgress = true;
			_cancelTimerTokenSource = new CancellationTokenSource();
			var token = _cancelTimerTokenSource.Token;

			await Task.Delay(timePerMillisecond, token);

			if(token.IsCancellationRequested)
			{
				IsInProgress = false;
				return;
			}
			
			onTimerCompleted?.Invoke();
			IsInProgress = false;
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
		}
	}

	private async void StartTickTimerPerMillisecond(int tickPeriodPerMilliseconds, Action onTick)
	{
		try
		{
			IsInProgress = true;
			_cancelTimerTokenSource = new CancellationTokenSource();
			var token = _cancelTimerTokenSource.Token;


			while (!token.IsCancellationRequested)
			{
				await Task.Delay(tickPeriodPerMilliseconds, token);

				if (token.IsCancellationRequested)
				{
					IsInProgress = false;
					return;
				}

				onTick?.Invoke();
			}

			IsInProgress = false;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Timer was canceled");
		}
		catch (Exception e)
		{
			_logger.LogException(e);
		}
	}
}
}