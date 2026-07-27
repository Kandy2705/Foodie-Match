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
            ResetSpoonPosition();

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

        public void ShowSpoon()
        {
            ResetSpoonPosition();

            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;

            _spoonRenderer.sprite = _spoonSprite;
            _spoonRenderer.enabled = true;
        }

        private void ShowSpoonAtOutsideLeft()
        {
            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;
            _spoonRoot.position =
                _spoonExitLeftAnchor.position;

            _spoonRenderer.sprite = _spoonSprite;
            _spoonRenderer.enabled = true;
        }

        public void HideSpoon()
        {
            _spoonRenderer.enabled = false;
            _spoonRoot.gameObject.SetActive(false);
        }

        public async Task PlayScoopFlickAsync(
            FoodItemView foodItemView,
            Vector3 currentFoodWorldPosition,
            bool hasNextFood,
            Vector3 nextFoodWorldPosition)
        {
            if (foodItemView == null)
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
            if (foodItemView == null)
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
            if (!IsVisible)
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
            if (!_spoonRoot.gameObject.activeSelf)
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
            _fridgeRendererBaseScale = _fridgeRenderer.transform.localScale;
            _spoonBaseScale = _spoonRoot.localScale;
        }

        private void ResetSpoonPosition()
        {
            _spoonRoot.position = _spoonStartAnchor.position;
            _spoonRoot.localScale = _spoonBaseScale;
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
    }
}
