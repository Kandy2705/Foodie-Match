using FoodieMatch.Features.LevelSystem;
using FoodieMatch.Features.Board;
using FoodieMatch.Features.RequiredPackage;
using FoodieMatch.Features.WaitingRack;
using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class AppRoot : MonoBehaviour
    {
        [Header("Installers")]
        [SerializeField] private AppInstaller _appInstaller;

        [Header("Controllers")]
        [SerializeField] private AppController _appController;

        [SerializeField] private GameplayController _gameplayController;

        [SerializeField] private UIManager _uiManager;

        [Header("Gameplay Roots")]
        [SerializeField] private BoardLayoutView _boardLayoutView;

        [SerializeField] private RequiredPackageGroupView _requiredPackageGroupView;

        [SerializeField] private WaitingRackView _waitingRackView;

        public AppInstaller AppInstaller => _appInstaller;
        public AppController AppController => _appController;
        public GameplayController GameplayController => _gameplayController;
        public UIManager UIManager => _uiManager;
        public BoardLayoutView BoardLayoutView => _boardLayoutView;
        public RequiredPackageGroupView RequiredPackageGroupView => _requiredPackageGroupView;
        public WaitingRackView WaitingRackView => _waitingRackView;

        public void Initialize()
        {
            if (!HasValidReferences())
            {
                return;
            }

            _appInstaller.Install(this);

            _appController.EnterHome();
        }

        private bool HasValidReferences()
        {
            if (_appInstaller == null)
            {
                Debug.LogError("AppInstaller is missing.");
                return false;
            }

            if (_appController == null)
            {
                Debug.LogError("AppController is missing.");
                return false;
            }

            if (_gameplayController == null)
            {
                Debug.LogError("GameplayController is missing.");
                return false;
            }

            if (_uiManager == null)
            {
                Debug.LogError("UIManager is missing.");
                return false;
            }

            if (_boardLayoutView == null)
            {
                Debug.LogError("BoardLayoutView is missing.");
                return false;
            }

            if (_requiredPackageGroupView == null)
            {
                Debug.LogError("RequiredPackageGroupView is missing.");
                return false;
            }

            if (_waitingRackView == null)
            {
                Debug.LogError("WaitingRackView is missing.");
                return false;
            }

            return true;
        }
    }
}

