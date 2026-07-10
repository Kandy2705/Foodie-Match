using System.Collections.Generic;

namespace FoodieMatch.Core.Application.UseCases
{
    public sealed class RequiredPackageLifecycleResult
    {
        private readonly List<int> _completedPackageIndexes = new();
        private readonly List<int> _updatedPackageIndexes = new();
        private readonly List<WaitingRackTransfer> _transfers = new();

        public IReadOnlyList<int> CompletedPackageIndexes =>
            _completedPackageIndexes;

        public IReadOnlyList<int> UpdatedPackageIndexes =>
            _updatedPackageIndexes;

        public IReadOnlyList<WaitingRackTransfer> Transfers =>
            _transfers;

        public void AddCompletedPackage(int packageIndex)
        {
            _completedPackageIndexes.Add(packageIndex);
        }

        public void MarkPackageUpdated(int packageIndex)
        {
            if (!_updatedPackageIndexes.Contains(packageIndex))
            {
                _updatedPackageIndexes.Add(packageIndex);
            }
        }

        public void AddTransfer(WaitingRackTransfer transfer)
        {
            _transfers.Add(transfer);
        }
    }
}
