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

        private void Awake()
        {
            EnsureSkeletonGraphic();
        }

        private void OnDestroy()
        {
            StopListeningForCompletion();
        }

        private void OnDisable()
        {
            if (_trackEntry == null)
            {
                return;
            }

            StopListeningForCompletion();
            Destroy(gameObject);
        }

        public void PlayRandomAnimation()
        {
            EnsureSkeletonGraphic();

            if (_skeletonGraphic == null)
            {
                Debug.LogError("Combo feedback SkeletonGraphic is missing.", this);
                Destroy(gameObject);
                return;
            }

            if (!_skeletonGraphic.IsValid)
            {
                _skeletonGraphic.Initialize(overwrite: false);
            }

            if (_skeletonGraphic.AnimationState == null)
            {
                Debug.LogError("Combo feedback AnimationState is missing.", this);
                Destroy(gameObject);
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
                StopListeningForCompletion();
                Destroy(gameObject);
            }
        }

        private void HandleAnimationCompleted(TrackEntry trackEntry)
        {
            if (trackEntry != _trackEntry)
            {
                return;
            }

            StopListeningForCompletion();
            Destroy(gameObject);
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

        private void EnsureSkeletonGraphic()
        {
            if (_skeletonGraphic == null)
            {
                _skeletonGraphic = GetComponentInChildren<SkeletonGraphic>(includeInactive: true);
            }
        }
    }
}
