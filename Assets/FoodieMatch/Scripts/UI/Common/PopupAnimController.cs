using System;
using System.Collections;
using UnityEngine;

namespace FoodieMatch.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class PopupAnimController : MonoBehaviour
    {
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
        private Coroutine _waitCoroutine;
        private bool _isOpened;
        private bool _hasAwakened;

        public bool IsOpened => _isOpened;

        private void Awake()
        {
            CacheHashes();
            _hasAwakened = true;

            // Do not call Animator.Update here. During Awake the Animator may not
            // have finished its own Awake yet, which triggers m_DidAwake assert.
            _isOpened = false;
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
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            StopWaiting();
            SetInteractable(false);

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
            if (gameObject.activeSelf && _isOpened)
            {
                Close();
                return;
            }

            Open();
        }

        public void HideInstantly()
        {
            StopWaiting();
            _isOpened = false;
            SampleHiddenStateIfPossible();
            SetInteractable(false);
            gameObject.SetActive(false);
        }

        private void SampleHiddenStateIfPossible()
        {
            if (_animator == null ||
                string.IsNullOrEmpty(_hiddenState) ||
                !_hasAwakened ||
                !_animator.isInitialized)
            {
                return;
            }

            _animator.Play(_hiddenStateHash, 0, 1f);

            if (_animator.isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                _animator.Update(0f);
            }
        }

        public void OnOpenAnimationFinished()
        {
            _isOpened = true;
            SetInteractable(true);
        }

        public void OnCloseAnimationFinished()
        {
            _isOpened = false;
            gameObject.SetActive(false);
        }

        private void CacheHashes()
        {
            _openTriggerHash = Animator.StringToHash(_openTrigger);
            _closeTriggerHash = Animator.StringToHash(_closeTrigger);
            _hiddenStateHash = Animator.StringToHash(_hiddenState);
        }

        private void SetInteractable(bool interactable)
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.interactable = interactable;
            _canvasGroup.blocksRaycasts = interactable;
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
                yield break;
            }

            yield return null;

            AnimatorStateInfo stateInfo;
            int stateHash = Animator.StringToHash(stateName);

            while (true)
            {
                stateInfo = _animator.GetCurrentAnimatorStateInfo(0);

                if (stateInfo.shortNameHash == stateHash &&
                    stateInfo.normalizedTime >= 1f &&
                    !_animator.IsInTransition(0))
                {
                    break;
                }

                yield return null;
            }

            onComplete?.Invoke();
            _waitCoroutine = null;
        }
    }
}
