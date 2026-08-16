using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.AmbientNpc.Application;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;

namespace DungeonTeam.Gameplay.AmbientNpc.Runtime
{
    public sealed class AmbientNpcSet : IDisposable
    {
        private readonly IReadOnlyList<AmbientNpcSnapshot> _snapshots;
        private readonly AmbientNpcProfileCatalog _profiles;
        private readonly Dictionary<string, AmbientNpcPresenterBase> _presenters = new(StringComparer.Ordinal);
        private readonly List<AmbientNpcVignetteController> _vignettes = new();
        private AmbientNpcViewBase[] _views;
        private AmbientNpcVignetteBinding[] _vignetteBindings;
        private bool _initialized;
        private bool _disposed;

        public AmbientNpcSet(
            IReadOnlyList<AmbientNpcSnapshot> snapshots,
            AmbientNpcProfileCatalog profiles,
            AmbientNpcViewBase[] views,
            AmbientNpcVignetteBinding[] vignetteBindings)
        {
            _snapshots = snapshots ?? throw new ArgumentNullException(nameof(snapshots));
            _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            _views = views ?? throw new ArgumentNullException(nameof(views));
            _vignetteBindings = vignetteBindings ?? Array.Empty<AmbientNpcVignetteBinding>();
        }

        public void Initialize()
        {
            if (_disposed || _initialized)
            {
                throw new InvalidOperationException("Ambient NPC set can only be initialized once.");
            }

            ValidateIdSets();
            try
            {
                for (var index = 0; index < _snapshots.Count; index++)
                {
                    var snapshot = _snapshots[index];
                    var view = FindView(snapshot.NpcId);
                    var presenter = new AmbientNpcPresenter(view, new AmbientNpcModel(), _profiles.Require(snapshot.AmbientProfileId));
                    _presenters.Add(snapshot.NpcId, presenter);
                    presenter.Initialize();
                }

                CreateVignettes();
                _initialized = true;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_initialized || _disposed)
            {
                return;
            }

            foreach (var presenter in _presenters.Values)
            {
                presenter.Tick(deltaTime);
            }

            for (var index = 0; index < _vignettes.Count; index++)
            {
                _vignettes[index].Tick(deltaTime);
            }
        }

        public void PauseAndFace(string npcId, UnityEngine.Vector3 playerPosition) =>
            RequirePresenter(npcId).PauseAndFace(playerPosition);

        public void ResumeRoutine(string npcId) => RequirePresenter(npcId).ResumeRoutine();

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            for (var index = _vignettes.Count - 1; index >= 0; index--)
            {
                _vignettes[index].Dispose();
            }

            _vignettes.Clear();
            foreach (var presenter in _presenters.Values)
            {
                presenter.Dispose();
            }

            _presenters.Clear();
            _views = Array.Empty<AmbientNpcViewBase>();
            _vignetteBindings = Array.Empty<AmbientNpcVignetteBinding>();
        }

        private void ValidateIdSets()
        {
            var snapshotIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < _snapshots.Count; index++)
            {
                var snapshot = _snapshots[index] ?? throw new InvalidOperationException(
                    $"Ambient NPC snapshot at index {index} is missing.");
                if (!snapshotIds.Add(snapshot.NpcId))
                {
                    throw new InvalidOperationException($"Ambient NPC ID '{snapshot.NpcId}' is duplicated in config.");
                }
            }

            var viewIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < _views.Length; index++)
            {
                var view = _views[index] ?? throw new InvalidOperationException(
                    $"Ambient NPC view at index {index} is missing.");
                view.ValidateBindings();
                if (!viewIds.Add(view.NpcId))
                {
                    throw new InvalidOperationException($"Ambient NPC ID '{view.NpcId}' is duplicated in prefab.");
                }
            }

            if (!snapshotIds.SetEquals(viewIds))
            {
                throw new InvalidOperationException("Ambient NPC config IDs must exactly match prefab binding IDs.");
            }
        }

        private AmbientNpcViewBase FindView(string npcId)
        {
            for (var index = 0; index < _views.Length; index++)
            {
                if (_views[index].NpcId == npcId)
                {
                    return _views[index];
                }
            }

            throw new InvalidOperationException($"Prefab has no Ambient NPC binding '{npcId}'.");
        }

        private AmbientNpcPresenterBase RequirePresenter(string npcId)
        {
            if (!_presenters.TryGetValue(npcId, out var presenter))
            {
                throw new KeyNotFoundException($"Unknown ambient NPC ID '{npcId}'.");
            }

            return presenter;
        }

        private void CreateVignettes()
        {
            for (var index = 0; index < _vignetteBindings.Length; index++)
            {
                var binding = _vignetteBindings[index] ?? throw new InvalidOperationException(
                    $"Ambient NPC vignette binding at index {index} is missing.");
                binding.Validate(index);
                _vignettes.Add(new AmbientNpcVignetteController(
                    binding,
                    RequirePresenter(binding.FirstNpcId),
                    RequirePresenter(binding.SecondNpcId)));
            }
        }
    }
}
