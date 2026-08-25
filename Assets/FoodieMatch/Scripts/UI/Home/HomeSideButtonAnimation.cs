using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace FoodieMatch.UI.Home
{
    [DisallowMultipleComponent]
    public sealed class HomeSideButtonAnimation : MonoBehaviour
    {
        private const string IdleAnimationSuffix = "_idle";

        [SerializeField] private SkeletonGraphic _skeletonGraphic;
        [SerializeField, Min(0f)] private float _minimumIdleDuration = 2f;
        [SerializeField, Min(0f)] private float _maximumIdleDuration = 5f;

        private Coroutine _animationRoutine;

        private void OnEnable()
        {
            _animationRoutine = StartCoroutine(PlayAnimationLoop());
        }

        private void OnDisable()
        {
            StopCoroutine(_animationRoutine);
            _animationRoutine = null;
        }

        private IEnumerator PlayAnimationLoop()
        {
            string idleAnimationName = _skeletonGraphic.startingAnimation;
            string jumpAnimationName = idleAnimationName.Substring(
                0,
                idleAnimationName.Length - IdleAnimationSuffix.Length);

            while (true)
            {
                _skeletonGraphic.AnimationState.SetAnimation(
                    trackIndex: 0,
                    idleAnimationName,
                    loop: true);

                yield return new WaitForSecondsRealtime(
                    Random.Range(
                        _minimumIdleDuration,
                        _maximumIdleDuration));

                TrackEntry jump =
                    _skeletonGraphic.AnimationState.SetAnimation(
                        trackIndex: 0,
                        jumpAnimationName,
                        loop: false);

                yield return new WaitForSecondsRealtime(
                    jump.Animation.Duration);
            }
        }
    }
}
