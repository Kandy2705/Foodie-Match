using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace FoodieMatch.UI.Social
{
    internal static class SocialShareService
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void FoodieMatchShareText(
            string title,
            string message);
#endif

        public static void Share(string title, string message)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ShareOnAndroid(title, message);
#elif UNITY_IOS && !UNITY_EDITOR
            FoodieMatchShareText(title, message);
#else
            GUIUtility.systemCopyBuffer = message;
            Debug.Log(
                $"Native sharing is unavailable in the Editor. " +
                $"Copied share content to the clipboard: {message}");
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static void ShareOnAndroid(string title, string message)
        {
            try
            {
                using AndroidJavaClass intentClass =
                    new("android.content.Intent");
                using AndroidJavaObject intent =
                    new("android.content.Intent");

                intent.Call<AndroidJavaObject>(
                    "setAction",
                    "android.intent.action.SEND");
                intent.Call<AndroidJavaObject>(
                    "setType",
                    "text/plain");
                intent.Call<AndroidJavaObject>(
                    "putExtra",
                    "android.intent.extra.SUBJECT",
                    title);
                intent.Call<AndroidJavaObject>(
                    "putExtra",
                    "android.intent.extra.TEXT",
                    message);

                using AndroidJavaObject chooser =
                    intentClass.CallStatic<AndroidJavaObject>(
                        "createChooser",
                        intent,
                        title);
                using AndroidJavaClass unityPlayer =
                    new("com.unity3d.player.UnityPlayer");
                using AndroidJavaObject activity =
                    unityPlayer.GetStatic<AndroidJavaObject>(
                        "currentActivity");

                activity.Call(
                    "startActivity",
                    chooser);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
#endif
    }
}
