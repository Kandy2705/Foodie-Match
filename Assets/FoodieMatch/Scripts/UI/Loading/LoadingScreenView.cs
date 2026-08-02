using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Loading
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        private const float InitialProgress = 0.08f;
        private const float ProgressFollowSpeed = 2.5f;
        private const float CompletionFollowSpeed = 12f;
        private const float CompletionThreshold = 0.001f;
        private const float MaximumFrameDelta = 0.05f;

        [SerializeField] private Slider _progressSlider;

        private float _targetProgress;
        private int _showVersion;

        private void Update()
        {
            float frameDelta = Mathf.Min(Time.unscaledDeltaTime, MaximumFrameDelta);
            float followSpeed = _targetProgress >= 1f
                ? CompletionFollowSpeed
                : ProgressFollowSpeed;
            float interpolation = 1f - Mathf.Exp(-followSpeed * frameDelta);
            float visibleProgress = Mathf.Lerp(
                _progressSlider.value,
                _targetProgress,
                interpolation);

            if (Mathf.Abs(_targetProgress - visibleProgress) <= CompletionThreshold)
            {
                visibleProgress = _targetProgress;
            }

            _progressSlider.SetValueWithoutNotify(visibleProgress);
        }

        public void Show()
        {
            _showVersion++;
            _targetProgress = InitialProgress;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _progressSlider.SetValueWithoutNotify(0f);
        }

        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Max(
                _targetProgress,
                Mathf.Clamp01(progress));
        }

        public async Task HideAsync()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            int showVersion = _showVersion;
            _targetProgress = 1f;

            while (this != null &&
                   showVersion == _showVersion &&
                   _progressSlider.value < 1f)
            {
                await Task.Yield();
            }

            if (this == null || showVersion != _showVersion)
            {
                return;
            }

            _progressSlider.SetValueWithoutNotify(1f);
            gameObject.SetActive(false);
        }

        public void HideImmediately()
        {
            _targetProgress = 0f;
            _progressSlider.SetValueWithoutNotify(0f);
            gameObject.SetActive(false);
        }
    }
}
