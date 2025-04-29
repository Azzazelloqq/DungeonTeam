using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Utils.AsyncUtils;
using Code.Utils.FloatUtils;
using InGameLogger;

namespace Code.Timer
{
public class ActionTimer : IDisposable
{
	public bool IsInProgress { get; private set; }

	private readonly IInGameLogger _logger;
	private CancellationTokenSource _cancelTimerTokenSource;
	private readonly Dictionary<Action, CallbackEntry> _normalizedCallbacks = new();
	private int _currentDurationMs;

	public ActionTimer(IInGameLogger logger)
	{
		_logger = logger;
	}

	public void Dispose()
	{
		_cancelTimerTokenSource?.CancelAndDispose();
		ClearAllCallbacks();

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

	public void AddCallbackByNormalizedTime(float normalizedTime, Action callback)
	{
		if (normalizedTime < 0f || normalizedTime > 1f)
		{
			throw new ArgumentOutOfRangeException(nameof(normalizedTime));
		}

		if (_normalizedCallbacks.ContainsKey(callback))
		{
			return;
		}

		var entry = new CallbackEntry(normalizedTime);
		_normalizedCallbacks[callback] = entry;

		// Если таймер уже запущен — сразу планируем
		if (IsInProgress && _currentDurationMs > 0)
		{
			ScheduleCallback(callback, entry);
		}
	}

	public void RemoveCallbackByNormalizedTime(Action callback)
	{
		if (!_normalizedCallbacks.TryGetValue(callback, out var entry))
		{
			return;
		}

		entry.CancellationTokenSource.Cancel();
		entry.CancellationTokenSource.Dispose();

		_normalizedCallbacks.Remove(callback);
	}

	public void StopTimer()
	{
		if (!IsInProgress)
		{
			return;
		}

		_cancelTimerTokenSource?.Cancel();
		ClearAllCallbacks();

		IsInProgress = false;
	}

	private async void ScheduleCallback(Action callback, CallbackEntry entry)
	{
		try
		{
			var dueMs = (int)(entry.NormalizedTime * _currentDurationMs);
			await Task.Delay(dueMs, entry.CancellationTokenSource.Token);
			if (!entry.CancellationTokenSource.Token.IsCancellationRequested)
			{
				callback();
			}
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			_logger.LogError(ex);
		}
	}

	private async void StartTimerPerMillisecond(int timePerMillisecond, Action onTimerCompleted)
	{
		try
		{
			_currentDurationMs = timePerMillisecond;

			foreach (var kv in _normalizedCallbacks)
			{
				ScheduleCallback(kv.Key, kv.Value);
			}

			IsInProgress = true;
			_cancelTimerTokenSource = new CancellationTokenSource();
			var token = _cancelTimerTokenSource.Token;

			await Task.Delay(timePerMillisecond, token);

			if (token.IsCancellationRequested)
			{
				IsInProgress = false;
				return;
			}

			onTimerCompleted?.Invoke();
			IsInProgress = false;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Timer was canceled");
		}
		catch (Exception e)
		{
			_logger.LogError(e);
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

	private void ClearAllCallbacks()
	{
		foreach (var keyValuePair in _normalizedCallbacks)
		{
			keyValuePair.Value.CancellationTokenSource.CancelAndDispose();
		}

		_normalizedCallbacks.Clear();
	}
}
}