using FoodieMatch.Runtime.UI.Popup;
namespace FoodieMatch.Runtime.UI.Result
{
    public sealed class LosePopupData : IPopupData
    {
        public int LevelId { get; }

        public string Reason { get; }

        public LosePopupData(
            int levelId,
            string reason)
        {
            LevelId = levelId;
            Reason = reason;
        }
    }
}
