namespace FoodieMatch.Core.Domain.Board
{
    public static class StackedGrillRules
    {
        public const int ColumnCount = 3;
        public const int AccessibleGrillCount = 5;
        public const int PreviewGrillCount = 1;
        public const int VisibleGrillCount = AccessibleGrillCount + PreviewGrillCount;
    }
}
