namespace FoodieMatch.Core.Domain.RequiredPackage
{
    public readonly struct RequiredPackageState
    {
        public RequiredPackageState(
            int foodTokenId,
            int requiredAmount,
            int filledAmount)
        {
            FoodTokenId = foodTokenId;
            RequiredAmount = requiredAmount;
            FilledAmount = filledAmount;
        }

        public int FoodTokenId { get; }
        public int RequiredAmount { get; }
        public int FilledAmount { get; }
        public bool IsEmpty => FoodTokenId <= 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;
        public int RemainingAmount => RequiredAmount - FilledAmount;

        public bool CanAccept(int foodTokenId)
        {
            return !IsEmpty && !IsComplete && FoodTokenId == foodTokenId;
        }
    }
}
