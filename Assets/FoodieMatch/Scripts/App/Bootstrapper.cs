using UnityEngine;
using PrimeTween;

namespace FoodieMatch.App
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private AppRoot _appRootPrefab;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            PrimeTweenConfig.SetTweensCapacity(800);
            CreateAppRoot();
        }

        private void CreateAppRoot()
        {
            AppRoot appRoot = Instantiate(_appRootPrefab);
            appRoot.gameObject.name = _appRootPrefab.gameObject.name;
            appRoot.Initialize();
        }
    }
}
