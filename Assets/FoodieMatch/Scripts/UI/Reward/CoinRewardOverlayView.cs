using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace FoodieMatch.UI.Reward
{
    public sealed class CoinRewardOverlayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _coinImagePrefab;
        [SerializeField] private RectTransform _coinContainer;
        [SerializeField] private RectTransform _defaultSpawnPoint;

        [Header("Appearance")]
        [SerializeField] private float _coinSpawnRadius = 100f;
        [SerializeField] private float _coinAppearDuration = 0.2f;
        [SerializeField] private float _coinAppearInterval = 0.2f;
        [SerializeField] private Ease _coinAppearEase = Ease.OutBack;
        [SerializeField] private float _coinHoldDuration = 1f;

        [Header("Movement")]
        [SerializeField] private Vector2 _coinFlightIntervalRange = new(0.12f, 0.28f);
        [SerializeField] private float _coinRetreatStrength = 150f;
        [SerializeField] private Vector2 _coinMovementDurationRange = new(0.75f, 1f);
        [SerializeField] private float _coinCurveOffset = 60f;
        [SerializeField] private Ease _coinMovementEase = Ease.Linear;

        private readonly List<CoinImageMotion> _coinImagePool = new();

        private CoinCounterView _coinCounter;
        private Action _coinArrived;
        private long _displayedCoinBalance;
        private long _targetCoinBalance;
        private int _coinValuePerImage;
        private int _remainingCoinImageCount;
        private bool _isRewardPlaying;

        private void Awake()
        {
            EnsureCoinContainerReference();
        }

        private void OnDisable()
        {
            CompleteRewardImmediately();
        }

        public void PlayCoinReward(
            CoinCounterView coinCounter,
            RectTransform spawnPoint,
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage,
            Action coinArrived)
        {
            CompleteRewardImmediately();

            if (coinCounter == null || targetCoinBalance <= startingCoinBalance || coinValuePerImage <= 0)
            {
                coinCounter?.SetCoinBalance(targetCoinBalance);
                return;
            }

            EnsureCoinContainerReference();

            if (_coinImagePrefab == null || _coinContainer == null || coinCounter.CoinTarget == null)
            {
                Debug.LogError("Coin reward overlay references are missing.", this);
                coinCounter.SetCoinBalance(targetCoinBalance);
                return;
            }

            int coinImageCount = CalculateCoinImageCount(
                startingCoinBalance,
                targetCoinBalance,
                coinValuePerImage);

            if (coinImageCount <= 0)
            {
                coinCounter.SetCoinBalance(targetCoinBalance);
                return;
            }

            EnsureCoinImagePoolCapacity(coinImageCount);
            _coinCounter = coinCounter;
            _coinArrived = coinArrived;
            _displayedCoinBalance = startingCoinBalance;
            _targetCoinBalance = targetCoinBalance;
            _coinValuePerImage = coinValuePerImage;
            _remainingCoinImageCount = coinImageCount;
            _isRewardPlaying = true;
            _coinCounter.SetCoinBalance(startingCoinBalance);

            Vector3 spawnPosition = GetLocalPosition(spawnPoint != null ? spawnPoint : _defaultSpawnPoint);
            float nextFlightStartTime = GetFirstFlightStartTime(coinImageCount);

            for (int i = 0; i < coinImageCount; i++)
            {
                CoinImageMotion coinImage = _coinImagePool[i];
                Vector2 spawnOffset = Random.insideUnitCircle * _coinSpawnRadius;
                Vector3 coinSpawnPosition = spawnPosition + new Vector3(spawnOffset.x, spawnOffset.y);
                float appearanceStartDelay = i * _coinAppearInterval;
                float movementStartDelay = nextFlightStartTime -
                                           appearanceStartDelay - _coinAppearDuration;

                coinImage.Play(
                    coinSpawnPosition,
                    appearanceStartDelay,
                    Mathf.Max(0f, movementStartDelay),
                    GetRandomPositiveValue(_coinMovementDurationRange),
                    Random.value < 0.5f ? -1f : 1f);
                nextFlightStartTime += GetRandomPositiveValue(_coinFlightIntervalRange);
            }
        }

        public void CompleteRewardImmediately()
        {
            if (!_isRewardPlaying)
            {
                return;
            }

            _coinCounter?.SetCoinBalance(_targetCoinBalance);
            StopReward();
        }

        private int CalculateCoinImageCount(
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage)
        {
            long rewardAmount = targetCoinBalance - startingCoinBalance;
            long coinImageCount = rewardAmount / coinValuePerImage;

            if (rewardAmount % coinValuePerImage != 0)
            {
                coinImageCount++;
            }

            if (coinImageCount > int.MaxValue)
            {
                Debug.LogError("Coin reward image count exceeds the supported range.", this);
                return 0;
            }

            return (int)coinImageCount;
        }

        private float GetFirstFlightStartTime(int coinImageCount)
        {
            float lastAppearanceStartTime = Mathf.Max(0, coinImageCount - 1) * _coinAppearInterval;
            return lastAppearanceStartTime + _coinAppearDuration + _coinHoldDuration;
        }

        private float GetRandomPositiveValue(Vector2 range)
        {
            float minimum = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Random.Range(minimum, maximum);
        }

        private void EnsureCoinContainerReference()
        {
            if (_coinContainer == null)
            {
                _coinContainer = transform as RectTransform;
            }
        }

        private void EnsureCoinImagePoolCapacity(int requiredCapacity)
        {
            while (_coinImagePool.Count < requiredCapacity)
            {
                Image coinImage = Instantiate(_coinImagePrefab, _coinContainer);
                coinImage.gameObject.name = $"CoinRewardImage_{_coinImagePool.Count + 1}";
                coinImage.raycastTarget = false;
                coinImage.gameObject.SetActive(false);
                _coinImagePool.Add(new CoinImageMotion(this, coinImage));
            }
        }

        private Vector3 GetLocalPosition(RectTransform rectTransform)
        {
            if (rectTransform == null || _coinContainer == null)
            {
                return Vector3.zero;
            }

            return _coinContainer.InverseTransformPoint(rectTransform.position);
        }

        private Vector3 GetCoinTargetLocalPosition()
        {
            return _coinCounter == null
                ? Vector3.zero
                : GetLocalPosition(_coinCounter.CoinTarget);
        }

        private void OnCoinImageArrived(CoinImageMotion coinImage)
        {
            if (!_isRewardPlaying || !coinImage.IsPlaying || _remainingCoinImageCount <= 0)
            {
                return;
            }

            coinImage.Hide();
            _remainingCoinImageCount--;
            long remainingCoinAmount = _targetCoinBalance - _displayedCoinBalance;
            long receivedCoinAmount = Math.Min(_coinValuePerImage, remainingCoinAmount);
            _displayedCoinBalance += receivedCoinAmount;
            _coinCounter?.SetCoinBalance(_displayedCoinBalance);
            _coinArrived?.Invoke();

            if (_remainingCoinImageCount == 0)
            {
                StopReward();
            }
        }

        private void StopReward()
        {
            for (int i = 0; i < _coinImagePool.Count; i++)
            {
                _coinImagePool[i].StopAndHide();
            }

            _coinCounter = null;
            _coinArrived = null;
            _coinValuePerImage = 0;
            _remainingCoinImageCount = 0;
            _isRewardPlaying = false;
        }

        private sealed class CoinImageMotion
        {
            private readonly CoinRewardOverlayView _owner;
            private readonly RectTransform _rectTransform;
            private readonly Vector3 _visibleScale;

            private Sequence _motionSequence;
            private Vector3 _spawnPosition;
            private float _curveDirection;

            public CoinImageMotion(CoinRewardOverlayView owner, Image coinImage)
            {
                _owner = owner;
                _rectTransform = coinImage.rectTransform;
                _visibleScale = _rectTransform.localScale;
            }

            public bool IsPlaying { get; private set; }

            public void Play(
                Vector3 spawnPosition,
                float appearanceStartDelay,
                float movementStartDelay,
                float movementDuration,
                float curveDirection)
            {
                StopAndHide();
                _spawnPosition = spawnPosition;
                _curveDirection = curveDirection;
                _rectTransform.localPosition = spawnPosition;
                _rectTransform.localScale = Vector3.zero;
                _rectTransform.gameObject.SetActive(true);
                _rectTransform.SetAsLastSibling();
                IsPlaying = true;

                _motionSequence = Sequence.Create(Tween.Scale(
                        _rectTransform,
                        _visibleScale,
                        _owner._coinAppearDuration,
                        _owner._coinAppearEase,
                        startDelay: appearanceStartDelay,
                        useUnscaledTime: true))
                    .Chain(Tween.Custom(
                        this,
                        0f,
                        1f,
                        movementDuration,
                        (coinImage, progress) => coinImage.UpdatePosition(progress),
                        _owner._coinMovementEase,
                        startDelay: movementStartDelay,
                        useUnscaledTime: true))
                    .ChainCallback(this, coinImage => coinImage.NotifyArrival());
            }

            public void Hide()
            {
                IsPlaying = false;
                _motionSequence = default;
                _rectTransform.gameObject.SetActive(false);
            }

            public void StopAndHide()
            {
                if (_motionSequence.isAlive)
                {
                    _motionSequence.Stop();
                }

                Hide();
            }

            private void UpdatePosition(float progress)
            {
                Vector3 targetPosition = _owner.GetCoinTargetLocalPosition();
                Vector3 targetDirection = targetPosition - _spawnPosition;
                Vector3 retreatDirection = targetDirection.sqrMagnitude <= Mathf.Epsilon
                    ? Vector3.down
                    : -targetDirection.normalized;
                Vector3 curveDirection = new(-targetDirection.y, targetDirection.x, 0f);

                if (curveDirection.sqrMagnitude > Mathf.Epsilon)
                {
                    curveDirection.Normalize();
                }

                Vector3 retreatControlPoint =
                    _spawnPosition + retreatDirection * _owner._coinRetreatStrength;
                Vector3 curveControlPoint = Vector3.Lerp(_spawnPosition, targetPosition, 0.5f) +
                                            curveDirection * _owner._coinCurveOffset * _curveDirection;
                float remainingProgress = 1f - progress;
                _rectTransform.localPosition =
                    remainingProgress * remainingProgress * remainingProgress * _spawnPosition +
                    3f * remainingProgress * remainingProgress * progress * retreatControlPoint +
                    3f * remainingProgress * progress * progress * curveControlPoint +
                    progress * progress * progress * targetPosition;
            }

            private void NotifyArrival()
            {
                _owner.OnCoinImageArrived(this);
            }
        }
    }
}
