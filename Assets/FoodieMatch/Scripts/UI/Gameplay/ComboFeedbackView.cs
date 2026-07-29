using System;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    public sealed class ComboFeedbackView : MonoBehaviour
    {
        private static readonly string[] AnimationNames =
        {
            "amazing",
            "awesome",
            "fantastic",
            "good_job",
            "nice"
        };

        [SerializeField] private SkeletonGraphic _skeletonGraphic;

        private TrackEntry _trackEntry;
        private Action<ComboFeedbackView> _completed;

        private void OnDestroy()
        {
            StopListeningForCompletion();
        }

        public void ResetForUse()
        {
            ResetAnimation();
        }

        public void ResetForPool()
        {
            ResetAnimation();
        }

        public void PlayRandomAnimation(
            Action<ComboFeedbackView> completed)
        {
            _completed = completed;

            if (!_skeletonGraphic.IsValid)
            {
                _skeletonGraphic.Initialize(overwrite: false);
            }

            if (_skeletonGraphic.AnimationState == null)
            {
                Debug.LogError("Combo feedback AnimationState is missing.", this);
                CompletePlayback();
                return;
            }

            StopListeningForCompletion();

            try
            {
                int animationIndex = UnityEngine.Random.Range(0, AnimationNames.Length);
                _skeletonGraphic.AnimationState.ClearTracks();
                _trackEntry = _skeletonGraphic.AnimationState.SetAnimation(
                    0,
                    AnimationNames[animationIndex],
                    loop: false);
                _trackEntry.Complete += HandleAnimationCompleted;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                CompletePlayback();
            }
        }

        private void HandleAnimationCompleted(TrackEntry trackEntry)
        {
            if (trackEntry != _trackEntry)
            {
                return;
            }

            CompletePlayback();
        }

        private void CompletePlayback()
        {
            StopListeningForCompletion();

            Action<ComboFeedbackView> completed = _completed;
            _completed = null;
            completed?.Invoke(this);
        }

        private void ResetAnimation()
        {
            StopListeningForCompletion();
            _completed = null;

            if (_skeletonGraphic.AnimationState != null)
            {
                _skeletonGraphic.AnimationState.ClearTracks();
            }

            _skeletonGraphic.Skeleton?.SetToSetupPose();
        }

        private void StopListeningForCompletion()
        {
            if (_trackEntry == null)
            {
                return;
            }

            _trackEntry.Complete -= HandleAnimationCompleted;
            _trackEntry = null;
        }
    }
}
