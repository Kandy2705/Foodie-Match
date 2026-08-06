using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FoodieMatch.UI.Reward
{
    public sealed class CoinRewardOverlayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CoinRewardView _coinPrefab;
        [SerializeField] private RectTransform _coinContainer;
        [SerializeField] private RectTransform _defaultSpawnPoint;
        [SerializeField] private RectTransform _coinArrivalParticleRoot;
        [SerializeField] private ParticleSystem _coinArrivalParticle;

        [Header("Appearance")]
        [SerializeField] private float _coinSpawnRadius = 100f;
        [SerializeField] private float _coinAppearInterval = 0.2f;
        [SerializeField] private float _coinHoldDuration = 1f;

        [Header("Movement")]
        [SerializeField] private Vector2 _coinFlightIntervalRange = new(0.12f, 0.28f);

        private readonly List<CoinRewardView> _coinPool = new();

        private CoinCounterView _coinCounter;
        private Action _coinArrived;
        private long _displayedCoinBalance;
        private long _targetCoinBalance;
        private int _coinValuePerImage;
        private int _remainingCoinCount;
        private int _remainingVisibleCoinCount;
        private bool _isRewardPlaying;

        private void Awake()
        {
            ParticleSystem.EmissionModule emission = _coinArrivalParticle.emission;
            emission.enabled = false;
        }

        private void OnDisable()
        {
            CompleteRewardImmediately();
            _coinArrivalParticle.Stop(
                withChildren: true,
                stopBehavior: ParticleSystemStopBehavior.StopEmittingAndClear);
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

            if (targetCoinBalance <= startingCoinBalance || coinValuePerImage <= 0)
            {
                coinCounter.SetCoinBalance(targetCoinBalance);
                return;
            }

            int coinCount = CalculateCoinCount(
                startingCoinBalance,
                targetCoinBalance,
                coinValuePerImage);

            if (coinCount <= 0)
            {
                coinCounter.SetCoinBalance(targetCoinBalance);
                return;
            }

            EnsureCoinPoolCapacity(coinCount);
            _coinCounter = coinCounter;
            _coinArrived = coinArrived;
            _displayedCoinBalance = startingCoinBalance;
            _targetCoinBalance = targetCoinBalance;
            _coinValuePerImage = coinValuePerImage;
            _remainingCoinCount = coinCount;
            _remainingVisibleCoinCount = coinCount;
            _isRewardPlaying = true;
            _coinCounter.SetCoinBalance(startingCoinBalance);

            Vector3 spawnPosition = GetLocalPosition(spawnPoint != null ? spawnPoint : _defaultSpawnPoint);
            float nextFlightStartTime = GetFirstFlightStartTime(coinCount);

            for (int i = 0; i < coinCount; i++)
            {
                CoinRewardView coin = _coinPool[i];
                Vector2 spawnOffset = Random.insideUnitCircle * _coinSpawnRadius;
                Vector3 coinSpawnPosition = spawnPosition + new Vector3(spawnOffset.x, spawnOffset.y);
                float appearanceStartDelay = i * _coinAppearInterval;

                coin.Play(
                    coinSpawnPosition,
                    appearanceStartDelay,
                    nextFlightStartTime,
                    _coinCounter.CoinTarget,
                    OnCoinArrived,
                    OnCoinArrivalHoldCompleted);
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

        private int CalculateCoinCount(
            long startingCoinBalance,
            long targetCoinBalance,
            int coinValuePerImage)
        {
            long rewardAmount = targetCoinBalance - startingCoinBalance;
            long coinCount = rewardAmount / coinValuePerImage;

            if (rewardAmount % coinValuePerImage != 0)
            {
                coinCount++;
            }

            if (coinCount > int.MaxValue)
            {
                Debug.LogError("Coin reward count exceeds the supported range.", this);
                return 0;
            }

            return (int)coinCount;
        }

        private float GetFirstFlightStartTime(int coinCount)
        {
            float lastAppearanceStartTime = Mathf.Max(0, coinCount - 1) * _coinAppearInterval;
            return lastAppearanceStartTime + _coinPrefab.AppearanceDuration + _coinHoldDuration;
        }

        private float GetRandomPositiveValue(Vector2 range)
        {
            float minimum = Mathf.Max(0.01f, Mathf.Min(range.x, range.y));
            float maximum = Mathf.Max(minimum, Mathf.Max(range.x, range.y));
            return Random.Range(minimum, maximum);
        }

        private void EnsureCoinPoolCapacity(int requiredCapacity)
        {
            while (_coinPool.Count < requiredCapacity)
            {
                CoinRewardView coin = Instantiate(_coinPrefab, _coinContainer);
                coin.gameObject.name = $"CoinReward_{_coinPool.Count + 1}";
                coin.gameObject.SetActive(false);
                _coinPool.Add(coin);
            }
        }

        private Vector3 GetLocalPosition(RectTransform rectTransform)
        {
            return _coinContainer.InverseTransformPoint(rectTransform.position);
        }

        private void OnCoinArrived(CoinRewardView coin)
        {
            if (!_isRewardPlaying || !coin.IsPlaying || _remainingCoinCount <= 0)
            {
                return;
            }

            PlayCoinArrivalParticle();
            _remainingCoinCount--;
            long remainingCoinAmount = _targetCoinBalance - _displayedCoinBalance;
            long receivedCoinAmount = Math.Min(_coinValuePerImage, remainingCoinAmount);
            _displayedCoinBalance += receivedCoinAmount;
            _coinCounter?.SetCoinBalance(_displayedCoinBalance);
            _coinArrived?.Invoke();
        }

        private void OnCoinArrivalHoldCompleted(CoinRewardView coin)
        {
            _remainingVisibleCoinCount--;

            if (_remainingVisibleCoinCount == 0)
            {
                StopReward();
            }
        }

        private void PlayCoinArrivalParticle()
        {
            _coinArrivalParticleRoot.position = _coinCounter.CoinTarget.position;
            ParticleSystem.Burst burst = _coinArrivalParticle.emission.GetBurst(0);
            int starCount = Mathf.RoundToInt(burst.count.Evaluate(0f, Random.value));
            _coinArrivalParticle.Emit(starCount);
        }

        private void StopReward()
        {
            for (int i = 0; i < _coinPool.Count; i++)
            {
                _coinPool[i].StopAndHide();
            }

            _coinCounter = null;
            _coinArrived = null;
            _coinValuePerImage = 0;
            _remainingCoinCount = 0;
            _remainingVisibleCoinCount = 0;
            _isRewardPlaying = false;
        }

    }
}
