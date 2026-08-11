using System;
using System.Threading.Tasks;
using FoodieMatch.Features.Effects;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageView : MonoBehaviour
    {
        private const int MinRequiredAmount = 1;
        private const int MaxRequiredAmount = 3;

        [SerializeField] private Transform _motionRoot;
        [SerializeField] private RequiredPackageAmountView _amount1View;
        [SerializeField] private RequiredPackageAmountView _amount2View;
        [SerializeField] private RequiredPackageAmountView _amount3View;
        [SerializeField] private GameObject _lid;
        [SerializeField] private SpriteRenderer _lidSpriteRenderer;
        [SerializeField] private SortingGroup _sortingGroup;

        [Header("Enter Motion")]
        [SerializeField] private Vector3 _enterOffset = new(-10f, 0f, 0f);
        [SerializeField] private float _enterDuration = 0.32f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;

        [Header("Enter Scale Motion")]
        [SerializeField] private float _enterScaleStartDelay = 0.18f;
        [SerializeField] private Vector3 _enterNarrowScaleMultiplier = new(0.82f, 1.14f, 1f);
        [SerializeField] private float _enterNarrowScaleDuration = 0.07f;
        [SerializeField] private Ease _enterNarrowScaleEase = Ease.OutCubic;
        [SerializeField] private Vector3 _enterWideScaleMultiplier = new(1.12f, 0.86f, 1f);
        [SerializeField] private float _enterWideScaleDuration = 0.08f;
        [SerializeField] private Ease _enterWideScaleEase = Ease.InOutSine;
        [SerializeField] private float _enterRestoreScaleDuration = 0.09f;
        [SerializeField] private Ease _enterRestoreScaleEase = Ease.OutCubic;

        [Header("Match Lid Motion")]
        [SerializeField] private Vector3 _lidDropOffset = new(0f, 0.35f, 0f);
        [SerializeField] private float _lidDropDuration = 0.12f;
        [SerializeField] private Ease _lidDropEase = Ease.OutCubic;

        [Header("Match Particle")]
        [SerializeField] private Vector3 _completeBurstOffset = new(0f, 0.5f, 0f);

        [Header("Match Scale Motion")]
        [SerializeField] private Vector3 _horizontalSquashScaleMultiplier = new(1.16f, 0.78f, 1f);
        [SerializeField] private float _horizontalSquashDuration = 0.09f;
        [SerializeField] private Ease _horizontalSquashEase = Ease.OutCubic;
        [SerializeField] private Vector3 _verticalStretchScaleMultiplier = new(0.88f, 1.14f, 1f);
        [SerializeField] private float _verticalStretchDuration = 0.11f;
        [SerializeField] private Ease _verticalStretchEase = Ease.InOutSine;
        [SerializeField] private float _restoreScaleDuration = 0.12f;
        [SerializeField] private Ease _restoreScaleEase = Ease.OutCubic;

        [Header("Match Exit Motion")]
        [SerializeField] private Vector3 _exitOffset = new(0f, 10f, 0f);
        [SerializeField] private float _exitDuration = 0.3f;
        [SerializeField] private Ease _exitEase = Ease.InBack;

        private Sprite _sprite;
        private Sequence _motionSequence;
        private bool _isMotionPlaying;
        private bool _didMotionFinish;
        private bool _hasInitialMotionRootTransform;
        private bool _hasInitialLidVisual;
        private Vector3 _initialMotionRootLocalPosition;
        private Vector3 _initialMotionRootLocalScale;
        private Vector3 _initialLidLocalPosition;
        private Color _lidVisibleColor;
        private Action _lidClosed;
        private ParticleEffectPool _completeBurstPool;
        private Transform _enterMotionTarget;
        private Vector3 _enterMotionRestPosition;
        private Vector3 _enterMotionRestScale;

        public int FoodTokenId { get; private set; }
        public int RequiredAmount { get; private set; }
        public int FilledAmount { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;

        private void Awake()
        {
            EnsureInitialMotionRootTransform();
            EnsureInitialLidVisual();
            HideLid();
        }

        private void OnDestroy()
        {
            StopMotion(resetTransform: false, hideLid: false);
        }

        public void Construct(
            ParticleEffectPool completeBurstPool)
        {
            _completeBurstPool = completeBurstPool;
        }

        public RequiredPackageSlotView GetTargetSlot(
            int requiredAmount,
            int filledSlotIndex)
        {
            RequiredPackageAmountView amountView = GetView(requiredAmount);
            return amountView.GetSlotAt(filledSlotIndex);
        }

        public void Setup(int foodTokenId, int requiredAmount, Sprite sprite)
        {
            StopMotion();

            if (foodTokenId <= 0)
            {
                Debug.LogWarning($"Required package food token id must be greater than 0: {foodTokenId}.", this);
                Clear();
                return;
            }

            if (requiredAmount < MinRequiredAmount || requiredAmount > MaxRequiredAmount)
            {
                Debug.LogWarning(
                    "Required package amount must be from " +
                    $"{MinRequiredAmount} to {MaxRequiredAmount}: " +
                    $"{requiredAmount}.",
                    this);
                Clear();
                return;
            }

            FoodTokenId = foodTokenId;
            RequiredAmount = requiredAmount;
            FilledAmount = 0;
            _sprite = sprite;

            RefreshActiveView();
        }

        public void SetFilledAmount(int filledAmount)
        {
            FilledAmount = Mathf.Clamp(filledAmount, 0, RequiredAmount);
            RefreshActiveView();
        }

        public void SetSortingOrder(int sortingOrder)
        {
            _sortingGroup.sortingOrder = sortingOrder;
        }

        public void Clear()
        {
            StopMotion();
            FoodTokenId = 0;
            RequiredAmount = 0;
            FilledAmount = 0;
            _sprite = null;

            HideAllViews();
        }

        public async Task<MotionResult> PlayEnterAsync(Action onEnterStarted)
        {
            if (IsEmpty || !CanPrepareEnter(_enterOffset))
            {
                return MotionResult.Failed;
            }

            PrepareEnterMotion(_motionRoot, _enterOffset);
            return await PlayPreparedEnterAsync(onEnterStarted);
        }

        internal void PrepareInitialEnter(
            Transform motionTarget,
            bool enterFromRight)
        {
            StopMotion();
            float direction = enterFromRight ? 1f : -1f;
            Vector3 enterOffset = _enterOffset;
            enterOffset.x = Mathf.Abs(enterOffset.x) * direction;

            if (!CanPrepareEnter(enterOffset))
            {
                return;
            }

            PrepareEnterMotion(
                motionTarget != null ? motionTarget : _motionRoot,
                enterOffset);
        }

        internal Task<MotionResult> PlayInitialEnterAsync()
        {
            return PlayPreparedEnterAsync(onEnterStarted: null);
        }

        private async Task<MotionResult> PlayPreparedEnterAsync(
            Action onEnterStarted)
        {
            if (_enterMotionTarget == null)
            {
                return MotionResult.Failed;
            }

            Transform motionTarget = _enterMotionTarget;
            Vector3 restPosition = _enterMotionRestPosition;
            Vector3 restScale = _enterMotionRestScale;
            _isMotionPlaying = true;
            _didMotionFinish = false;

            Vector3 narrowScale = Vector3.Scale(
                restScale,
                _enterNarrowScaleMultiplier);
            Vector3 wideScale = Vector3.Scale(
                restScale,
                _enterWideScaleMultiplier);

            try
            {
                Sequence scaleSequence = Sequence.Create()
                    .ChainDelay(_enterScaleStartDelay)
                    .Chain(Tween.Scale(
                        motionTarget,
                        narrowScale,
                        _enterNarrowScaleDuration,
                        _enterNarrowScaleEase))
                    .Chain(Tween.Scale(
                        motionTarget,
                        wideScale,
                        _enterWideScaleDuration,
                        _enterWideScaleEase))
                    .Chain(Tween.Scale(
                        motionTarget,
                        restScale,
                        _enterRestoreScaleDuration,
                        _enterRestoreScaleEase));

                _motionSequence = Sequence.Create(Tween.LocalPosition(
                        motionTarget,
                        restPosition,
                        _enterDuration,
                        _enterEase))
                    .Group(scaleSequence)
                    .ChainCallback(MarkMotionFinished);

                InvokeMotionCallback(onEnterStarted);
                await _motionSequence;

                return _didMotionFinish ? MotionResult.Completed : MotionResult.Cancelled;
            }
            finally
            {
                motionTarget.localPosition = restPosition;
                motionTarget.localScale = restScale;
                _enterMotionTarget = null;
                _motionSequence = default;
                _isMotionPlaying = false;
            }
        }

        private bool CanPrepareEnter(Vector3 enterOffset)
        {
            return !_isMotionPlaying &&
                   IsValidTime(_enterDuration) &&
                   IsValidTime(_enterScaleStartDelay) &&
                   IsValidTime(_enterNarrowScaleDuration) &&
                   IsValidTime(_enterWideScaleDuration) &&
                   IsValidTime(_enterRestoreScaleDuration) &&
                   IsValidVector(enterOffset) &&
                   IsValidScaleMultiplier(_enterNarrowScaleMultiplier) &&
                   IsValidScaleMultiplier(_enterWideScaleMultiplier);
        }

        private void PrepareEnterMotion(
            Transform motionTarget,
            Vector3 enterOffset)
        {
            if (motionTarget == _motionRoot)
            {
                EnsureInitialMotionRootTransform();
                ResetMotionRootTransform();
            }

            _enterMotionTarget = motionTarget;
            _enterMotionRestPosition = motionTarget.localPosition;
            _enterMotionRestScale = motionTarget.localScale;
            HideLid();
            motionTarget.localPosition =
                _enterMotionRestPosition + enterOffset;
        }

        public async Task<MotionResult> PlayMatchAndExitAsync(
            Action<Vector3> onMatchStarted,
            Action onLidClosed)
        {
            if (IsEmpty ||
                _isMotionPlaying ||
                !IsValidTime(_lidDropDuration) ||
                !IsValidTime(_horizontalSquashDuration) ||
                !IsValidTime(_verticalStretchDuration) ||
                !IsValidTime(_restoreScaleDuration) ||
                !IsValidTime(_exitDuration) ||
                !IsValidVector(_lidDropOffset) ||
                !IsValidScaleMultiplier(_horizontalSquashScaleMultiplier) ||
                !IsValidScaleMultiplier(_verticalStretchScaleMultiplier) ||
                !IsValidVector(_exitOffset))
            {
                return MotionResult.Failed;
            }

            EnsureInitialMotionRootTransform();
            EnsureInitialLidVisual();
            ResetMotionRootTransform();
            PrepareLidForDrop();
            _isMotionPlaying = true;
            _didMotionFinish = false;
            _lidClosed = onLidClosed;

            Vector3 horizontalSquashScale = Vector3.Scale(
                _initialMotionRootLocalScale,
                _horizontalSquashScaleMultiplier);
            Vector3 verticalStretchScale = Vector3.Scale(
                _initialMotionRootLocalScale,
                _verticalStretchScaleMultiplier);

            try
            {
                Sequence lidSequence = Sequence.Create(Tween.LocalPosition(
                        _lid.transform,
                        _initialLidLocalPosition,
                        _lidDropDuration,
                        _lidDropEase))
                    .Group(Tween.Alpha(
                        _lidSpriteRenderer,
                        endValue: 1f,
                        duration: _lidDropDuration,
                        ease: _lidDropEase))
                    .ChainCallback(this, target => target.NotifyLidClosed());

                _motionSequence = Sequence.Create()
                    .Chain(lidSequence)
                    .Chain(Tween.Scale(
                        _motionRoot,
                        horizontalSquashScale,
                        _horizontalSquashDuration,
                        _horizontalSquashEase))
                    .Chain(Tween.Scale(
                        _motionRoot,
                        verticalStretchScale,
                        _verticalStretchDuration,
                        _verticalStretchEase))
                    .Chain(Tween.Scale(
                        _motionRoot,
                        _initialMotionRootLocalScale,
                        _restoreScaleDuration,
                        _restoreScaleEase))
                    .Chain(Tween.LocalPosition(
                        _motionRoot,
                        _initialMotionRootLocalPosition + _exitOffset,
                        _exitDuration,
                        _exitEase))
                    .ChainCallback(this, target => target.MarkMotionFinished());

                InvokeMotionCallback(onMatchStarted, transform.position);
                await _motionSequence;

                return _didMotionFinish ? MotionResult.Completed : MotionResult.Cancelled;
            }
            finally
            {
                _lidClosed = null;
                _motionSequence = default;
                _isMotionPlaying = false;
            }
        }

        public void StopMotion()
        {
            StopMotion(resetTransform: true, hideLid: true);
        }

        private void RefreshActiveView()
        {
            HideAllViews();

            RequiredPackageAmountView activeView = GetView(RequiredAmount);
            activeView.Show(_sprite, FilledAmount);
        }

        private RequiredPackageAmountView GetView(int requiredAmount)
        {
            return requiredAmount switch
            {
                1 => _amount1View,
                2 => _amount2View,
                3 => _amount3View,
                _ => throw new ArgumentOutOfRangeException(nameof(requiredAmount))
            };
        }

        private void HideAllViews()
        {
            _amount1View.Hide();
            _amount2View.Hide();
            _amount3View.Hide();
        }

        private void MarkMotionFinished()
        {
            _didMotionFinish = true;
        }

        private void NotifyLidClosed()
        {
            PlayCompleteBurst();
            InvokeMotionCallback(_lidClosed);
        }

        private void PlayCompleteBurst()
        {
            Vector3 spawnPosition = transform.position + _completeBurstOffset;
            _completeBurstPool.Play(spawnPosition);
        }

        private static void InvokeMotionCallback(Action callback)
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void InvokeMotionCallback(Action<Vector3> callback, Vector3 worldPosition)
        {
            try
            {
                callback?.Invoke(worldPosition);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void StopMotion(bool resetTransform, bool hideLid)
        {
            _lidClosed = null;

            if (_motionSequence.isAlive)
            {
                _motionSequence.Stop();
            }

            _motionSequence = default;

            if (_enterMotionTarget != null)
            {
                _enterMotionTarget.localPosition =
                    _enterMotionRestPosition;
                _enterMotionTarget.localScale =
                    _enterMotionRestScale;
                _enterMotionTarget = null;
            }

            if (resetTransform)
            {
                ResetMotionRootTransform();
            }

            if (hideLid)
            {
                HideLid();
            }
        }

        private void ResetMotionRootTransform()
        {
            EnsureInitialMotionRootTransform();
            _motionRoot.localPosition = _initialMotionRootLocalPosition;
            _motionRoot.localScale = _initialMotionRootLocalScale;
        }

        private void PrepareLidForDrop()
        {
            ResetLidVisual();
            _lid.transform.localPosition = _initialLidLocalPosition + _lidDropOffset;
            SetLidAlpha(0f);
            _lid.SetActive(true);
        }

        private void HideLid()
        {
            ResetLidVisual();
            _lid.SetActive(false);
        }

        private void ResetLidVisual()
        {
            EnsureInitialLidVisual();

            if (!_hasInitialLidVisual)
            {
                return;
            }

            _lid.transform.localPosition = _initialLidLocalPosition;
            _lidSpriteRenderer.color = _lidVisibleColor;
        }

        private void SetLidAlpha(float alpha)
        {
            Color color = _lidSpriteRenderer.color;
            color.a = alpha;
            _lidSpriteRenderer.color = color;
        }

        private void EnsureInitialLidVisual()
        {
            if (_hasInitialLidVisual)
            {
                return;
            }

            _initialLidLocalPosition = _lid.transform.localPosition;
            _lidVisibleColor = _lidSpriteRenderer.color;
            _lidVisibleColor.a = 1f;
            _hasInitialLidVisual = true;
        }

        private void EnsureInitialMotionRootTransform()
        {
            if (_hasInitialMotionRootTransform)
            {
                return;
            }

            _initialMotionRootLocalPosition = _motionRoot.localPosition;
            _initialMotionRootLocalScale = _motionRoot.localScale;
            _hasInitialMotionRootTransform = true;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsValidVector(Vector3 value)
        {
            return IsValidNumber(value.x) && IsValidNumber(value.y) && IsValidNumber(value.z);
        }

        private static bool IsValidScaleMultiplier(Vector3 value)
        {
            return value.x > 0f &&
                   value.y > 0f &&
                   value.z > 0f &&
                   IsValidVector(value);
        }

        private static bool IsValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
