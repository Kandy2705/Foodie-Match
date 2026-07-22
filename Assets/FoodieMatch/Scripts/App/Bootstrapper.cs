using UnityEngine;

namespace FoodieMatch.App
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
            Application.targetFrameRate = 60;
            AppRoot appRoot = Instantiate(_appRootPrefab);
            appRoot.gameObject.name = _appRootPrefab.gameObject.name;
            appRoot.Initialize();
        }
    }
}
