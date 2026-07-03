using UnityEngine;

namespace FoodieMatch.Runtime.App
{
    public sealed class Bootstrapper : MonoBehaviour
    {
        [SerializeField] private AppRoot _appRootPrefab;

        private void Awake()
        {
            CreateAppRoot();
        }

        private void CreateAppRoot()
        {
            if (_appRootPrefab == null)
            {
                Debug.LogError("AppRoot prefab is missing.");
                return;
            }

            AppRoot appRoot = Instantiate(_appRootPrefab);
            appRoot.gameObject.name = _appRootPrefab.gameObject.name;
            appRoot.Initialize();
        }
    }
}
