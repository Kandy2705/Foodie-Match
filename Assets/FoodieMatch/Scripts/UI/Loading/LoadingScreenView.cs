using System.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private float _duration = 2f;

        private Tween _progressTween;

        private void OnDestroy()
        {
            StopProgressMotion();
        }

        public async Task PlayAsync()
        {
            if (_progressSlider == null)
            {
                Debug.LogError("Loading progress slider is missing.");
                return;
            }

            if (!IsValidDuration())
            {
                Debug.LogError("Loading duration must be greater than zero.");
                return;
            }

            StopProgressMotion();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            SetProgress(0f);

            try
            {
                _progressTween = Tween.Custom(
                        this,
                        0f,
                        1f,
                        _duration,
                        (view, progress) => view.SetProgress(progress),
                        Ease.Linear)
                    .OnComplete(this, view => view.MarkCompleted());

                await _progressTween;
            }
            finally
            {
                _progressTween = default;
            }
        }

        public void Hide()
        {
            StopProgressMotion();
            SetProgress(0f);
            gameObject.SetActive(false);
        }

        private void SetProgress(float progress)
        {
            if (_progressSlider != null)
            {
                _progressSlider.SetValueWithoutNotify(progress);
            }
        }

        private void MarkCompleted()
        {
            SetProgress(1f);
        }

        private void StopProgressMotion()
        {
            if (_progressTween.isAlive)
            {
                _progressTween.Stop();
            }

            _progressTween = default;
        }

        private bool IsValidDuration()
        {
            return _duration > 0f && !float.IsNaN(_duration) && !float.IsInfinity(_duration);
        }
    }
}
