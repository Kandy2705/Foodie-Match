using System;
using System.Threading.Tasks;
using FoodieMatch.Core.Application.Shop;
using FoodieMatch.UI.Packages;
using UnityEngine;

namespace FoodieMatch.UI.StarterPack
{
    [DisallowMultipleComponent]
    public sealed class StarterPackPopupView : PackagePopupViewBase
    {
        public void SetActions(StarterPackPopupViewActions actions)
        {
            base.SetActions(actions);
        }
    }
}
