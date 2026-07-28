using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Social
{
    public sealed class SocialView : MonoBehaviour
    {
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
        [SerializeField] private string _playWithFriendsMessage =
            "Play Foodie-Match with me!";
        [SerializeField] private string _shareMessage =
            "Check out Foodie-Match!";
        [SerializeField] private string _inviteFriendsMessage =
            "Join me in Foodie-Match!";

        private void Awake()
        {
            _playWithFriendsButton.onClick.AddListener(OnPlayWithFriendsClicked);
            _joinGroupButton.onClick.AddListener(OnJoinGroupClicked);
            _shareButton.onClick.AddListener(OnShareClicked);
            _followPageButton.onClick.AddListener(OnFollowPageClicked);
            _inviteFriendsButton.onClick.AddListener(OnInviteFriendsClicked);
        }

        private void OnDestroy()
        {
            _playWithFriendsButton.onClick.RemoveListener(OnPlayWithFriendsClicked);
            _joinGroupButton.onClick.RemoveListener(OnJoinGroupClicked);
            _shareButton.onClick.RemoveListener(OnShareClicked);
            _followPageButton.onClick.RemoveListener(OnFollowPageClicked);
            _inviteFriendsButton.onClick.RemoveListener(OnInviteFriendsClicked);
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

            SocialShareService.Share(_shareDialogTitle, message);
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
