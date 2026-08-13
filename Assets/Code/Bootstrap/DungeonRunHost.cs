using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DungeonTeam.Gameplay.DungeonRun.Application;
using DungeonTeam.Gameplay.DungeonRun.Runtime;

namespace Code.ApplicationRoot
{
    internal sealed class DungeonRunHost : IDisposable
    {
        private readonly Func<DungeonRunStartRequest, DungeonRunRoot> _createRun;
        private DungeonRunRoot _activeRun;
        private bool _isStarting;

        public DungeonRunHost(Func<DungeonRunStartRequest, DungeonRunRoot> createRun)
        {
            _createRun = createRun ?? throw new ArgumentNullException(nameof(createRun));
        }

        public DungeonRunRoot ActiveRun => _activeRun;

        public bool IsBusy => _isStarting || _activeRun != null;

        public async UniTask<DungeonRunRoot> StartAsync(
            DungeonRunStartRequest request,
            CancellationToken token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (IsBusy)
            {
                throw new InvalidOperationException("A dungeon run is already active or starting.");
            }

            _isStarting = true;
            DungeonRunRoot run = null;
            try
            {
                run = _createRun(request) ?? throw new InvalidOperationException(
                    "Dungeon run factory returned no root.");
                _activeRun = run;
                await run.InitializeAsync(token);
                return run;
            }
            catch
            {
                if (ReferenceEquals(_activeRun, run))
                {
                    _activeRun = null;
                }

                run?.Dispose();
                throw;
            }
            finally
            {
                _isStarting = false;
            }
        }

        public void Stop()
        {
            var run = _activeRun;
            _activeRun = null;
            run?.Dispose();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
