using FoodieMatch.Features.Board;
using FoodieMatch.Features.Effects;
using FoodieMatch.Features.Food;
using FoodieMatch.Shared.Pooling;
using FoodieMatch.UI.Gameplay;
using UnityEngine;

namespace FoodieMatch.App
{
    public sealed class GameplayPoolRoot : MonoBehaviour
    {
        [SerializeField] private FoodItemViewPool _foodItems;
        [SerializeField] private GrillViewPool _grills;
        [SerializeField] private TrayViewPool _trays;
        [SerializeField] private ParticleEffectPool _smokeParticles;
        [SerializeField] private ParticleEffectPool _packageCompleteBursts;
        [SerializeField] private ComboFeedbackViewPool _comboFeedback;

        private IPoolLifecycle[] _poolLifecycles;

        public FoodItemViewPool FoodItems => _foodItems;
        public GrillViewPool Grills => _grills;
        public TrayViewPool Trays => _trays;
        public ParticleEffectPool SmokeParticles => _smokeParticles;
        public ParticleEffectPool PackageCompleteBursts => _packageCompleteBursts;
        public ComboFeedbackViewPool ComboFeedback => _comboFeedback;

        private void OnDestroy()
        {
            Application.lowMemory -= Clear;
        }

        public void Initialize()
        {
            _poolLifecycles = new IPoolLifecycle[]
            {
                _foodItems,
                _grills,
                _trays,
                _smokeParticles,
                _packageCompleteBursts,
                _comboFeedback
            };

            foreach (IPoolLifecycle poolLifecycle in _poolLifecycles)
            {
                poolLifecycle.Initialize();
            }

            Application.lowMemory += Clear;
        }

        public void Clear()
        {
            foreach (IPoolLifecycle poolLifecycle in _poolLifecycles)
            {
                poolLifecycle.Clear();
            }
        }
    }
}
