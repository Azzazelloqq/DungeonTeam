using System;
using System.Collections.Generic;
using DungeonTeam.Gameplay.ContextActions.Runtime;
using DungeonTeam.Gameplay.GuildHall.Application;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Interaction
{
    public sealed class GuildHallInteractionRequest
    {
        public GuildHallInteractionRequest(string semanticId, GuildInteractionKind kind)
        {
            if (string.IsNullOrWhiteSpace(semanticId))
            {
                throw new ArgumentException("Interaction semantic ID cannot be empty.", nameof(semanticId));
            }

            SemanticId = semanticId;
            Kind = kind;
        }

        public string SemanticId { get; }
        public GuildInteractionKind Kind { get; }
    }

    internal sealed class GuildHallInteractionController : IDisposable
    {
        private readonly Transform _player;
        private readonly GuildHallInteractionPoint[] _points;
        private readonly ContextActionsModel _model;
        private readonly GuildHallCatalog _catalog;
        private readonly Action<GuildHallInteractionRequest> _interactionRequested;
        private readonly Action _worldMapRequested;
        private readonly Action<string> _selectionChanged;
        private readonly float _scanInterval;

        private GuildHallInteractionPoint _current;
        private float _elapsed;
        private bool _isBlocked;
        private bool _isDisposed;

        public GuildHallInteractionController(
            Transform player,
            IReadOnlyList<GuildHallInteractionPoint> points,
            ContextActionsModel model,
            GuildHallCatalog catalog,
            Action<GuildHallInteractionRequest> interactionRequested,
            Action worldMapRequested,
            Action<string> selectionChanged = null)
        {
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _interactionRequested = interactionRequested ??
                throw new ArgumentNullException(nameof(interactionRequested));
            _worldMapRequested = worldMapRequested ?? throw new ArgumentNullException(
                nameof(worldMapRequested));
            _selectionChanged = selectionChanged ?? (_ => { });
            _scanInterval = catalog.Movement.InteractionScanInterval;
            _elapsed = _scanInterval;

            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            _points = new GuildHallInteractionPoint[points.Count];
            var semanticIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < points.Count; index++)
            {
                var point = points[index] ?? throw new InvalidOperationException(
                    $"Guild Hall interaction at index {index} is missing.");
                point.Validate(index);
                if (!semanticIds.Add(point.SemanticId))
                {
                    throw new InvalidOperationException(
                        $"Guild Hall interaction ID '{point.SemanticId}' is duplicated.");
                }

                if (point.Kind == GuildInteractionKind.Npc)
                {
                    catalog.RequireNpc(point.SemanticId);
                }

                _points[index] = point;
            }
        }

        public void SetBlocked(bool isBlocked)
        {
            if (_isDisposed || _isBlocked == isBlocked)
            {
                return;
            }

            _isBlocked = isBlocked;
            if (isBlocked)
            {
                SetCurrent(null);
            }
            else
            {
                _elapsed = _scanInterval;
            }
        }

        public void Tick(float deltaTime)
        {
            if (_isDisposed || _isBlocked)
            {
                return;
            }

            _elapsed += Mathf.Max(0f, deltaTime);
            if (_elapsed < _scanInterval)
            {
                return;
            }

            _elapsed = 0f;
            Scan();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _current = null;
            _model.SetActions(Array.Empty<ContextAction>());
        }

        private void Scan()
        {
            GuildHallInteractionPoint nearest = null;
            var nearestDistance = float.PositiveInfinity;
            var playerPosition = _player.position;
            for (var index = 0; index < _points.Length; index++)
            {
                var point = _points[index];
                var distance = point.SqrDistance(playerPosition);
                if (distance <= point.Radius * point.Radius && distance < nearestDistance)
                {
                    nearest = point;
                    nearestDistance = distance;
                }
            }

            SetCurrent(nearest);
        }

        private void SetCurrent(GuildHallInteractionPoint point)
        {
            if (ReferenceEquals(_current, point))
            {
                return;
            }

            _current = point;
            _selectionChanged(point != null ? point.SemanticId : null);
            if (point == null)
            {
                _model.SetActions(Array.Empty<ContextAction>());
                return;
            }

            var label = _catalog.InteractionLabels.Get(point.Kind).DisplayText;
            _model.SetActions(new[] { new ContextAction(label, ExecuteCurrent) });
        }

        private void ExecuteCurrent()
        {
            var point = _current;
            if (_isDisposed ||
                _isBlocked ||
                point == null ||
                !point.IsInRange(_player.position))
            {
                SetCurrent(null);
                return;
            }

            if (point.Kind == GuildInteractionKind.Exit)
            {
                _worldMapRequested();
                return;
            }

            _interactionRequested(new GuildHallInteractionRequest(point.SemanticId, point.Kind));
        }
    }
}
