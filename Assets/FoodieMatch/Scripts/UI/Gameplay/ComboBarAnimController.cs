using UnityEngine;

namespace FoodieMatch.UI.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ComboBarAnimController : MonoBehaviour
    {
        private const string DefaultStartState = "ComboBar_Start";
        private const string DefaultContinueState = "ComboBar_Continue";
        private const string DefaultBreakState = "ComboBar_Break";

        [Header("References")]
        [SerializeField] private Animator _animator;

        [Header("Triggers")]
        [SerializeField] private string _startTrigger = "Start";
        [SerializeField] private string _continueTrigger = "Continue";
        [SerializeField] private string _breakTrigger = "Break";

        [Header("States")]
        [SerializeField] private string _startState = DefaultStartState;
        [SerializeField] private string _continueState = DefaultContinueState;
        [SerializeField] private string _breakState = DefaultBreakState;

        [Header("Timing")]
        [SerializeField] private float _breakDuration = 0.6f;

        private int _startTriggerHash;
        private int _continueTriggerHash;
        private int _breakTriggerHash;

        public float BreakDuration => Mathf.Max(0.01f, _breakDuration);

        private void Awake()
        {
            EnsureAnimator();
            CacheHashes();
        }

        private void OnValidate()
        {
            CacheHashes();
        }

        public void PlayStart()
        {
            PlayState(_startState, _startTriggerHash);
        }

        public void PlayContinue()
        {
            PlayState(_continueState, _continueTriggerHash);
        }

        public void PlayBreak()
        {
            PlayState(_breakState, _breakTriggerHash);
        }

        private void PlayState(string stateName, int triggerHash)
        {
            EnsureAnimator();

            if (_animator == null)
            {
                Debug.LogWarning(
                    $"{nameof(ComboBarAnimController)} on {name} has no Animator.",
                    this);
                return;
            }

            if (!_animator.isActiveAndEnabled)
            {
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(
                    $"{nameof(ComboBarAnimController)} on {name} has no Animator Controller.",
                    this);
                return;
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                _animator.Play(stateName, 0, 0f);
                _animator.Update(0f);
            }
        }

        private void EnsureAnimator()
        {
            if (_animator != null)
            {
                return;
            }

            _animator = GetComponent<Animator>();

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>(true);
            }

            if (_animator == null)
            {
                _animator = GetComponentInParent<Animator>();
            }
        }

        private void CacheHashes()
        {
            _startTriggerHash = Animator.StringToHash(_startTrigger);
            _continueTriggerHash = Animator.StringToHash(_continueTrigger);
            _breakTriggerHash = Animator.StringToHash(_breakTrigger);
        }
    }
}
