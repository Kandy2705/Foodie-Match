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

        [SerializeField] private Transform _motionRoot;
        [SerializeField] private RequiredPackageAmountView _amount1View;
        [SerializeField] private RequiredPackageAmountView _amount2View;
        [SerializeField] private RequiredPackageAmountView _amount3View;
        [SerializeField] private GameObject _lid;

        [Header("Enter Motion")]
        [SerializeField] private Vector3 _enterOffset = new(-10f, 0f, 0f);
        [SerializeField] private float _enterDuration = 0.3f;
        [SerializeField] private Ease _enterEase = Ease.OutCubic;
        [SerializeField] private Vector3 _enterPunchStrength = new(0.16f, 0.16f, 0f);
        [SerializeField] private float _enterPunchDuration = 0.18f;

        [Header("Match Motion")]
        [SerializeField] private Vector3 _completePunchStrength = new(0.16f, 0.16f, 0f);
        [SerializeField] private float _completeFeedbackDuration = 0.22f;
        [SerializeField] private Vector3 _exitOffset = new(0f, 10f, 0f);
        [SerializeField] private float _exitDuration = 0.3f;
        [SerializeField] private Ease _exitEase = Ease.InCubic;

        private Sprite _sprite;
        private Sequence _motionSequence;
        private bool _isMotionPlaying;
        private bool _didMotionFinish;
        private bool _hasInitialMotionRootTransform;
        private Vector3 _initialMotionRootLocalPosition;
        private Vector3 _initialMotionRootLocalScale;

        public int FoodTokenId { get; private set; }
        public int RequiredAmount { get; private set; }
        public int FilledAmount { get; private set; }
        public bool IsEmpty => FoodTokenId == 0;
        public bool IsComplete => !IsEmpty && FilledAmount >= RequiredAmount;

        private void Awake()
        {
            EnsureInitialMotionRootTransform();
            HideLid();

            if (_motionRoot == null)
            {
                Debug.LogWarning("Required package motion root is missing.", this);
            }

            if (_lid == null)
            {
                Debug.LogWarning("Required package lid is missing.", this);
            }
        }

        private void OnDestroy()
        {
            StopMotion(resetTransform: false, hideLid: false);
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

        public void Clear()
        {
            StopMotion();
            FoodTokenId = 0;
            RequiredAmount = 0;
            FilledAmount = 0;
            _sprite = null;

            HideAllViews();
        }

        public async Task<MotionResult> PlayEnterAsync()
        {
            if (IsEmpty ||
                _motionRoot == null ||
                _isMotionPlaying ||
                !IsValidTime(_enterDuration) ||
                !IsValidTime(_enterPunchDuration) ||
                !IsValidVector(_enterOffset))
            {
                return MotionResult.Failed;
            }

            EnsureInitialMotionRootTransform();
            ResetMotionRootTransform();
            HideLid();
            _motionRoot.localPosition = _initialMotionRootLocalPosition + _enterOffset;
            _isMotionPlaying = true;
            _didMotionFinish = false;

            try
            {
                _motionSequence = Sequence.Create()
                    .Chain(Tween.LocalPosition(
                        _motionRoot, _initialMotionRootLocalPosition, _enterDuration, _enterEase))
                    .Chain(Tween.PunchScale(_motionRoot, _enterPunchStrength, _enterPunchDuration))
                    .ChainCallback(this, target => target.MarkMotionFinished());

                await _motionSequence;

                return _didMotionFinish ? MotionResult.Completed : MotionResult.Cancelled;
            }
            finally
            {
                _motionSequence = default;
                _isMotionPlaying = false;
            }
        }

        public async Task<MotionResult> PlayMatchAndExitAsync()
        {
            if (IsEmpty ||
                _motionRoot == null ||
                _isMotionPlaying ||
                !IsValidTime(_completeFeedbackDuration) ||
                !IsValidTime(_exitDuration) ||
                !IsValidVector(_exitOffset))
            {
                return MotionResult.Failed;
            }

            EnsureInitialMotionRootTransform();
            ResetMotionRootTransform();
            ShowLid();
            _isMotionPlaying = true;
            _didMotionFinish = false;

            try
            {
                _motionSequence = Sequence.Create()
                    .Chain(Tween.PunchScale(_motionRoot, _completePunchStrength, _completeFeedbackDuration))
                    .Chain(Tween.LocalPosition(
                        _motionRoot,
                        _initialMotionRootLocalPosition + _exitOffset,
                        _exitDuration,
                        _exitEase))
                    .ChainCallback(this, target => target.MarkMotionFinished());

                await _motionSequence;

                return _didMotionFinish ? MotionResult.Completed : MotionResult.Cancelled;
            }
            finally
            {
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

        private void MarkMotionFinished()
        {
            _didMotionFinish = true;
        }

        private void StopMotion(bool resetTransform, bool hideLid)
        {
            if (_motionSequence.isAlive)
            {
                _motionSequence.Stop();
            }

            _motionSequence = default;

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
            if (_motionRoot == null)
            {
                return;
            }

            EnsureInitialMotionRootTransform();
            _motionRoot.localPosition = _initialMotionRootLocalPosition;
            _motionRoot.localScale = _initialMotionRootLocalScale;
        }

        private void ShowLid()
        {
            if (_lid != null)
            {
                _lid.SetActive(true);
            }
        }

        private void HideLid()
        {
            if (_lid != null)
            {
                _lid.SetActive(false);
            }
        }

        private void EnsureInitialMotionRootTransform()
        {
            if (_hasInitialMotionRootTransform || _motionRoot == null)
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

        private static bool IsValidNumber(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
