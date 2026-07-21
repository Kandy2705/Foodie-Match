using System.Threading.Tasks;
using FoodieMatch.UI.Popup;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Booster
{
    public sealed class FridgeBoosterPopup : PopupBase
    {
        [Header("Fridge")]
        [SerializeField] private RectTransform _fridgeRoot;
        [SerializeField] private Image _fridgeImage;
        [SerializeField] private Sprite _fridgeCloseSprite;
        [SerializeField] private Sprite _fridgeOpenSprite;
        [SerializeField] private Sprite _fridgeFullSprite;
        [SerializeField] private RectTransform _fridgeFoodEntryPoint;

        [Header("Spoon")]
        [SerializeField] private RectTransform _spoonRoot;
        [SerializeField] private Image _spoonImage;
        [SerializeField] private RectTransform _spoonFoodHoldPoint;

        [Header("Anchors")]
        [SerializeField] private RectTransform _fridgeOffscreenRightAnchor;
        [SerializeField] private RectTransform _fridgeVisibleAnchor;
        [SerializeField] private RectTransform _spoonStartAnchor;
        [SerializeField] private RectTransform _spoonExitLeftAnchor;
        [SerializeField] private RectTransform _foodCloneLayer;

        [Header("Animation")]
        [SerializeField] private float _fridgeEnterDuration = 0.35f;
        [SerializeField] private float _openGrowDuration = 0.14f;
        [SerializeField] private float _openSettleDuration = 0.09f;
        [SerializeField] private float _openScaleMultiplier = 1.2f;
        [SerializeField] private float _spoonExitDuration = 0.30f;

        private CanvasGroup _canvasGroup;
        private Sequence _animationSequence;
        private Vector3 _fridgeBaseScale;
        private Vector3 _spoonBaseScale;
        private bool _hasCapturedBaseScales;

        public RectTransform FoodCloneLayer => _foodCloneLayer;
        public RectTransform SpoonFoodHoldPoint => _spoonFoodHoldPoint;
        public RectTransform FridgeFoodEntryPoint => _fridgeFoodEntryPoint;

        private void Awake()
        {
            EnsureCanvasGroup();
            CaptureBaseScales();
            ValidateReferences();
        }

        private void OnDestroy()
        {
            CancelAnimations();
        }

        public override void Show()
        {
            base.Show();
            CancelAnimations();
            EnsureCanvasGroup();
            CaptureBaseScales();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (!ValidateReferences())
            {
                return;
            }

            SetFridgeSprite(_fridgeCloseSprite);
            SetAnchoredPosition(_fridgeRoot, _fridgeOffscreenRightAnchor);
            _fridgeRoot.localScale = _fridgeBaseScale;
            HideSpoonInternal();
            ResetSpoonPosition();
        }

        public async Task PlayEnterAndOpenAsync()
        {
            if (!ValidateReferences())
            {
                return;
            }

            CancelAnimations();
            SetCanvasNonBlocking();
            SetFridgeSprite(_fridgeCloseSprite);
            SetAnchoredPosition(_fridgeRoot, _fridgeOffscreenRightAnchor);
            _fridgeRoot.localScale = _fridgeBaseScale;

            Vector2 visiblePosition = _fridgeVisibleAnchor.anchoredPosition;
            Vector3 openScale = _fridgeBaseScale * _openScaleMultiplier;

            _animationSequence = Sequence.Create()
                .Chain(Tween.Custom(
                    this,
                    _fridgeRoot.anchoredPosition,
                    visiblePosition,
                    _fridgeEnterDuration,
                    (popup, position) => popup.SetFridgeAnchoredPosition(position),
                    Ease.OutCubic))
                .ChainCallback(this, popup => popup.SetFridgeSprite(popup._fridgeOpenSprite))
                .Chain(Tween.Scale(
                    _fridgeRoot,
                    openScale,
                    _openGrowDuration,
                    Ease.OutBack))
                .Chain(Tween.Scale(
                    _fridgeRoot,
                    _fridgeBaseScale,
                    _openSettleDuration,
                    Ease.OutQuad));

            await _animationSequence;
            _animationSequence = default;
            SetCanvasNonBlocking();
        }

        public void SetFullState()
        {
            if (_fridgeImage == null || _fridgeFullSprite == null)
            {
                ValidateReferences();
                return;
            }

            SetFridgeSprite(_fridgeFullSprite);
        }

        public void SetClosedState()
        {
            if (_fridgeImage == null || _fridgeCloseSprite == null)
            {
                ValidateReferences();
                return;
            }

            SetFridgeSprite(_fridgeCloseSprite);

            if (_fridgeRoot != null && _fridgeVisibleAnchor != null)
            {
                SetAnchoredPosition(_fridgeRoot, _fridgeVisibleAnchor);
            }
        }

        public async Task PlaySpoonExitLeftAsync()
        {
            if (!ValidateReferences() || !_spoonRoot.gameObject.activeSelf)
            {
                return;
            }

            CancelAnimations();
            SetCanvasNonBlocking();

            Vector2 exitPosition = _spoonExitLeftAnchor.anchoredPosition;

            _animationSequence = Sequence.Create(Tween.Custom(
                this,
                _spoonRoot.anchoredPosition,
                exitPosition,
                _spoonExitDuration,
                (popup, position) => popup.SetSpoonAnchoredPosition(position),
                Ease.InOutCubic));

            await _animationSequence;
            _animationSequence = default;
            HideSpoonInternal();
            ResetSpoonPosition();
            SetCanvasNonBlocking();
        }

        public void ShowSpoon()
        {
            if (!ValidateReferences())
            {
                return;
            }

            ResetSpoonPosition();
            _spoonRoot.gameObject.SetActive(true);
            _spoonRoot.localScale = _spoonBaseScale;
            _spoonImage.enabled = true;
            Color color = _spoonImage.color;
            color.a = 1f;
            _spoonImage.color = color;
        }

        public void HideImmediately()
        {
            CancelAnimations();
            base.Hide();
        }

        public void CancelAnimations()
        {
            if (_animationSequence.isAlive)
            {
                _animationSequence.Stop();
            }

            _animationSequence = default;
        }

        public Vector2 GetFridgeEntryScreenPosition()
        {
            return _fridgeFoodEntryPoint != null
                ? RectTransformUtility.WorldToScreenPoint(null, _fridgeFoodEntryPoint.position)
                : Vector2.zero;
        }

        public Vector2 GetSpoonHoldScreenPosition()
        {
            return _spoonFoodHoldPoint != null
                ? RectTransformUtility.WorldToScreenPoint(null, _spoonFoodHoldPoint.position)
                : Vector2.zero;
        }

        private void EnsureCanvasGroup()
        {
            if (_canvasGroup != null)
            {
                return;
            }

            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void CaptureBaseScales()
        {
            if (_hasCapturedBaseScales || _fridgeRoot == null || _spoonRoot == null)
            {
                return;
            }

            _fridgeBaseScale = _fridgeRoot.localScale;
            _spoonBaseScale = _spoonRoot.localScale;
            _hasCapturedBaseScales = true;
        }

        private bool ValidateReferences()
        {
            bool isValid = true;
            isValid &= ValidateReference(_fridgeRoot, nameof(_fridgeRoot));
            isValid &= ValidateReference(_fridgeImage, nameof(_fridgeImage));
            isValid &= ValidateReference(_fridgeCloseSprite, nameof(_fridgeCloseSprite));
            isValid &= ValidateReference(_fridgeOpenSprite, nameof(_fridgeOpenSprite));
            isValid &= ValidateReference(_fridgeFullSprite, nameof(_fridgeFullSprite));
            isValid &= ValidateReference(_fridgeFoodEntryPoint, nameof(_fridgeFoodEntryPoint));
            isValid &= ValidateReference(_spoonRoot, nameof(_spoonRoot));
            isValid &= ValidateReference(_spoonImage, nameof(_spoonImage));
            isValid &= ValidateReference(_spoonFoodHoldPoint, nameof(_spoonFoodHoldPoint));
            isValid &= ValidateReference(_fridgeOffscreenRightAnchor, nameof(_fridgeOffscreenRightAnchor));
            isValid &= ValidateReference(_fridgeVisibleAnchor, nameof(_fridgeVisibleAnchor));
            isValid &= ValidateReference(_spoonStartAnchor, nameof(_spoonStartAnchor));
            isValid &= ValidateReference(_spoonExitLeftAnchor, nameof(_spoonExitLeftAnchor));
            isValid &= ValidateReference(_foodCloneLayer, nameof(_foodCloneLayer));
            return isValid;
        }

        private bool ValidateReference(Object reference, string fieldName)
        {
            if (reference != null)
            {
                return true;
            }

            Debug.LogError($"FridgeBoosterPopup missing serialized reference: {fieldName}.", this);
            return false;
        }

        private void SetCanvasNonBlocking()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void SetFridgeSprite(Sprite sprite)
        {
            if (_fridgeImage != null)
            {
                _fridgeImage.sprite = sprite;
                _fridgeImage.enabled = sprite != null;
            }
        }

        private static void SetAnchoredPosition(RectTransform target, RectTransform anchor)
        {
            if (target != null && anchor != null)
            {
                target.anchoredPosition = anchor.anchoredPosition;
            }
        }

        private void SetFridgeAnchoredPosition(Vector2 anchoredPosition)
        {
            if (_fridgeRoot != null)
            {
                _fridgeRoot.anchoredPosition = anchoredPosition;
            }
        }

        private void SetSpoonAnchoredPosition(Vector2 anchoredPosition)
        {
            if (_spoonRoot != null)
            {
                _spoonRoot.anchoredPosition = anchoredPosition;
            }
        }

        private void ResetSpoonPosition()
        {
            if (_spoonRoot != null && _spoonStartAnchor != null)
            {
                _spoonRoot.anchoredPosition = _spoonStartAnchor.anchoredPosition;
                _spoonRoot.localScale = _spoonBaseScale;
            }
        }

        private void HideSpoonInternal()
        {
            if (_spoonImage != null)
            {
                _spoonImage.enabled = false;
            }

            if (_spoonRoot != null)
            {
                _spoonRoot.gameObject.SetActive(false);
            }
        }
    }
}
