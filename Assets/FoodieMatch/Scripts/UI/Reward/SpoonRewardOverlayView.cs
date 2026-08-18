using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FoodieMatch.UI.Reward
{
    public sealed class SpoonRewardOverlayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SpoonRewardView _spoonPrefab;
        [SerializeField] private RectTransform _spoonContainer;
        [SerializeField] private RectTransform _spawnPoint;
        [SerializeField] private RectTransform _arrivalParticleRoot;
        [SerializeField] private ParticleSystem _arrivalParticle;

        [Header("Timing")]
        [SerializeField] private float _spawnRadius = 120f;
        [SerializeField] private float _spoonInterval = 0.15f;

        private readonly List<SpoonRewardView> _spoonPool = new();

        private RectTransform _target;
        private Action _spoonArrived;
        private int _remainingSpoonCount;

        private void Awake()
        {
            ParticleSystem.EmissionModule emission =
                _arrivalParticle.emission;
            emission.enabled = false;
        }

        private void OnDisable()
        {
            StopReward();
        }

        public void PlaySpoonReward(
            int spoonCount,
            RectTransform target,
            Action spoonArrived)
        {
            StopReward();

            if (spoonCount <= 0)
            {
                return;
            }

            EnsurePoolCapacity(spoonCount);
            _target = target;
            _spoonArrived = spoonArrived;
            _remainingSpoonCount = spoonCount;

            Vector3 spawnPosition = GetLocalPosition(_spawnPoint);

            for (int i = 0; i < spoonCount; i++)
            {
                Vector2 spawnOffset =
                    Random.insideUnitCircle * _spawnRadius;
                Vector3 spoonSpawnPosition = spawnPosition +
                    new Vector3(spawnOffset.x, spawnOffset.y);

                _spoonPool[i].Play(
                    spoonSpawnPosition,
                    i * _spoonInterval,
                    target,
                    OnSpoonArrived);
            }
        }

        public void StopReward()
        {
            for (int i = 0; i < _spoonPool.Count; i++)
            {
                _spoonPool[i].StopAndHide();
            }

            _target = null;
            _spoonArrived = null;
            _remainingSpoonCount = 0;
            _arrivalParticle.Stop(
                withChildren: true,
                stopBehavior:
                    ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EnsurePoolCapacity(int requiredCapacity)
        {
            while (_spoonPool.Count < requiredCapacity)
            {
                SpoonRewardView spoon = Instantiate(
                    _spoonPrefab,
                    _spoonContainer);
                spoon.gameObject.name =
                    $"SpoonReward_{_spoonPool.Count + 1}";
                spoon.gameObject.SetActive(false);
                _spoonPool.Add(spoon);
            }
        }

        private Vector3 GetLocalPosition(RectTransform rectTransform)
        {
            return _spoonContainer.InverseTransformPoint(
                rectTransform.position);
        }

        private void OnSpoonArrived(SpoonRewardView spoon)
        {
            PlayArrivalParticle();
            _spoonArrived();
            _remainingSpoonCount--;

            if (_remainingSpoonCount == 0)
            {
                _target = null;
                _spoonArrived = null;
            }
        }

        private void PlayArrivalParticle()
        {
            _arrivalParticleRoot.position = _target.position;
            ParticleSystem.Burst burst =
                _arrivalParticle.emission.GetBurst(0);
            int particleCount = Mathf.RoundToInt(
                burst.count.Evaluate(0f, Random.value));
            _arrivalParticle.Emit(particleCount);
        }
    }
}
