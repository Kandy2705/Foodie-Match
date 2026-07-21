using System.Threading.Tasks;
using FoodieMatch.Features.Food;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.Gameplay
{
    public sealed class FridgeBoosterView : MonoBehaviour
    {
        [Header("Fridge")]
        [SerializeField] private SpriteRenderer _fridgeRenderer;
        [SerializeField] private Sprite _fridgeCloseSprite;
        [SerializeField] private Sprite _fridgeOpenSprite;
        [SerializeField] private Sprite _fridgeFullSprite;
        [SerializeField] private Transform _fridgeFoodEntryPoint;

        [Header("Spoon")]
        [SerializeField] private SpriteRenderer _spoonRenderer;
        [SerializeField] private Transform _spoonRoot;
        [SerializeField] private Transform _spoonFoodHoldPoint;
        [SerializeField] private Sprite _spoonSprite;

        [Header("Anchors")]
        [SerializeField] private Transform _offscreenRightAnchor;
        [SerializeField] private Transform _visibleAnchor;
        [SerializeField] private Transform _spoonStartAnchor;
        [SerializeField] private Transform _spoonExitLeftAnchor;

        [Header("Animation")]
        [SerializeField, Min(0f)]
        private float _enterDuration = 0.35f;

        [SerializeField, Min(0f)]
        private float _openGrowDuration = 0.14f;

        [SerializeField, Min(0f)]
        private float _openSettleDuration = 0.09f;

        [SerializeField, Min(1f)]
        private float _openScaleMultiplier = 1.2f;

        [SerializeField, Min(0f)]
        private float _spoonExitDuration = 0.3f;

        [SerializeField, Min(0f)]
        private float _scoopGatherDuration = 0.25f;

        [SerializeField, Min(0f)]
        private float _scoopReturnDuration = 0.3f;

        [SerializeField, Min(0f)]
        private float _foodShrinkDuration = 0.15f;

        [SerializeField, Min(0f)]
        private float _releaseGrowDuration = 0.12f;

        [SerializeField, Min(0f)]
        private float _releaseSettleDuration = 0.1f;

        [SerializeField, Min(1f)]
        private float _releaseScaleMultiplier = 1.2f;

        private Vector3 _fridgeBaseScale;
        private Vector3 _spoonBaseScale;

        private Sequence _activeSequence;
        private Tween _activeTween;

        public Transform FridgeFoodEntryPoint =>
            _fridgeFoodEntryPoint;

        private void Awake()
        {
            CaptureBaseScales();
            ValidateReferences();
        }

        private void OnDisable()
        {
            CancelAnimations();
        }

        private void OnDestroy()
        {
            CancelAnimations();
        }

        public void SetOffscreen()
        {
            CancelAnimations();
            CaptureBaseScales();

            gameObject.SetActive(true);

            if (_offscreenRightAnchor != null)
            {
                transform.position =
                    _offscreenRightAnchor.position;
            }

            transform.localScale =
                _fridgeBaseScale;

            SetFridgeSprite(_fridgeCloseSprite);

            if (_fridgeRenderer != null)
            {
                _fridgeRenderer.enabled = true;
            }

            HideSpoon();
            ResetSpoonPosition();
        }

        public async Task PlayEnterAndOpenAsync()
        {
            if (!ValidateReferences())
            {
                return;
            }

            SetOffscreen();

            Vector3 openScale =
                _fridgeBaseScale *
                _openScaleMultiplier;

            _activeSequence = Sequence.Create()
                .Chain(Tween.Position(
                    transform,
                    _visibleAnchor.position,
                    _enterDuration,
                    Ease.OutCubic))
                .ChainCallback(
                    this,
                    view => view.SetFridgeSprite(
                        view._fridgeOpenSprite))
                .Chain(Tween.Scale(
                    transform,
                    openScale,
                    _openGrowDuration,
                    Ease.OutBack))
                .Chain(Tween.Scale(
                    transform,
                    _fridgeBaseScale,
                    _openSettleDuration,
                    Ease.OutQuad));

            await _activeSequence;
            _activeSequence = default;
        }

        public void ShowSpoon()
        {
            if (_spoonRoot == null ||
                _spoonRenderer == null)
            {
                return;
            }

            ResetSpoonPosition();

            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;

            _spoonRenderer.sprite = _spoonSprite;
            _spoonRenderer.enabled = true;
        }

        public void HideSpoon()
        {
            if (_spoonRenderer != null)
            {
                _spoonRenderer.enabled = false;
            }

            if (_spoonRoot != null)
            {
                _spoonRoot.gameObject.SetActive(false);
            }
        }

        public async Task PlayScoopFoodAsync(
            FoodItemView foodItemView,
            Vector3 waitingRackWorldPosition)
        {
            if (foodItemView == null ||
                _spoonRoot == null ||
                _spoonFoodHoldPoint == null ||
                _fridgeFoodEntryPoint == null)
            {
                return;
            }

            ShowSpoon();

            _activeTween = Tween.Position(
                _spoonRoot,
                waitingRackWorldPosition,
                _scoopGatherDuration,
                Ease.OutCubic);

            await _activeTween;
            _activeTween = default;

            if (foodItemView == null)
            {
                return;
            }

            foodItemView.SetInteractable(false);

            foodItemView.transform.SetParent(
                _spoonFoodHoldPoint,
                worldPositionStays: true);

            foodItemView.transform.SetPositionAndRotation(
                _spoonFoodHoldPoint.position,
                _spoonFoodHoldPoint.rotation);

            _activeTween = Tween.Position(
                _spoonRoot,
                _fridgeFoodEntryPoint.position,
                _scoopReturnDuration,
                Ease.InOutCubic);

            await _activeTween;
            _activeTween = default;

            if (foodItemView == null)
            {
                return;
            }

            foodItemView.transform.SetParent(
                null,
                worldPositionStays: true);

            foodItemView.transform.position =
                _fridgeFoodEntryPoint.position;

            _activeTween = Tween.Scale(
                foodItemView.transform,
                Vector3.zero,
                _foodShrinkDuration,
                Ease.InBack);

            await _activeTween;
            _activeTween = default;
        }

        public async Task PlayReleasePopAsync(
            FoodItemView foodItemView)
        {
            if (foodItemView == null ||
                _fridgeFoodEntryPoint == null)
            {
                return;
            }

            CancelAnimations();
            SetOpenState();

            foodItemView.SetInteractable(false);
            foodItemView.transform.position =
                _fridgeFoodEntryPoint.position;

            Vector3 targetScale =
                foodItemView.transform.localScale;

            if (targetScale == Vector3.zero)
            {
                targetScale = Vector3.one;
            }

            Vector3 growScale =
                targetScale * _releaseScaleMultiplier;

            foodItemView.transform.localScale =
                Vector3.zero;

            _activeSequence = Sequence.Create()
                .Chain(Tween.Scale(
                    foodItemView.transform,
                    growScale,
                    _releaseGrowDuration,
                    Ease.OutBack))
                .Chain(Tween.Scale(
                    foodItemView.transform,
                    targetScale,
                    _releaseSettleDuration,
                    Ease.OutQuad));

            await _activeSequence;
            _activeSequence = default;
        }

        public async Task PlaySpoonExitLeftAsync()
        {
            if (_spoonRoot == null ||
                _spoonExitLeftAnchor == null)
            {
                return;
            }

            ShowSpoon();

            _activeTween = Tween.Position(
                _spoonRoot,
                _spoonExitLeftAnchor.position,
                _spoonExitDuration,
                Ease.InOutCubic);

            await _activeTween;
            _activeTween = default;

            HideSpoon();
            ResetSpoonPosition();
        }

        public void SetClosedState()
        {
            SetFridgeSprite(_fridgeCloseSprite);

            if (_visibleAnchor != null)
            {
                transform.position =
                    _visibleAnchor.position;
            }

            transform.localScale =
                _fridgeBaseScale;
        }

        public void SetFullState()
        {
            SetFridgeSprite(_fridgeFullSprite);
            transform.localScale = _fridgeBaseScale;
        }

        public void SetOpenState()
        {
            SetFridgeSprite(_fridgeOpenSprite);
            transform.localScale = _fridgeBaseScale;
        }

        public Vector3 GetFridgeEntryWorldPosition()
        {
            return _fridgeFoodEntryPoint != null
                ? _fridgeFoodEntryPoint.position
                : transform.position;
        }

        public void HideImmediately()
        {
            CancelAnimations();
            HideSpoon();

            if (_fridgeRenderer != null)
            {
                _fridgeRenderer.enabled = false;
            }
        }

        public void CancelAnimations()
        {
            if (_activeSequence.isAlive)
            {
                _activeSequence.Stop();
            }

            if (_activeTween.isAlive)
            {
                _activeTween.Stop();
            }

            _activeSequence = default;
            _activeTween = default;
        }

        private void CaptureBaseScales()
        {
            _fridgeBaseScale = transform.localScale;

            if (_spoonRoot != null)
            {
                _spoonBaseScale =
                    _spoonRoot.localScale;
            }
        }

        private void ResetSpoonPosition()
        {
            if (_spoonRoot == null)
            {
                return;
            }

            if (_spoonStartAnchor != null)
            {
                _spoonRoot.position =
                    _spoonStartAnchor.position;
            }

            _spoonRoot.localScale =
                _spoonBaseScale;
        }

        private void SetFridgeSprite(Sprite sprite)
        {
            if (_fridgeRenderer == null)
            {
                return;
            }

            _fridgeRenderer.sprite = sprite;
            _fridgeRenderer.enabled =
                sprite != null;
        }

        private bool ValidateReferences()
        {
            bool valid = true;

            valid &= ValidateReference(
                _fridgeRenderer,
                nameof(_fridgeRenderer));

            valid &= ValidateReference(
                _fridgeCloseSprite,
                nameof(_fridgeCloseSprite));

            valid &= ValidateReference(
                _fridgeOpenSprite,
                nameof(_fridgeOpenSprite));

            valid &= ValidateReference(
                _fridgeFullSprite,
                nameof(_fridgeFullSprite));

            valid &= ValidateReference(
                _fridgeFoodEntryPoint,
                nameof(_fridgeFoodEntryPoint));

            valid &= ValidateReference(
                _spoonRenderer,
                nameof(_spoonRenderer));

            valid &= ValidateReference(
                _spoonRoot,
                nameof(_spoonRoot));

            valid &= ValidateReference(
                _spoonFoodHoldPoint,
                nameof(_spoonFoodHoldPoint));

            valid &= ValidateReference(
                _spoonSprite,
                nameof(_spoonSprite));

            valid &= ValidateReference(
                _offscreenRightAnchor,
                nameof(_offscreenRightAnchor));

            valid &= ValidateReference(
                _visibleAnchor,
                nameof(_visibleAnchor));

            valid &= ValidateReference(
                _spoonStartAnchor,
                nameof(_spoonStartAnchor));

            valid &= ValidateReference(
                _spoonExitLeftAnchor,
                nameof(_spoonExitLeftAnchor));

            return valid;
        }

        private bool ValidateReference(
            Object reference,
            string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError(
                $"FridgeBoosterView: {fieldName} is missing.",
                this);

            return false;
        }
    }
}