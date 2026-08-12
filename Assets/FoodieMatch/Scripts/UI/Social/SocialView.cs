using System;
using FoodieMatch.UI.MainMenu;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Social
{
    public sealed class SocialView : MonoBehaviour, IMainMenuTabSelectionHandler
    {
        private const float ButtonOvershootScale = 1.05f;
        private const float ButtonStaggerFraction = 0.35f;

        [Header("Buttons")]
        [SerializeField] private Button _playWithFriendsButton;
        [SerializeField] private Button _joinGroupButton;
        [SerializeField] private Button _shareButton;
        [SerializeField] private Button _followPageButton;
        [SerializeField] private Button _inviteFriendsButton;

        [Header("Links")]
        [SerializeField] private string _joinGroupUrl;
        [SerializeField] private string _followPageUrl;

        [Header("Sharing")]
        [SerializeField] private string _shareDialogTitle = "Foodie-Match";
        [SerializeField] private string _playWithFriendsMessage = "Play Foodie-Match with me!";
        [SerializeField] private string _shareMessage = "Check out Foodie-Match!";
        [SerializeField] private string _inviteFriendsMessage = "Join me in Foodie-Match!";
        [SerializeField] private string _storeUrl = "https://play.google.com/store/games";

        [Header("Button Reveal")]
        [SerializeField, Min(0f)] private float _buttonRevealDelay = 0.1f;
        [SerializeField, Min(0f)] private float _buttonScaleUpDuration = 0.4f;
        [SerializeField, Min(0f)] private float _buttonSettleDuration = 0.07f;

        private Button[] _revealButtons;
        private Animator[] _buttonAnimators;
        private Graphic[][] _buttonGraphics;
        private Sequence _buttonRevealSequence;

        private void Awake()
        {
            _revealButtons = new[]
            {
                _shareButton,
                _joinGroupButton,
                _followPageButton,
                _inviteFriendsButton,
                _playWithFriendsButton
            };

            _buttonAnimators = new Animator[_revealButtons.Length];
            _buttonGraphics = new Graphic[_revealButtons.Length][];

            for (int i = 0; i < _revealButtons.Length; i++)
            {
                _buttonAnimators[i] = _revealButtons[i].GetComponent<Animator>();
                _buttonGraphics[i] = _revealButtons[i].GetComponentsInChildren<Graphic>(true);
            }

            _playWithFriendsButton.onClick.AddListener(OnPlayWithFriendsClicked);
            _joinGroupButton.onClick.AddListener(OnJoinGroupClicked);
            _shareButton.onClick.AddListener(OnShareClicked);
            _followPageButton.onClick.AddListener(OnFollowPageClicked);
            _inviteFriendsButton.onClick.AddListener(OnInviteFriendsClicked);

            PrepareButtonsForReveal();
        }

        private void OnDestroy()
        {
            StopButtonRevealAnimation();

            _playWithFriendsButton.onClick.RemoveListener(OnPlayWithFriendsClicked);
            _joinGroupButton.onClick.RemoveListener(OnJoinGroupClicked);
            _shareButton.onClick.RemoveListener(OnShareClicked);
            _followPageButton.onClick.RemoveListener(OnFollowPageClicked);
            _inviteFriendsButton.onClick.RemoveListener(OnInviteFriendsClicked);
        }

        public void OnTabSelected()
        {
            PlayButtonRevealAnimation();
        }

        private void PlayButtonRevealAnimation()
        {
            StopButtonRevealAnimation();
            PrepareButtonsForReveal();

            float revealDelay = Mathf.Max(0f, _buttonRevealDelay);
            float scaleUpDuration = Mathf.Max(0f, _buttonScaleUpDuration);
            float settleDuration = Mathf.Max(0f, _buttonSettleDuration);
            float buttonStagger = scaleUpDuration * ButtonStaggerFraction;

            Vector3 overshootScale = Vector3.one * ButtonOvershootScale;

            Sequence sequence = Sequence.Create(useUnscaledTime: true);

            for (int i = 0; i < _revealButtons.Length; i++)
            {
                Button button = _revealButtons[i];
                int buttonIndex = i;

                float startTime = revealDelay + buttonStagger * i;
                float settleStartTime = startTime + scaleUpDuration;
                float endTime = settleStartTime + settleDuration;

                sequence = sequence.Insert(
                    startTime,
                    Tween.Scale(
                        button.transform,
                        overshootScale,
                        scaleUpDuration,
                        Ease.OutQuad));

                sequence = sequence.Insert(
                    settleStartTime,
                    Tween.Scale(
                        button.transform,
                        Vector3.one,
                        settleDuration,
                        Ease.OutQuad));

                Graphic[] graphics = _buttonGraphics[i];

                for (int graphicIndex = 0; graphicIndex < graphics.Length; graphicIndex++)
                {
                    sequence = sequence.Insert(
                        startTime,
                        Tween.Alpha(
                            graphics[graphicIndex],
                            0f,
                            1f,
                            scaleUpDuration,
                            Ease.InCubic));
                }

                sequence = sequence.InsertCallback(
                    endTime,
                    this,
                    view => view.EnableButton(buttonIndex));
            }

            _buttonRevealSequence = sequence;
        }

        private void PrepareButtonsForReveal()
        {
            for (int i = 0; i < _revealButtons.Length; i++)
            {
                if (_buttonAnimators[i] != null)
                {
                    _buttonAnimators[i].enabled = false;
                }

                _revealButtons[i].interactable = false;
                _revealButtons[i].transform.localScale = Vector3.zero;

                SetButtonAlpha(i, 0f);
            }
        }

        private void EnableButton(int buttonIndex)
        {
            SetButtonAlpha(buttonIndex, 1f);

            _revealButtons[buttonIndex].transform.localScale = Vector3.one;
            _revealButtons[buttonIndex].interactable = true;

            if (_buttonAnimators[buttonIndex] != null)
            {
                _buttonAnimators[buttonIndex].enabled = true;
            }
        }

        private void SetButtonAlpha(int buttonIndex, float alpha)
        {
            Graphic[] graphics = _buttonGraphics[buttonIndex];

            for (int i = 0; i < graphics.Length; i++)
            {
                Color color = graphics[i].color;
                color.a = alpha;
                graphics[i].color = color;
            }
        }

        private void StopButtonRevealAnimation()
        {
            if (_buttonRevealSequence.isAlive)
            {
                _buttonRevealSequence.Stop();
            }

            _buttonRevealSequence = default;
        }

        private void OnPlayWithFriendsClicked()
        {
            Share(_playWithFriendsMessage, "Play with Friends");
        }

        private void OnJoinGroupClicked()
        {
            OpenExternalUrl(_joinGroupUrl, "Join Group");
        }

        private void OnShareClicked()
        {
            Share(_shareMessage, "Share");
        }

        private void OnFollowPageClicked()
        {
            OpenExternalUrl(_followPageUrl, "Follow Page");
        }

        private void OnInviteFriendsClicked()
        {
            Share(_inviteFriendsMessage, "Invite Friends");
        }

        private void Share(string message, string actionName)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning(
                    $"Social action '{actionName}' does not have share content configured.",
                    this);

                return;
            }

            string content = message;

            if (!string.IsNullOrWhiteSpace(_storeUrl))
            {
                content += "\n" + _storeUrl;
            }

            SocialShareService.Share(_shareDialogTitle, content);
        }

        private void OpenExternalUrl(string url, string actionName)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning(
                    $"Social action '{actionName}' does not have a URL configured.",
                    this);

                return;
            }

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri targetUri) ||
                (targetUri.Scheme != Uri.UriSchemeHttp &&
                 targetUri.Scheme != Uri.UriSchemeHttps))
            {
                Debug.LogWarning(
                    $"Social action '{actionName}' has an invalid HTTP/HTTPS URL.",
                    this);

                return;
            }

            Application.OpenURL(targetUri.AbsoluteUri);
        }
    }
}