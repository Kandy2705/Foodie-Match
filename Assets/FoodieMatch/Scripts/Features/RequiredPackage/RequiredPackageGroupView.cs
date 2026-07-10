using FoodieMatch.Core.Domain.RequiredPackage;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageGroupView : MonoBehaviour
    {
        [SerializeField] private RequiredPackageView[] _packages;

        public int PackageCount => _packages != null ? _packages.Length : 0;

        public RequiredPackageModel[] CreatePackages()
        {
            if (_packages == null)
            {
                return new RequiredPackageModel[0];
            }

            RequiredPackageModel[] packages =
                new RequiredPackageModel[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
            {
                RequiredPackageView packageView = _packages[i];

                packages[i] = packageView != null
                    ? packageView.CreatePackage()
                    : new RequiredPackageModel(0, 0, 0);
            }

            return packages;
        }

        public bool ApplyPackageAt(
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
