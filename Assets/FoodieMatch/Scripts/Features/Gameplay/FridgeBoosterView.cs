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
        private float _enterFadeDuration = 0.3f;

        [SerializeField, Min(0f)]
        private float _exitDuration = 0.35f;

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

        [Header("Scoop Motion")]
        [SerializeField]
        private Vector3 _scoopHoverOffset =
            new Vector3(0f, 0.25f, 0f);

        [SerializeField]
        private Vector3 _scoopContactOffset =
            new Vector3(0f, 0f, 0f);

        [SerializeField]
        private Vector3 _scoopFlickOffset =
            new Vector3(0.40f, 0.30f, 0f);

        [SerializeField, Min(0f)]
        private float _scoopLowerDuration = 0.1f;

        [SerializeField, Min(0f)]
        private float _scoopFlickDuration = 0.07f;

        [SerializeField, Min(0f)]
        private float _foodEnterDuration = 0.1f;

        [Header("Fridge Bump")]
        [SerializeField, Min(0f)]
        private float _fridgeBumpHeight = 0.12f;

        [SerializeField, Min(0f)]
        private float _fridgeBumpUpDuration = 0.09f;

        [SerializeField, Min(0f)]
        private float _fridgeBumpDownDuration = 0.13f;

        [Header("Release Pop")]
        [SerializeField, Min(0f)]
        private float _releaseGrowDuration = 0.28f;

        [SerializeField, Min(0f)]
        private float _releaseSettleDuration = 0.16f;

        [SerializeField, Range(0f, 1f)]
        private float _releaseStartScaleMultiplier = 0.12f;

        [SerializeField, Min(1f)]
        private float _releaseOvershootMultiplier = 1.18f;

        private Vector3 _fridgeBaseScale;
        private Vector3 _spoonBaseScale;

        private Sequence _activeSequence;
        private Tween _activeTween;
        private Sequence _fridgeBumpSequence;

        public bool IsVisible { get; private set; }

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

            SetFridgeAlpha(0f);

            HideSpoon();
            ResetSpoonPosition();

            IsVisible = false;
        }

        public async Task PlayEnterAndOpenAsync()
        {
            if (!ValidateReferences())
            {
                return;
            }

            SetOffscreen();
            IsVisible = true;

            Vector3 openScale =
                _fridgeBaseScale *
                _openScaleMultiplier;

            _activeSequence = Sequence.Create(
                    Tween.Position(
                        transform,
                        _visibleAnchor.position,
                        _enterDuration,
                        Ease.OutCubic))
                .Group(
                    Tween.Alpha(
                        _fridgeRenderer,
                        1f,
                        _enterFadeDuration,
                        Ease.OutQuad))
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

            SetFridgeAlpha(1f);
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
                _fridgeFoodEntryPoint == null)
            {
                return;
            }

            foodItemView.SetInteractable(false);

            Vector3 hoverPosition =
                waitingRackWorldPosition +
                _scoopHoverOffset;

            Vector3 contactPosition =
                waitingRackWorldPosition +
                _scoopContactOffset;

            Vector3 spoonFlickPosition =
                contactPosition +
                _scoopFlickOffset;

            Vector3 foodFlickPosition =
                waitingRackWorldPosition +
                _scoopFlickOffset;

            Vector3 fridgeEntryPosition =
                _fridgeFoodEntryPoint.position;

            if (!_spoonRoot.gameObject.activeSelf)
            {
                ShowSpoon();

                _spoonRoot.position =
                    hoverPosition;
            }
            else
            {
                _activeTween = Tween.Position(
                    _spoonRoot,
                    hoverPosition,
                    _scoopGatherDuration,
                    Ease.OutCubic);

                await _activeTween;
                _activeTween = default;
            }

            if (foodItemView == null)
            {
                return;
            }

            _activeTween = Tween.Position(
                _spoonRoot,
                contactPosition,
                _scoopLowerDuration,
                Ease.InCubic);

            await _activeTween;
            _activeTween = default;

            if (foodItemView == null)
            {
                return;
            }

            _activeSequence = Sequence.Create(
                    Tween.Position(
                        _spoonRoot,
                        spoonFlickPosition,
                        _scoopFlickDuration,
                        Ease.OutCubic))
                .Group(
                    Tween.Position(
                        foodItemView.transform,
                        foodFlickPosition,
                        _scoopFlickDuration,
                        Ease.OutCubic));

            await _activeSequence;
            _activeSequence = default;

            if (foodItemView == null)
            {
                return;
            }

            _activeSequence = Sequence.Create(
                    Tween.Position(
                        foodItemView.transform,
                        fridgeEntryPosition,
                        _foodEnterDuration,
                        Ease.InOutCubic))
                .Group(
                    Tween.Scale(
                        foodItemView.transform,
                        Vector3.zero,
                        _foodEnterDuration,
                        Ease.InCubic));

            await _activeSequence;
            _activeSequence = default;

            if (foodItemView == null)
            {
                return;
            }

            foodItemView.transform.position =
                fridgeEntryPosition;

            foodItemView.transform.localScale =
                Vector3.zero;

            await PlayFridgeBumpAsync();
        }

        private async Task PlayFridgeBumpAsync()
        {
            if (!IsVisible)
            {
                return;
            }

            if (_fridgeBumpSequence.isAlive)
            {
                _fridgeBumpSequence.Stop();
            }

            Vector3 basePosition = transform.position;

            Vector3 bumpedPosition =
                basePosition +
                Vector3.up * _fridgeBumpHeight;

            _fridgeBumpSequence = Sequence.Create()
                .Chain(Tween.Position(
                    transform,
                    bumpedPosition,
                    _fridgeBumpUpDuration,
                    Ease.OutQuad))
                .Chain(Tween.Position(
                    transform,
                    basePosition,
                    _fridgeBumpDownDuration,
                    Ease.InQuad));

            await _fridgeBumpSequence;
            _fridgeBumpSequence = default;

            transform.position = basePosition;
        }

        public async Task<Vector3> PlayReleasePopAsync(
            FoodItemView foodItemView)
        {
            if (foodItemView == null ||
                _fridgeFoodEntryPoint == null)
            {
                return Vector3.one;
            }

            CancelAnimations();
            SetFullState();

            foodItemView.SetInteractable(false);

            foodItemView.transform.SetParent(
                null,
                worldPositionStays: true);

            foodItemView.transform.position =
                _fridgeFoodEntryPoint.position;

            Vector3 targetScale =
                foodItemView.transform.localScale;

            if (targetScale == Vector3.zero)
            {
                targetScale = Vector3.one;
            }

            foodItemView.transform.localScale =
                targetScale *
                _releaseStartScaleMultiplier;

            await PlayFridgeBumpAsync();

            return targetScale;
        }

        public async Task PlayReleaseGrowAsync(
            FoodItemView foodItemView,
            Vector3 targetScale)
        {
            if (foodItemView == null)
            {
                return;
            }

            Vector3 overshootScale =
                targetScale *
                _releaseOvershootMultiplier;

            Sequence sequence = Sequence.Create()
                .Chain(Tween.Scale(
                    foodItemView.transform,
                    overshootScale,
                    _releaseGrowDuration,
                    Ease.OutCubic))
                .Chain(Tween.Scale(
                    foodItemView.transform,
                    targetScale,
                    _releaseSettleDuration,
                    Ease.OutQuad));

            await sequence;
        }

        public async Task PlaySpoonExitLeftAsync()
        {
            if (_spoonRoot == null ||
                _spoonExitLeftAnchor == null)
            {
                return;
            }

            if (!_spoonRoot.gameObject.activeSelf)
            {
                ShowSpoon();
            }

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
            IsVisible = true;
            SetFridgeAlpha(1f);
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
            IsVisible = true;
            SetFridgeAlpha(1f);
            SetFridgeSprite(_fridgeFullSprite);
            transform.localScale = _fridgeBaseScale;
        }

        public void SetOpenState()
        {
            IsVisible = true;
            SetFridgeAlpha(1f);
            SetFridgeSprite(_fridgeOpenSprite);
            transform.localScale = _fridgeBaseScale;
        }

        public Vector3 GetFridgeEntryWorldPosition()
        {
            return _fridgeFoodEntryPoint != null
                ? _fridgeFoodEntryPoint.position
                : transform.position;
        }

        public async Task PlayDisappearAsync()
        {
            if (!IsVisible)
            {
                HideImmediately();
                return;
            }

            if (_offscreenRightAnchor == null)
            {
                HideImmediately();
                return;
            }

            CancelAnimations();
            HideSpoon();

            SetClosedState();

            _activeTween = Tween.Position(
                transform,
                _offscreenRightAnchor.position,
                _exitDuration,
                Ease.InCubic);

            await _activeTween;
            _activeTween = default;

            HideImmediately();
        }

        public void HideImmediately()
        {
            CancelAnimations();
            HideSpoon();

            if (_fridgeRenderer != null)
            {
                _fridgeRenderer.enabled = false;
            }

            IsVisible = false;
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

            if (_fridgeBumpSequence.isAlive)
            {
                _fridgeBumpSequence.Stop();
            }

            _activeSequence = default;
            _activeTween = default;
            _fridgeBumpSequence = default;
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

        private void SetFridgeAlpha(float alpha)
        {
            if (_fridgeRenderer == null)
            {
                return;
            }

            Color color = _fridgeRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            _fridgeRenderer.color = color;
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