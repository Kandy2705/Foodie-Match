using FoodieMatch.Core.Application.Configuration.GoldPass;
using FoodieMatch.Core.Application.GoldPass;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.UI.GoldPass
{
    public sealed class GoldPassRewardPreviewView : MonoBehaviour
    {
        [SerializeField] private RectTransform _pointerImage;
        [SerializeField] private GoldPassRewardItemView[] _rewardItems;
        [SerializeField] private float _freePointerX = -74f;
        [SerializeField] private float _seasonPointerX = 74f;
        [SerializeField, Min(0f)] private float _visibleDuration = 3f;

        private Tween _hideTween;
        private int _sourceMilestoneLevel;
        private GoldPassTrack _sourceTrack;
        private bool _isVisible;

        public void Toggle(
            int milestoneLevel,
            GoldPassTrack track,
            GoldPassRewardDefinition treasure,
            GoldPassRewardVisualCatalogSO visualCatalog)
        {
            if (_isVisible &&
                _sourceMilestoneLevel == milestoneLevel &&
                _sourceTrack == track)
            {
                Hide();
                return;
            }

            StopHideTimer();
            _sourceMilestoneLevel = milestoneLevel;
            _sourceTrack = track;
            _isVisible = true;

            Vector2 pointerPosition = _pointerImage.anchoredPosition;
            pointerPosition.x = track == GoldPassTrack.Free
                ? _freePointerX
                : _seasonPointerX;
            _pointerImage.anchoredPosition = pointerPosition;

            for (int i = 0; i < _rewardItems.Length; i++)
            {
                if (i < treasure.Contents.Count)
                {
                    _rewardItems[i].Bind(
                        treasure.Contents[i],
                        visualCatalog);
                    continue;
                }

                _rewardItems[i].gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            _hideTween = Tween.Delay(
                this,
                _visibleDuration,
                view => view.Hide(),
                useUnscaledTime: true);
        }

        public void Hide()
        {
            StopHideTimer();
            _isVisible = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            StopHideTimer();
        }

        private void StopHideTimer()
        {
            if (_hideTween.isAlive)
            {
                _hideTween.Stop();
            }

            _hideTween = default;
        }
    }
}
