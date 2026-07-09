namespace FoodieMatch.Core.Domain.RequiredPackage
{
    public sealed class RequiredPackageModel
    {
        public RequiredPackageModel(
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
        public int FilledAmount { get; private set; }
        public bool IsEmpty => FoodTokenId <= 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;
        public int RemainingAmount => RequiredAmount - FilledAmount;

        public bool CanAccept(int foodTokenId)
        {
            return !IsEmpty && !IsComplete && FoodTokenId == foodTokenId;
        }

        public bool TryPlaceFood(int foodTokenId)
        {
            if (!CanAccept(foodTokenId))
            {
                return false;
            }

            FilledAmount++;
            return true;
        }
    }
}
