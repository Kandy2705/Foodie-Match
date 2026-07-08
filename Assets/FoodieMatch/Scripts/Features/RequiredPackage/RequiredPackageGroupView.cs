using FoodieMatch.Core.Domain.RequiredPackage;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageGroupView : MonoBehaviour
    {
        [SerializeField] private RequiredPackageView[] _packages;

        public int PackageCount => _packages != null ? _packages.Length : 0;

        public RequiredPackageState[] GetStates()
        {
            if (_packages == null)
            {
                return new RequiredPackageState[0];
            }

            RequiredPackageState[] states = new RequiredPackageState[_packages.Length];

            for (int i = 0; i < _packages.Length; i++)
            {
                if (_packages[i] != null)
                {
                    states[i] = _packages[i].GetState();
                }
            }

            return states;
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
