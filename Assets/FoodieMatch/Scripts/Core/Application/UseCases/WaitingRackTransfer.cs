namespace FoodieMatch.Core.Application.UseCases
{
    public readonly struct WaitingRackTransfer
    {
        public WaitingRackTransfer(
            int rackSlotIndex,
            int packageIndex,
            int foodTokenId)
        {
            RackSlotIndex = rackSlotIndex;
            PackageIndex = packageIndex;
            FoodTokenId = foodTokenId;
        }

        public int RackSlotIndex { get; }
        public int PackageIndex { get; }
        public int FoodTokenId { get; }
    }
}
