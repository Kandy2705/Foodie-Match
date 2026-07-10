using FoodieMatch.Core.Domain.RequiredPackage;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageGroupView : MonoBehaviour
    {
        [SerializeField] private RequiredPackageView[] _packages;

        public int PackageCount => _packages != null ? _packages.Length : 0;

        public bool ShowPackageAt(
            int packageIndex,
            RequiredPackageModel package,
            Sprite sprite)
        {
            RequiredPackageView packageView = GetPackageAt(packageIndex);

            if (packageView == null)
            {
                return false;
            }

            if (package == null || package.IsEmpty)
            {
                packageView.Clear();
                return true;
            }

            packageView.Setup(
                package.FoodTokenId,
                package.RequiredAmount,
                sprite);
            packageView.SetFilledAmount(package.FilledAmount);

            return true;
        }

        public bool UpdateFilledAmountAt(
            int packageIndex,
            RequiredPackageModel package)
        {
            RequiredPackageView packageView = GetPackageAt(packageIndex);

            if (packageView == null || package == null)
            {
                return false;
            }

            packageView.SetFilledAmount(package.FilledAmount);
            return true;
        }

        public RequiredPackageView GetPackageAt(int packageIndex)
        {
            if (_packages == null || packageIndex < 0 || packageIndex >= _packages.Length)
            {
                return null;
            }

            return _packages[packageIndex];
        }
    }
}
