using System.Threading.Tasks;
using FoodieMatch.Features.Motion;
using PrimeTween;
using UnityEngine;

namespace FoodieMatch.Features.RequiredPackage
{
    public sealed class RequiredPackageView : MonoBehaviour
    {
        private const int MinRequiredAmount = 1;
        private const int MaxRequiredAmount = 3;

        [SerializeField] private RequiredPackageAmountView _amount1View;
        [SerializeField] private RequiredPackageAmountView _amount2View;
        [SerializeField] private RequiredPackageAmountView _amount3View;

        [Header("Motion")]
        [SerializeField] private Vector3 _completePunchStrength =
            new Vector3(0.16f, 0.16f, 0f);
        [SerializeField] private float _completeFeedbackDuration = 0.22f;

        private Sprite _sprite;
        private Tween _completeFeedbackTween;
        private bool _isCompleteFeedbackPlaying;
        private bool _didCompleteFeedbackFinish;
        private bool _hasInitialLocalScale;
        private Vector3 _initialLocalScale;

        public int FoodTokenId { get; private set; }
        public int RequiredAmount { get; private set; }
        public int FilledAmount { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;

        private void Awake()
        {
            EnsureInitialLocalScale();
        }

        private void OnDestroy()
        {
            StopCompleteFeedback(resetScale: false);
        }

        public RequiredPackageSlotView GetTargetSlot(
            int requiredAmount,
            int filledSlotIndex)
        {
            RequiredPackageAmountView amountView = GetView(requiredAmount);
            return amountView?.GetSlotAt(filledSlotIndex);
        }

        public void Setup(int foodTokenId, int requiredAmount, Sprite sprite)
        {
            StopCompleteFeedback(resetScale: true);

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

        public void Clear()
        {
            StopCompleteFeedback(resetScale: true);
            FoodTokenId = 0;
            RequiredAmount = 0;
            FilledAmount = 0;
            _sprite = null;

            HideAllViews();
        }

        public async Task<MotionResult> PlayCompleteFeedbackAsync()
        {
            if (IsEmpty ||
                _isCompleteFeedbackPlaying ||
                !IsValidTime(_completeFeedbackDuration))
            {
                return MotionResult.Failed;
            }

            EnsureInitialLocalScale();
            _isCompleteFeedbackPlaying = true;
            _didCompleteFeedbackFinish = false;

            try
            {
                _completeFeedbackTween = Tween.PunchScale(
                        transform,
                        _completePunchStrength,
                        _completeFeedbackDuration)
                    .OnComplete(
                        target: this,
                        target =>
                            target.MarkCompleteFeedbackFinished());

                await _completeFeedbackTween;

                return _didCompleteFeedbackFinish
                    ? MotionResult.Completed
                    : MotionResult.Cancelled;
            }
            finally
            {
                _completeFeedbackTween = default;
                _isCompleteFeedbackPlaying = false;
            }
        }

        public void StopCompleteFeedback()
        {
            StopCompleteFeedback(resetScale: true);
        }

        private void RefreshActiveView()
        {
            HideAllViews();

            RequiredPackageAmountView activeView = GetView(RequiredAmount);

            if (activeView == null)
            {
                Debug.LogWarning($"Required package view for amount {RequiredAmount} is missing.", this);
                return;
            }

            if (activeView.SlotCount != RequiredAmount)
            {
                Debug.LogWarning(
                    "Required package view for amount " +
                    $"{RequiredAmount} must have " +
                    $"{RequiredAmount} slots.",
                    this);
            }

            activeView.Show(_sprite, FilledAmount);
        }

        private RequiredPackageAmountView GetView(int requiredAmount)
        {
            return requiredAmount switch
            {
                1 => _amount1View,
                2 => _amount2View,
                3 => _amount3View,
                _ => null
            };
        }

        private void HideAllViews()
        {
            _amount1View?.Hide();
            _amount2View?.Hide();
            _amount3View?.Hide();
        }

        private void MarkCompleteFeedbackFinished()
        {
            _didCompleteFeedbackFinish = true;
        }

        private void StopCompleteFeedback(bool resetScale)
        {
            if (_completeFeedbackTween.isAlive)
            {
                _completeFeedbackTween.Stop();
            }

            if (resetScale)
            {
                EnsureInitialLocalScale();
                transform.localScale = _initialLocalScale;
            }
        }

        private void EnsureInitialLocalScale()
        {
            if (_hasInitialLocalScale)
            {
                return;
            }

            _initialLocalScale = transform.localScale;
            _hasInitialLocalScale = true;
        }

        private static bool IsValidTime(float value)
        {
            return value >= 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}
