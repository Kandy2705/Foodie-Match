using TMPro;
using UnityEngine;

namespace FoodieMatch.UI.Common
{
    public static class UiTmpText
    {
        public static TMP_Text FindChild(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < texts.Length; i++)
            {
                TMP_Text text = texts[i];

                if (text != null && text.gameObject.name == objectName)
                {
                    return text;
                }
            }

            return null;
        }

        public static void SetText(TMP_Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;
        }
    }
}
