using System;
using System.Collections;
using UnityEngine;

namespace FoodieMatch.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class PopupAnimController : MonoBehaviour
    {
        private const float WaitTimeoutSeconds = 3f;

        [Header("References")]
        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [Header("Animator Parameters")]
        [SerializeField]
        private string _openTrigger = "Open";

        [SerializeField]
        private string _closeTrigger = "Close";

        [SerializeField]
        private string _hiddenState = "Hidden";

        [SerializeField]
        private string _shownState = "Shown";

        [Header("Animator States")]
        [SerializeField]
        private string _openState = "Open";

        [SerializeField]
        private string _closeState = "Close";

        [SerializeField]
        private bool _waitForAnimatorStates = true;

        private int _openTriggerHash;
        private int _closeTriggerHash;
        private int _hiddenStateHash;
        private int _shownStateHash;
        private Coroutine _waitCoroutine;
        private PopupMotionState _state;

        public bool IsOpened => _state == PopupMotionState.Open;

        private void Awake()
        {
            CacheHashes();
            _state = PopupMotionState.Closed;
            SetInteractable(false);
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            CacheHashes();
        }

        public void Open(Action onComplete = null)
        {
            StopWaiting();

            gameObject.SetActive(true);
            SetInteractable(false);
            _state = PopupMotionState.Opening;

            if (_animator != null)
            {
                SampleHiddenStateIfPossible();
                _animator.ResetTrigger(_closeTriggerHash);
                _animator.SetTrigger(_openTriggerHash);
            }

            if (_waitForAnimatorStates && _animator != null)
            {
                _waitCoroutine = StartCoroutine(
                    WaitForState(
                        _openState,
                        () =>
                        {
                            OnOpenAnimationFinished();
                            onComplete?.Invoke();
                        }));
                return;
            }

            OnOpenAnimationFinished();
            onComplete?.Invoke();
        }

        public void Close(Action onComplete = null)
        {
            if (_state == PopupMotionState.Closed)
            {
                onComplete?.Invoke();
                return;
            }

            StopWaiting();
            SetInteractable(false);
            _state = PopupMotionState.Closing;

            if (_animator != null)
            {
                _animator.ResetTrigger(_openTriggerHash);
                _animator.SetTrigger(_closeTriggerHash);
            }

            if (_waitForAnimatorStates && _animator != null)
            {
                _waitCoroutine = StartCoroutine(
                    WaitForState(
                        _closeState,
                        () =>
                        {
                            OnCloseAnimationFinished();
                            onComplete?.Invoke();
                        }));
                return;
            }

            OnCloseAnimationFinished();
            onComplete?.Invoke();
        }

        public void Toggle()
        {
            if (_state == PopupMotionState.Open ||
                _state == PopupMotionState.Opening)
            {
                Close();
                return;
            }

            Open();
        }

        public void ShowInstantly()
        {
            StopWaiting();
            gameObject.SetActive(true);

            if (_animator != null)
            {
                _animator.ResetTrigger(_openTriggerHash);
                _animator.ResetTrigger(_closeTriggerHash);
                SampleStateIfPossible(_shownState, _shownStateHash);
            }

            _state = PopupMotionState.Open;
            SetInteractable(true);
        }

        public void HideInstantly()
        {
            StopWaiting();
            _state = PopupMotionState.Closed;
            SampleStateIfPossible(_hiddenState, _hiddenStateHash);
            SetInteractable(false);
            gameObject.SetActive(false);
        }

        private void SampleHiddenStateIfPossible()
        {
            SampleStateIfPossible(_hiddenState, _hiddenStateHash);
        }

        private void SampleStateIfPossible(string stateName, int stateHash)
        {
            if (_animator == null ||
                string.IsNullOrEmpty(stateName) ||
                !_animator.isInitialized)
            {
                return;
            }

            _animator.Play(stateHash, 0, 1f);

            if (_animator.isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                _animator.Update(0f);
            }
        }

        public void OnOpenAnimationFinished()
        {
            _state = PopupMotionState.Open;
            SetInteractable(true);
        }

        public void OnCloseAnimationFinished()
        {
            _state = PopupMotionState.Closed;

            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
        }

        private void CacheHashes()
        {
            _openTriggerHash = Animator.StringToHash(_openTrigger);
            _closeTriggerHash = Animator.StringToHash(_closeTrigger);
            _hiddenStateHash = Animator.StringToHash(_hiddenState);
            _shownStateHash = Animator.StringToHash(_shownState);
        }

        private void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = interactable;

            _canvasGroup.blocksRaycasts = gameObject.activeSelf;
        }

        private void StopWaiting()
        {
            if (_waitCoroutine == null)
            {
                return;
            }

            StopCoroutine(_waitCoroutine);
            _waitCoroutine = null;
        }

        private IEnumerator WaitForState(string stateName, Action onComplete)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName))
            {
                onComplete?.Invoke();
                _waitCoroutine = null;
                yield break;
            }

            int stateHash = Animator.StringToHash(stateName);
            float elapsed = 0f;
            bool hasEnteredState = false;

            yield return null;
            elapsed += Time.unscaledDeltaTime;

            while (elapsed < WaitTimeoutSeconds)
            {
                AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
                bool isInTargetState = stateInfo.shortNameHash == stateHash;

                if (isInTargetState)
                {
                    hasEnteredState = true;

                    if (stateInfo.normalizedTime >= 1f && !_animator.IsInTransition(0))
                    {
                        break;
                    }
                }
                else if (hasEnteredState)
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            onComplete?.Invoke();
            _waitCoroutine = null;
        }

        private enum PopupMotionState
        {
            Closed,
            Opening,
            Open,
            Closing
        }
    }
}
