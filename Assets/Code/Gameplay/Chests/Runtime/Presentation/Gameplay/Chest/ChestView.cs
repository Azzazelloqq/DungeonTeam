using System;
using System.Threading;
using System.Threading.Tasks;
using DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest.Base;
using UnityEngine;

namespace DungeonTeam.Gameplay.Chests.Runtime.Presentation.Gameplay.Chest
{
    public sealed class ChestView : ChestViewBase
    {
        public const string OpenParameter = "Open";

        private static readonly int OpenHash = Animator.StringToHash(OpenParameter);

        [SerializeField]
        private Animator _animator;

        [SerializeField, Tooltip(
            "Point where configured rewards appear. Defaults to the chest root.")]
        private Transform _rewardAnchor;

        public override Vector3 Position => transform.position;

        public override Vector3 RewardPosition =>
            _rewardAnchor != null ? _rewardAnchor.position : transform.position;

        public override void SetOpened(bool isOpened)
        {
            if (isOpened)
            {
                _animator.SetTrigger(OpenHash);
            }
            else
            {
                _animator.ResetTrigger(OpenHash);
            }
        }

        protected override void OnInitialize()
        {
            ValidateBindings();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            ValidateBindings();
            return default;
        }

        protected override void OnDispose()
        {
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }

        private void ValidateBindings()
        {
            if (_animator == null)
            {
                throw new InvalidOperationException(
                    "Chest View requires an Animator binding.");
            }

            if (_animator.applyRootMotion)
            {
                throw new InvalidOperationException(
                    "Chest Animator Root Motion must be disabled.");
            }

            var parameters = _animator.parameters;
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameter = parameters[index];
                if (parameter.nameHash == OpenHash &&
                    parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Chest Animator requires Trigger parameter '{OpenParameter}'.");
        }
    }
}
