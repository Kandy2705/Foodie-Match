using System.Collections.Generic;
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
        private float _spoonEnterDuration = 0.24f;

        [SerializeField, Min(0f)]
        private float _scoopGatherDuration = 0.16f;

        [Header("Scoop Motion")]
        [SerializeField]
        private Vector3 _scoopHoverOffset =
            new Vector3(0f, 0.45f, 0f);

        [SerializeField]
        private Vector3 _scoopContactOffset =
            new Vector3(0f, 0f, 0f);

        [SerializeField]
        private Vector3 _scoopFlickOffset =
            new Vector3(0.40f, 0.50f, 0f);

        [SerializeField, Min(0f)]
        private float _scoopLowerDuration = 0.11f;

        [SerializeField, Min(0f)]
        private float _scoopFlickDuration = 0.11f;

        [SerializeField, Min(0f)]
        private float _foodEnterDuration = 0.03f;

        [Header("Scoop Timing")]
        [SerializeField, Min(0f)]
        private float _delayBeforeNextScoop = 0.05f;

        [Header("Fridge Pulse")]
        [SerializeField, Min(1f)]
        private float _fridgePulseScaleMultiplier = 1.2f;

        [SerializeField, Min(0f)]
        private float _fridgePulseGrowDuration = 0.09f;

        [SerializeField, Min(0f)]
        private float _fridgePulseSettleDuration = 0.12f;

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
        private Vector3 _fridgeRendererBaseScale;
        private Vector3 _spoonBaseScale;

        private Sequence _activeSequence;
        private Tween _activeTween;
        private Sequence _fridgeBumpSequence;

        private readonly List<Sequence>
            _foodEnterSequences = new();

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

        private void ShowSpoonAtOutsideLeft()
        {
            if (_spoonRoot == null ||
                _spoonRenderer == null ||
                _spoonExitLeftAnchor == null)
            {
                return;
            }

            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;
            _spoonRoot.position =
                _spoonExitLeftAnchor.position;

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

        public async Task PlayScoopFlickAsync(
            FoodItemView foodItemView,
            Vector3 currentFoodWorldPosition,
            bool hasNextFood,
            Vector3 nextFoodWorldPosition)
        {
            if (foodItemView == null ||
                _spoonRoot == null)
            {
                return;
            }

            foodItemView.SetInteractable(false);

            Vector3 currentHoverPosition =
                currentFoodWorldPosition +
                _scoopHoverOffset;

            Vector3 currentContactPosition =
                currentFoodWorldPosition +
                _scoopContactOffset;

            Vector3 spoonFlickTarget =
                hasNextFood
                    ? nextFoodWorldPosition +
                      _scoopHoverOffset
                    : currentHoverPosition;

            Vector3 foodFlickTarget =
                currentFoodWorldPosition +
                _scoopFlickOffset;

            if (!_spoonRoot.gameObject.activeSelf)
            {
                ShowSpoonAtOutsideLeft();

                _activeTween = Tween.Position(
                    _spoonRoot,
                    currentHoverPosition,
                    _spoonEnterDuration,
                    Ease.OutCubic);

                await _activeTween;
                _activeTween = default;
            }
            else
            {
                float distance =
                    Vector3.Distance(
                        _spoonRoot.position,
                        currentHoverPosition);

                if (distance > 0.02f)
                {
                    _activeTween = Tween.Position(
                        _spoonRoot,
                        currentHoverPosition,
                        0.05f,
                        Ease.OutQuad);

                    await _activeTween;
                    _activeTween = default;
                }
            }

            if (foodItemView == null)
            {
                return;
            }

            _activeTween = Tween.Position(
                _spoonRoot,
                currentContactPosition,
                _scoopLowerDuration,
                Ease.InCubic);

            await _activeTween;
            _activeTween = default;

            if (foodItemView == null)
            {
                return;
            }

            Sequence flickSequence = Sequence.Create(
                Tween.Position(
                    _spoonRoot,
                    spoonFlickTarget,
                    _scoopFlickDuration,
                    Ease.OutCubic));

            _ = flickSequence.Group(
                Tween.Position(
                    foodItemView.transform,
                    foodFlickTarget,
                    _scoopFlickDuration,
                    Ease.OutCubic));

            await flickSequence;
        }

        public async Task WaitBeforeNextScoopAsync()
        {
            if (_delayBeforeNextScoop <= 0f)
            {
                return;
            }

            await Tween.Delay(_delayBeforeNextScoop);
        }

        public async Task PlayFoodEnterAsync(
            FoodItemView foodItemView)
        {
            if (foodItemView == null ||
                _fridgeFoodEntryPoint == null)
            {
                return;
            }

            Vector3 fridgeEntryPosition =
                _fridgeFoodEntryPoint.position;

            Sequence foodSequence = Sequence.Create(
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

            RemoveFinishedFoodSequences();
            _foodEnterSequences.Add(foodSequence);

            await foodSequence;

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

        private void RemoveFinishedFoodSequences()
        {
            for (int i = _foodEnterSequences.Count - 1;
                 i >= 0;
                 i--)
            {
                if (!_foodEnterSequences[i].isAlive)
                {
                    _foodEnterSequences.RemoveAt(i);
                }
            }
        }

        public async Task PlayFridgeBumpAsync()
        {
            if (!IsVisible ||
                _fridgeRenderer == null)
            {
                return;
            }

            if (_fridgeBumpSequence.isAlive)
            {
                _fridgeBumpSequence.Stop();
            }

            Transform pulseTarget =
                _fridgeRenderer.transform;

            pulseTarget.localScale =
                _fridgeRendererBaseScale;

            Vector3 pulseScale =
                _fridgeRendererBaseScale *
                _fridgePulseScaleMultiplier;

            _fridgeBumpSequence = Sequence.Create()
                .Chain(Tween.Scale(
                    pulseTarget,
                    pulseScale,
                    _fridgePulseGrowDuration,
                    Ease.OutBack))
                .Chain(Tween.Scale(
                    pulseTarget,
                    _fridgeRendererBaseScale,
                    _fridgePulseSettleDuration,
                    Ease.OutQuad));

            await _fridgeBumpSequence;
            _fridgeBumpSequence = default;

            if (pulseTarget != null)
            {
                pulseTarget.localScale =
                    _fridgeRendererBaseScale;
            }
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
                _spoonExitLeftAnchor == null ||
                !_spoonRoot.gameObject.activeSelf)
            {
                return;
            }

            _activeTween = Tween.Position(
                _spoonRoot,
                _spoonExitLeftAnchor.position,
                _spoonExitDuration,
                Ease.InOutCubic);

            await _activeTween;
            _activeTween = default;

            HideSpoon();

            _spoonRoot.position =
                _spoonExitLeftAnchor.position;
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

            for (int i = 0;
                 i < _foodEnterSequences.Count;
                 i++)
            {
                Sequence sequence =
                    _foodEnterSequences[i];

                if (sequence.isAlive)
                {
                    sequence.Stop();
                }
            }

            _foodEnterSequences.Clear();

            _activeSequence = default;
            _activeTween = default;
            _fridgeBumpSequence = default;
        }

        private void CaptureBaseScales()
        {
            _fridgeBaseScale = transform.localScale;

            if (_fridgeRenderer != null)
            {
                _fridgeRendererBaseScale =
                    _fridgeRenderer.transform.localScale;
            }

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