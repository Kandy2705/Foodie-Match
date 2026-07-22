namespace FoodieMatch.Core.Application.UseCases
{
    public readonly struct FridgeTransfer
    {
        public FridgeTransfer(
            int packageIndex,
            int foodTokenId)
        {
            PackageIndex = packageIndex;
            FoodTokenId = foodTokenId;
        }

        public int PackageIndex { get; }
        public int FoodTokenId { get; }
    }
}
