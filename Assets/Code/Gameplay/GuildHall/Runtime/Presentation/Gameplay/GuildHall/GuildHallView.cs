using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.ContextActions.Runtime.Base;
using DungeonTeam.Gameplay.GuildHall.Application;
using DungeonTeam.Gameplay.GuildHall.Runtime.Interaction;
using DungeonTeam.Gameplay.AmbientNpc.Runtime;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.Gameplay.AmbientNpc.Base;
using DungeonTeam.Gameplay.AmbientNpc.Runtime.Presentation.UI.Dialogue.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.NoticeBoard;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.RunSummary;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.GuildProfile.Base;
using DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.UI.QuestRewardCollection.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.GuildHall.Runtime.Presentation.Gameplay.GuildHall
{
    public sealed class GuildHallView : GuildHallViewBase
    {
        [SerializeField]
        private CharacterController _playerController;

        [SerializeField]
        private Transform _player;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private Transform _playerSpawn;

        [SerializeField]
        private ContextActionsViewBase _contextActionsView;

        [SerializeField]
        private GuildHallInteractionPoint[] _interactionPoints =
            Array.Empty<GuildHallInteractionPoint>();

        [SerializeField] private AmbientNpcViewBase[] _ambientNpcViews = Array.Empty<AmbientNpcViewBase>();
        [SerializeField] private AmbientNpcVignetteBinding[] _ambientNpcVignettes = Array.Empty<AmbientNpcVignetteBinding>();
        [SerializeField] private DialogueViewBase _dialogueView;
        [SerializeField] private NoticeBoardViewBase _noticeBoardView;
        [SerializeField] private RunSummaryViewBase _runSummaryView;
        [SerializeField] private GuildProfileViewBase _guildProfileView;
        [SerializeField] private QuestRewardCollectionViewBase _questRewardCollectionView;

        public override Transform PlayerTransform => _player;
        public override Transform CameraTransform => _camera != null ? _camera.transform : null;
        public override ContextActionsViewBase ContextActionsView => _contextActionsView;
        public override GuildHallInteractionPoint[] InteractionPoints => _interactionPoints;
        public override AmbientNpcViewBase[] AmbientNpcViews => _ambientNpcViews;
        public override AmbientNpcVignetteBinding[] AmbientNpcVignettes => _ambientNpcVignettes;
        public override DialogueViewBase DialogueView => _dialogueView;
        public override NoticeBoardViewBase NoticeBoardView => _noticeBoardView;
        public override RunSummaryViewBase RunSummaryView => _runSummaryView;
        public override GuildProfileViewBase GuildProfileView => _guildProfileView;
        public override QuestRewardCollectionViewBase QuestRewardCollectionView => _questRewardCollectionView;

        public override void ValidateBindings()
        {
            if (_playerController == null || _player == null || _camera == null || _playerSpawn == null)
            {
                throw new InvalidOperationException(
                    "Guild Hall player, controller, camera and spawn bindings are required.");
            }

            if (_contextActionsView == null)
            {
                throw new InvalidOperationException("Guild Hall ContextActions view is required.");
            }

            if (_interactionPoints == null)
            {
                throw new InvalidOperationException("Guild Hall interaction bindings cannot be null.");
            }

            if (_ambientNpcViews == null || _ambientNpcVignettes == null)
            {
                throw new InvalidOperationException("Guild Hall ambient NPC bindings cannot be null.");
            }

            if (_noticeBoardView == null)
            {
                throw new InvalidOperationException("Guild Hall Notice Board view is required.");
            }

            _noticeBoardView.ValidateBindings();

            if (_runSummaryView == null)
            {
                throw new InvalidOperationException("Guild Hall Run Summary view is required.");
            }

            _runSummaryView.ValidateBindings();

            if (_guildProfileView == null)
            {
                throw new InvalidOperationException("Guild Hall Profile view is required.");
            }

            _guildProfileView.ValidateBindings();

            if (_questRewardCollectionView != null)
            {
                _questRewardCollectionView.ValidateBindings();
            }

            var kinds = new HashSet<GuildInteractionKind>();
            for (var index = 0; index < _interactionPoints.Length; index++)
            {
                var point = _interactionPoints[index] ?? throw new InvalidOperationException(
                    $"Guild Hall interaction binding at index {index} is missing.");
                point.Validate(index);
                kinds.Add(point.Kind);
            }

            foreach (GuildInteractionKind kind in Enum.GetValues(typeof(GuildInteractionKind)))
            {
                if (!kinds.Contains(kind))
                {
                    throw new InvalidOperationException(
                        $"Guild Hall prefab has no authored interaction of kind '{kind}'.");
                }
            }
        }

        public override void ResetPlayer()
        {
            _playerController.enabled = false;
            _player.SetPositionAndRotation(_playerSpawn.position, _playerSpawn.rotation);
            _playerController.enabled = true;
        }

        public override void Move(Vector3 displacement)
        {
            _playerController.Move(displacement);
        }

        protected override void OnInitialize()
        {
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
