using System.Collections.Generic;

namespace FoodieMatch.Core.Domain.RequiredPackage
{
    public sealed class RequiredPackageMatcher
    {
        public bool TryFindBestMatchIndex(
            IReadOnlyList<RequiredPackageState> packages,
            int foodTokenId,
            out int packageIndex)
        {
            packageIndex = -1;

            if (packages == null || foodTokenId <= 0)
            {
                return false;
            }

            int bestRemainingAmount = int.MaxValue;

            for (int i = 0; i < packages.Count; i++)
            {
                RequiredPackageState package = packages[i];

                if (!package.CanAccept(foodTokenId))
                {
                    continue;
                }

                if (package.RemainingAmount >= bestRemainingAmount)
                {
                    continue;
                }

                packageIndex = i;
                bestRemainingAmount = package.RemainingAmount;
            }

            return packageIndex >= 0;
        }
    }
}
