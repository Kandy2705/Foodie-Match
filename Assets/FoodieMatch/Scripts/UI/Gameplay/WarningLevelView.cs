using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Domain.Level;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class WarningLevelView : MonoBehaviour
    {
        private const string HardAnimationName = "hard";
        private const string SuperHardAnimationName = "super_hard";

        [SerializeField] private SkeletonGraphic _warningSkeletonGraphic;

        private TrackEntry _trackEntry;
        private TaskCompletionSource<bool> _animationCompletion;

        private void OnDestroy()
        {
            StopAnimation();
        }

        public async Task PlayAsync(LevelDifficulty difficulty)
        {
            string animationName = difficulty switch
            {
                LevelDifficulty.Hard => HardAnimationName,
                LevelDifficulty.SuperHard => SuperHardAnimationName,
                _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
            };

            StopAnimation();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (!_warningSkeletonGraphic.IsValid)
            {
                _warningSkeletonGraphic.Initialize(overwrite: false);
            }

            _animationCompletion = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _warningSkeletonGraphic.AnimationState.ClearTracks();
            _trackEntry = _warningSkeletonGraphic.AnimationState.SetAnimation(
                0,
                animationName,
                loop: false);
            _trackEntry.Complete += OnAnimationCompleted;

            try
            {
                await _animationCompletion.Task;
            }
            finally
            {
                StopAnimation();
                gameObject.SetActive(false);
            }
        }

        private void OnAnimationCompleted(TrackEntry trackEntry)
        {
            if (trackEntry == _trackEntry)
            {
                _animationCompletion.TrySetResult(true);
            }
        }

        private void StopAnimation()
        {
            if (_trackEntry != null)
            {
                _trackEntry.Complete -= OnAnimationCompleted;
                _trackEntry = null;
            }

            _animationCompletion?.TrySetResult(false);
            _animationCompletion = null;
        }
    }
}
