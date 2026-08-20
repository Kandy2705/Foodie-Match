using System;
using FoodieMatch.UI.Common;
using FoodieMatch.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.DailyReward
{
    [DisallowMultipleComponent]
    public sealed class DailyRewardPopupView : PopupBase
    {
        [Header("Buttons")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _questsTabButton;
        [SerializeField] private Button _freeCoinTabButton;

        [Header("Content")]
        [SerializeField] private GameObject _questContent;
        [SerializeField] private GameObject _freeCoinContent;

        [Header("Tabs")]
        [SerializeField] private Image _questsTabImage;
        [SerializeField] private Image _freeCoinTabImage;

        [Header("Sprites")]
        [SerializeField] private Sprite _tabOnSprite;
        [SerializeField] private Sprite _tabOffSprite;

        [Header("Animation")]
        [SerializeField] private PopupAnimController _popupAnimController;

        private Action _closeClicked;

        private void Awake()
        {
            _popupAnimController ??= GetComponent<PopupAnimController>();

            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            if (_questsTabButton != null)
            {
                _questsTabButton.onClick.AddListener(OnQuestsTabButtonClicked);
            }

            if (_freeCoinTabButton != null)
            {
                _freeCoinTabButton.onClick.AddListener(OnFreeCoinTabButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }

            if (_questsTabButton != null)
            {
                _questsTabButton.onClick.RemoveListener(OnQuestsTabButtonClicked);
            }

            if (_freeCoinTabButton != null)
            {
                _freeCoinTabButton.onClick.RemoveListener(OnFreeCoinTabButtonClicked);
            }
        }

        public void SetActions(DailyRewardPopupViewActions actions)
        {
            _closeClicked = actions.CloseClicked;
        }

        public override void Show()
        {
            base.Show();
            ShowQuests();

            if (_popupAnimController != null)
            {
                _popupAnimController.Open();
            }
        }

        public override void Hide()
        {
            if (gameObject.activeInHierarchy && _popupAnimController != null)
            {
                _popupAnimController.Close(OnCloseAnimationFinished);
                return;
            }

            base.Hide();
        }

        public override void Dispose()
        {
            _closeClicked = null;
            base.Dispose();
        }

        public void ShowQuests()
        {
            if (_questContent != null)
            {
                _questContent.SetActive(true);
            }

            if (_freeCoinContent != null)
            {
                _freeCoinContent.SetActive(false);
            }

            SetTabVisual(_questsTabImage, _tabOnSprite, flipX: true);
            SetTabVisual(_freeCoinTabImage, _tabOffSprite, flipX: true);
        }

        public void ShowFreeCoin()
        {
            if (_questContent != null)
            {
                _questContent.SetActive(false);
            }

            if (_freeCoinContent != null)
            {
                _freeCoinContent.SetActive(true);
            }

            SetTabVisual(_questsTabImage, _tabOffSprite, flipX: false);
            SetTabVisual(_freeCoinTabImage, _tabOnSprite, flipX: false);
        }

        private static void SetTabVisual(Image image, Sprite sprite, bool flipX)
        {
            if (image == null)
            {
                return;
            }

            if (sprite != null)
            {
                image.sprite = sprite;
            }

            Vector3 scale = image.rectTransform.localScale;
            scale.x = flipX ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            image.rectTransform.localScale = scale;
        }

        private void OnCloseButtonClicked()
        {
            if (_closeClicked != null)
            {
                _closeClicked();
            }
            else
            {
                RequestHide();
            }
        }

        private void OnQuestsTabButtonClicked()
        {
            ShowQuests();
        }

        private void OnFreeCoinTabButtonClicked()
        {
            ShowFreeCoin();
        }

        private void OnCloseAnimationFinished()
        {
            base.Hide();
        }
    }
}
