using System;
using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Common
{
    public sealed class ActionFeedbackView : MonoBehaviour
    {
        private static readonly int ShowTrigger = Animator.StringToHash("Show");
        private static readonly int NormalTrigger = Animator.StringToHash("Normal");

        [SerializeField] private TMP_Text _messageText;
        [SerializeField] private Animator _animator;

        private Vector2 _basePosition;
        private Action<ActionFeedbackView> _hidden;

        private void Awake()
        {
            _basePosition = ((RectTransform)transform).anchoredPosition;
        }

        public void Show(string message, Action<ActionFeedbackView> hidden)
        {
            _messageText.text = message;
            _hidden = hidden;
            gameObject.SetActive(true);

            if (_animator.runtimeAnimatorController != null)
            {
                _animator.ResetTrigger(ShowTrigger);
                _animator.SetTrigger(ShowTrigger);
            }
        }

        public void Hide()
        {
            _hidden?.Invoke(this);
            Destroy(gameObject);
        }

        public void SetNormal()
        {
            if (_animator.runtimeAnimatorController != null)
            {
                _animator.ResetTrigger(ShowTrigger);
                _animator.SetTrigger(NormalTrigger);
            }
        }

        public void SetStackIndex(int index)
        {
            RectTransform rectTransform = (RectTransform)transform;
            float verticalOffset = index * (rectTransform.rect.height + 24f);
            rectTransform.anchoredPosition = _basePosition + Vector2.up * verticalOffset;
        }
    }
}
