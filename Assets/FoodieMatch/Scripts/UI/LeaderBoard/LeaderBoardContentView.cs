using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class LeaderBoardContentView : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private VerticalLayoutGroup _layoutGroup;
        [SerializeField] private ContentSizeFitter _contentSizeFitter;
        [SerializeField] private LeaderBoardPlayerRowView[] _playerRows;
        [SerializeField] private LeaderBoardPodiumPlayerView[]
            _podiumPlayers;
        [SerializeField] private RectTransform _podiumRoot;
        [SerializeField] private CanvasGroup _podiumCanvasGroup;

        public ScrollRect ScrollRect => _scrollRect;
        public VerticalLayoutGroup LayoutGroup => _layoutGroup;
        public ContentSizeFitter ContentSizeFitter =>
            _contentSizeFitter;
        public LeaderBoardPlayerRowView[] PlayerRows => _playerRows;
        public LeaderBoardPodiumPlayerView[] PodiumPlayers =>
            _podiumPlayers;
        public RectTransform PodiumRoot => _podiumRoot;
        public CanvasGroup PodiumCanvasGroup => _podiumCanvasGroup;
    }
}
