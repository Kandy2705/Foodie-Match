using UnityEngine;
using UnityEngine.UI;

namespace FoodieMatch.UI.Loading
{
    public sealed class AddressableLoadingOverlayView : MonoBehaviour
    {
        private const float SpinnerSize = 76f;
        private const float RotationSpeed = 220f;

        private RectTransform _spinner;

        public static AddressableLoadingOverlayView Create(
            Transform parent,
            Texture loadingTexture)
        {
            GameObject overlayObject = new(
                nameof(AddressableLoadingOverlayView),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(AddressableLoadingOverlayView));
            overlayObject.layer = parent.gameObject.layer;

            RectTransform overlayTransform =
                overlayObject.GetComponent<RectTransform>();
            overlayTransform.SetParent(parent, false);
            overlayTransform.anchorMin = Vector2.zero;
            overlayTransform.anchorMax = Vector2.one;
            overlayTransform.anchoredPosition = Vector2.zero;
            overlayTransform.sizeDelta = Vector2.zero;

            Image background = overlayObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.8f);
            background.raycastTarget = true;

            GameObject spinnerObject = new(
                "Spinner",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            spinnerObject.layer = parent.gameObject.layer;

            RectTransform spinnerTransform =
                spinnerObject.GetComponent<RectTransform>();
            spinnerTransform.SetParent(overlayTransform, false);
            spinnerTransform.anchorMin = new Vector2(0.5f, 0.5f);
            spinnerTransform.anchorMax = new Vector2(0.5f, 0.5f);
            spinnerTransform.anchoredPosition = Vector2.zero;
            spinnerTransform.sizeDelta =
                new Vector2(SpinnerSize, SpinnerSize);

            RawImage spinnerImage = spinnerObject.GetComponent<RawImage>();
            spinnerImage.texture = loadingTexture;
            spinnerImage.color = Color.white;
            spinnerImage.raycastTarget = false;

            AddressableLoadingOverlayView overlay =
                overlayObject.GetComponent<AddressableLoadingOverlayView>();
            overlay._spinner = spinnerTransform;
            overlayObject.SetActive(false);
            return overlay;
        }

        public void SetVisible(
            bool visible,
            Transform parent)
        {
            if (visible)
            {
                if (transform.parent != parent)
                {
                    transform.SetParent(parent, false);
                }

                transform.SetAsLastSibling();
            }

            gameObject.SetActive(visible);
        }

        private void Update()
        {
            _spinner.Rotate(
                0f,
                0f,
                -RotationSpeed * Time.unscaledDeltaTime);
        }
    }
}
