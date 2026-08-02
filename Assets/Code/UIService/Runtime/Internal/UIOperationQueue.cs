using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.UIService
{
    internal sealed class UIOperationQueue
    {
        private readonly object _sync = new();
        private readonly Queue<Operation> _operations = new();

        private bool _isRunning;

        public UniTask Enqueue(Func<UniTask> operation, CancellationToken token)
        {
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            var completion = new UniTaskCompletionSource();
            var startRunner = false;

            lock (_sync)
            {
                _operations.Enqueue(new Operation(operation, token, completion));

                if (!_isRunning)
                {
                    _isRunning = true;
                    startRunner = true;
                }
            }

            if (startRunner)
                RunAsync().Forget(Debug.LogException);

            return completion.Task;
        }

        private async UniTask RunAsync()
        {
            while (true)
            {
                Operation operation;

                lock (_sync)
                {
                    if (_operations.Count == 0)
                    {
                        _isRunning = false;
                        return;
                    }

                    operation = _operations.Dequeue();
                }

                try
                {
                    operation.Token.ThrowIfCancellationRequested();
                    await operation.Callback();
                    operation.Completion.TrySetResult();
                }
                catch (OperationCanceledException exception)
                {
                    operation.Completion.TrySetCanceled(exception.CancellationToken);
                }
                catch (Exception exception)
                {
                    operation.Completion.TrySetException(exception);
                }
            }
        }

        private readonly struct Operation
        {
            public Operation(
                Func<UniTask> callback,
                CancellationToken token,
                UniTaskCompletionSource completion)
            {
                Callback = callback;
                Token = token;
                Completion = completion;
            }

            public Func<UniTask> Callback { get; }

            public CancellationToken Token { get; }

            public UniTaskCompletionSource Completion { get; }
        }
    }
}
