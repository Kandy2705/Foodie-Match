namespace FoodieMatch.Core.Domain.Board
{
    public static class StackedGrillRules
    {
        public const int ColumnCount = 3;
        public const int ActiveGrillCount = 5;
        public const int PreviewGrillCount = 1;
        public const int VisibleGrillCount = ActiveGrillCount + PreviewGrillCount;
    }
}
