using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.LeaderBoard
{
    public sealed class WeeklyContestIntroPopupView : MonoBehaviour
    {
        private const string PopupHiddenStateName =
            "WeeklyContestIntroPopup_Hidden";

        private const string PopupShownStateName =
            "WeeklyContestIntroPopup_Shown";

        private const string PopupOpenTriggerName = "Open";
        private const string PopupCloseTriggerName = "Close";

        private static readonly int PopupHiddenStateHash =
            Animator.StringToHash(PopupHiddenStateName);

        private static readonly int PopupShownStateHash =
            Animator.StringToHash(PopupShownStateName);

        private static readonly int PopupOpenTriggerHash =
            Animator.StringToHash(PopupOpenTriggerName);

        private static readonly int PopupCloseTriggerHash =
            Animator.StringToHash(PopupCloseTriggerName);

        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Animator _popupAnimator;
        [SerializeField] private Button _tapCatcher;

        private bool _isOpening;
        private bool _isOpen;
        private bool _canClose;

        private Coroutine _stateCoroutine;
        private void Awake()
        {
            InitializeState();
        }

        private void OnEnable()
        {
            AddListeners();
            ResetPopup();
        }

        private void OnDisable()
        {
            RemoveListeners();
            StopStateCoroutine();
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        public void Open()
        {
            if (_isOpening || _isOpen)
            {
                return;
            }

            if (_popupRoot == null)
            {
                return;
            }

            if (!_popupRoot.activeSelf)
            {
                _popupRoot.SetActive(true);
            }

            if (!isActiveAndEnabled ||
                _popupAnimator == null)
            {
                return;
            }

            StopStateCoroutine();

            _isOpening = true;
            _isOpen = false;
            _canClose = false;

            SetTapCatcherState(
                false,
                true);

            _popupAnimator.enabled = true;

            _popupAnimator.ResetTrigger(
                PopupCloseTriggerHash);

            _popupAnimator.ResetTrigger(
                PopupOpenTriggerHash);

            _popupAnimator.Play(
                PopupHiddenStateHash,
                0,
                0f);

            _popupAnimator.Update(0f);

            _popupAnimator.SetTrigger(
                PopupOpenTriggerHash);

            _stateCoroutine =
                StartCoroutine(
                    WaitForOpenComplete());
        }

        public void Close()
        {
            if (!_canClose ||
                !_isOpen ||
                _popupAnimator == null)
            {
                return;
            }

            StopStateCoroutine();

            _isOpening = false;
            _isOpen = false;
            _canClose = false;

            SetTapCatcherState(
                false,
                true);

            _popupAnimator.SetTrigger(
                PopupCloseTriggerHash);

            _stateCoroutine =
                StartCoroutine(
                    WaitForHiddenComplete());
        }

        public void OnTapCatcherClicked()
        {
            if (!_canClose)
            {
                return;
            }

            Close();
        }

        public void ResetPopup()
        {
            StopStateCoroutine();

            InitializeState();

            if (_popupAnimator == null ||
                !_popupAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            _popupAnimator.enabled = true;

            _popupAnimator.ResetTrigger(
                PopupOpenTriggerHash);

            _popupAnimator.ResetTrigger(
                PopupCloseTriggerHash);

            _popupAnimator.Play(
                PopupHiddenStateHash,
                0,
                0f);

            _popupAnimator.Update(0f);
        }

        private void InitializeState()
        {
            _isOpening = false;
            _isOpen = false;
            _canClose = false;

            SetTapCatcherState(
                false,
                false);

        }

        private void AddListeners()
        {
            if (_tapCatcher == null)
            {
                return;
            }

            _tapCatcher.onClick.RemoveListener(
                OnTapCatcherClicked);

            _tapCatcher.onClick.AddListener(
                OnTapCatcherClicked);
        }

        private void RemoveListeners()
        {
            if (_tapCatcher != null)
            {
                _tapCatcher.onClick.RemoveListener(
                    OnTapCatcherClicked);
            }
        }

        private IEnumerator WaitForOpenComplete()
        {
            yield return WaitForStateEntered(
                PopupShownStateHash);

            _stateCoroutine = null;

            if (!_isOpening)
            {
                yield break;
            }

            _isOpening = false;
            _isOpen = true;
            _canClose = true;

            SetTapCatcherState(
                true,
                true);
        }

        private IEnumerator WaitForHiddenComplete()
        {
            yield return WaitForStateEntered(
                PopupHiddenStateHash);

            _stateCoroutine = null;

            SetTapCatcherState(
                false,
                false);

            if (_popupRoot != null)
            {
                _popupRoot.SetActive(false);
            }
        }

        private IEnumerator WaitForStateEntered(
            int stateHash)
        {
            while (true)
            {
                if (!_popupAnimator.IsInTransition(0))
                {
                    AnimatorStateInfo stateInfo =
                        _popupAnimator
                            .GetCurrentAnimatorStateInfo(0);

                    if (stateInfo.shortNameHash == stateHash)
                    {
                        yield break;
                    }
                }

                yield return null;
            }
        }

        private void SetTapCatcherState(
            bool interactable,
            bool blocksRaycasts)
        {
            if (_tapCatcher == null)
            {
                return;
            }

            _tapCatcher.interactable =
                interactable;

            if (_tapCatcher.targetGraphic != null)
            {
                _tapCatcher.targetGraphic.raycastTarget =
                    blocksRaycasts;
            }
        }

        private void StopStateCoroutine()
        {
            if (_stateCoroutine == null)
            {
                return;
            }

            StopCoroutine(
                _stateCoroutine);

            _stateCoroutine = null;
        }
    }
}
