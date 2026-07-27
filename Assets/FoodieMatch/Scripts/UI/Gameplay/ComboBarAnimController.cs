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

        [Header("States")]
        [SerializeField] private string _startState = DefaultStartState;
        [SerializeField] private string _continueState = DefaultContinueState;
        [SerializeField] private string _breakState = DefaultBreakState;

        [Header("Timing")]
        [SerializeField] private float _breakDuration = 0.6f;

        public float BreakDuration => Mathf.Max(0.01f, _breakDuration);

        public void PlayStart()
        {
            PlayState(_startState);
        }

        public void PlayContinue()
        {
            PlayState(_continueState);
        }

        public void PlayBreak()
        {
            PlayState(_breakState);
        }

        private void PlayState(string stateName)
        {
            if (!_animator.isActiveAndEnabled)
            {
                return;
            }

            if (!string.IsNullOrEmpty(stateName))
            {
                _animator.Play(stateName, 0, 0f);
                _animator.Update(0f);
            }
        }

    }
}
