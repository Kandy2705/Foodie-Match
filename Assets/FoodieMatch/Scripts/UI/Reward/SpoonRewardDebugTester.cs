using FoodieMatch.UI;
using UnityEngine;

namespace FoodieMatch.UI.Reward
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIManager))]
    public sealed class SpoonRewardDebugTester : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _spoonCount = 3;

        private UIManager _uiManager;

        private void Awake()
        {
            _uiManager = GetComponent<UIManager>();
        }

        public void Play()
        {
            _uiManager.PlayHomeSpoonReward(_spoonCount);
        }
    }
}
