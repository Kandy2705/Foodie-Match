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
        [SerializeField] private Sprite _spoonSprite;

        [Header("Anchors")]
        [SerializeField] private Transform _offscreenRightAnchor;
        [SerializeField] private Transform _visibleAnchor;

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

        [Header("Scoop Motion")]
        [SerializeField]
        private Vector3 _scoopHoverOffset =
            new Vector3(0f, 0.45f, 0f);

        [SerializeField]
        private Vector3 _scoopContactOffset =
            new Vector3(0f, 0f, 0f);

        [SerializeField, Min(0f)]
        private float _spoonAppearDuration = 0.24f;

        [SerializeField]
        private Ease _spoonAppearEase = Ease.OutCubic;

        [SerializeField, Min(0f)]
        private float _spoonMoveDuration = 0.12f;

        [SerializeField, Min(0f)]
        private float _scoopLowerDuration = 0.11f;

        [SerializeField, Min(0f)]
        private float _scoopLiftDuration = 0.14f;

        [SerializeField]
        private Ease _scoopLiftEase = Ease.OutCubic;

        [SerializeField, Min(0f)]
        private float _spoonReturnDuration = 0.2f;

        [Header("Food Flight")]
        [SerializeField, Min(0f)]
        private float _foodFlightDuration = 0.3f;

        [SerializeField, Min(0f)]
        private float _foodFlightArcHeight = 1f;

        [SerializeField, Range(0.1f, 0.9f)]
        private float _foodFlightPeakProgress = 0.45f;

        [SerializeField]
        private Ease _foodFlightScaleEase = Ease.InCubic;

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
        private int _fridgeBumpVersion;

        private readonly List<Sequence>
            _foodFlightSequences = new();

        public bool IsVisible { get; private set; }

        public Transform FridgeFoodEntryPoint =>
            _fridgeFoodEntryPoint;

        private void Awake()
        {
            CaptureBaseScales();
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
            transform.position = _offscreenRightAnchor.position;
            transform.localScale = _fridgeBaseScale;
            SetFridgeSprite(_fridgeCloseSprite);
            _fridgeRenderer.enabled = true;
            SetFridgeAlpha(0f);

            HideSpoon();

            IsVisible = false;
        }

        public async Task PlayEnterAndOpenAsync()
        {
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

        public async Task PlaySpoonAppearAsync(
            Vector3 restFoodAnchorPosition)
        {
            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;
            _spoonRoot.position = restFoodAnchorPosition;
            _spoonRenderer.sprite = _spoonSprite;
            _spoonRenderer.enabled = true;

            await PlaySpoonPositionAsync(
                restFoodAnchorPosition + _scoopHoverOffset,
                _spoonAppearDuration,
                _spoonAppearEase);
        }

        public void HideSpoon()
        {
            _spoonRenderer.enabled = false;
            _spoonRoot.gameObject.SetActive(false);
        }

        public Task PlaySpoonMoveToFoodAsync(
            Vector3 foodAnchorPosition)
        {
            Vector3 targetPosition =
                foodAnchorPosition + _scoopHoverOffset;

            if (Vector3.Distance(
                    _spoonRoot.position,
                    targetPosition) <= 0.02f)
            {
                return Task.CompletedTask;
            }

            return PlaySpoonPositionAsync(
                targetPosition,
                _spoonMoveDuration,
                Ease.InOutCubic);
        }

        public Task PlaySpoonLowerAsync(
            Vector3 foodAnchorPosition)
        {
            return PlaySpoonPositionAsync(
                foodAnchorPosition + _scoopContactOffset,
                _scoopLowerDuration,
                Ease.InCubic);
        }

        public Task PlaySpoonLiftAsync(
            Vector3 foodAnchorPosition)
        {
            return PlaySpoonPositionAsync(
                foodAnchorPosition + _scoopHoverOffset,
                _scoopLiftDuration,
                _scoopLiftEase);
        }

        public async Task PlayFoodFlightAsync(
            FoodItemView foodItemView)
        {
            if (foodItemView == null)
            {
                return;
            }

            Vector3 fridgeEntryPosition =
                _fridgeFoodEntryPoint.position;

            FoodFlightMotion flightMotion = new(
                foodItemView.transform,
                fridgeEntryPosition,
                _foodFlightArcHeight,
                _foodFlightPeakProgress,
                _foodFlightScaleEase);

            Sequence foodSequence = Sequence.Create(
                Tween.Custom(
                    flightMotion,
                    0f,
                    1f,
                    _foodFlightDuration,
                    (motion, progress) =>
                        motion.Update(progress)));

            _foodFlightSequences.Add(foodSequence);

            await foodSequence;
            _foodFlightSequences.Remove(foodSequence);

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

        public async Task PlayFridgeBumpAsync()
        {
            if (!IsVisible)
            {
                return;
            }

            int bumpVersion = ++_fridgeBumpVersion;

            if (_fridgeBumpSequence.isAlive)
            {
                _fridgeBumpSequence.Stop();
            }

            Transform pulseTarget =
                _fridgeRenderer.transform;

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

            if (bumpVersion != _fridgeBumpVersion)
            {
                return;
            }

            _fridgeBumpSequence = default;
            pulseTarget.localScale =
                _fridgeRendererBaseScale;
        }

        public async Task<Vector3> PlayReleasePopAsync(
            FoodItemView foodItemView)
        {
            if (foodItemView == null)
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

            await foodItemView.PlayScaleAsync(
                overshootScale,
                _releaseGrowDuration,
                Ease.OutCubic,
                targetScale,
                _releaseSettleDuration,
                Ease.OutQuad);
        }

        public async Task PlaySpoonReturnAsync(
            Vector3 restFoodAnchorPosition)
        {
            if (!_spoonRoot.gameObject.activeSelf)
            {
                return;
            }

            await PlaySpoonPositionAsync(
                restFoodAnchorPosition + _scoopHoverOffset,
                _spoonReturnDuration,
                Ease.InOutCubic);

            HideSpoon();
        }

        public void SetClosedState()
        {
            IsVisible = true;
            SetFridgeAlpha(1f);
            SetFridgeSprite(_fridgeCloseSprite);
            transform.position = _visibleAnchor.position;
            transform.localScale = _fridgeBaseScale;
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
            return _fridgeFoodEntryPoint.position;
        }

        public async Task PlayDisappearAsync()
        {
            if (!IsVisible)
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
            _fridgeRenderer.enabled = false;
            IsVisible = false;
        }

        public void CancelAnimations()
        {
            _fridgeBumpVersion++;

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
                 i < _foodFlightSequences.Count;
                 i++)
            {
                Sequence sequence =
                    _foodFlightSequences[i];

                if (sequence.isAlive)
                {
                    sequence.Stop();
                }
            }

            _foodFlightSequences.Clear();

            _activeSequence = default;
            _activeTween = default;
            _fridgeBumpSequence = default;
            _fridgeRenderer.transform.localScale =
                _fridgeRendererBaseScale;
        }

        private void CaptureBaseScales()
        {
            _fridgeBaseScale = transform.localScale;
            _fridgeRendererBaseScale = _fridgeRenderer.transform.localScale;
            _spoonBaseScale = _spoonRoot.localScale;
        }

        private async Task PlaySpoonPositionAsync(
            Vector3 targetPosition,
            float duration,
            Ease ease)
        {
            _activeTween = Tween.Position(
                _spoonRoot,
                targetPosition,
                duration,
                ease);

            await _activeTween;
            _activeTween = default;
        }

        private void SetFridgeSprite(Sprite sprite)
        {
            _fridgeRenderer.sprite = sprite;
            _fridgeRenderer.enabled = true;
        }

        private void SetFridgeAlpha(float alpha)
        {
            Color color = _fridgeRenderer.color;
            color.a = Mathf.Clamp01(alpha);
            _fridgeRenderer.color = color;
        }

        private sealed class FoodFlightMotion
        {
            private readonly Transform _foodTransform;
            private readonly Vector3 _startPosition;
            private readonly Vector3 _targetPosition;
            private readonly Vector3 _startScale;
            private readonly float _arcHeight;
            private readonly float _peakProgress;
            private readonly Ease _scaleEase;

            public FoodFlightMotion(
                Transform foodTransform,
                Vector3 targetPosition,
                float arcHeight,
                float peakProgress,
                Ease scaleEase)
            {
                _foodTransform = foodTransform;
                _startPosition = foodTransform.position;
                _targetPosition = targetPosition;
                _startScale = foodTransform.localScale;
                _arcHeight = arcHeight;
                _peakProgress = peakProgress;
                _scaleEase = scaleEase;
            }

            public void Update(float progress)
            {
                Vector3 position = Vector3.LerpUnclamped(
                    _startPosition,
                    _targetPosition,
                    progress);
                position.y = CalculatePositionY(progress);
                _foodTransform.position = position;

                float scaleProgress = Easing.Evaluate(
                    progress,
                    _scaleEase);
                _foodTransform.localScale = Vector3.LerpUnclamped(
                    _startScale,
                    Vector3.zero,
                    scaleProgress);
            }

            private float CalculatePositionY(float progress)
            {
                float peakPositionY = Mathf.Max(
                    _startPosition.y,
                    _targetPosition.y) + _arcHeight;

                if (progress <= _peakProgress)
                {
                    float risingProgress = progress / _peakProgress;
                    float easedProgress = 1f -
                        (1f - risingProgress) *
                        (1f - risingProgress);
                    return Mathf.LerpUnclamped(
                        _startPosition.y,
                        peakPositionY,
                        easedProgress);
                }

                float fallingProgress =
                    (progress - _peakProgress) /
                    (1f - _peakProgress);
                return Mathf.LerpUnclamped(
                    peakPositionY,
                    _targetPosition.y,
                    fallingProgress * fallingProgress);
            }
        }
    }
}
