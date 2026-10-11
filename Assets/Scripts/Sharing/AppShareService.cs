using System;
using Cysharp.Threading.Tasks;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.Android;
#endif

namespace DefaultNamespace.Sharing
{
    public class AppShareService
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public bool IsAppSharingSupported => true;

        /// <summary>
        /// Opens app sharing with the Google Play link. Completion doesn't confirm that anything was sent.
        /// </summary>
        public async UniTask<bool> TryShareAppAsync()
        {
            string appStoreUrl = $"https://play.google.com/store/apps/details?id={Application.identifier}";
            var shareCompletionSource = new UniTaskCompletionSource<bool>();

            try
            {
                try
                {
                    AndroidApplication.InvokeOnUIThread(() =>
                    {
                        try
                        {
                            using var intentClass = new AndroidJavaClass("android.content.Intent");
                            using var shareIntent = new AndroidJavaObject("android.content.Intent", intentClass.GetStatic<string>("ACTION_SEND"));
                            using var typedShareIntent = shareIntent.Call<AndroidJavaObject>("setType", "text/plain");
                            using var populatedShareIntent = shareIntent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), appStoreUrl);
                            using var chooserIntent = intentClass.CallStatic<AndroidJavaObject>("createChooser", shareIntent, (AndroidJavaObject)null);
                            AndroidApplication.currentActivity.Call("startActivity", chooserIntent);
                            shareCompletionSource.TrySetResult(true);
                        }
                        catch (Exception exception)
                        {
                            shareCompletionSource.TrySetException(exception);
                        }
                    });

                    return await shareCompletionSource.Task;
                }
                finally
                {
                    await UniTask.SwitchToMainThread();
                }
            }
            catch (AndroidJavaException exception)
            {
                Debug.LogWarning($"App sharing launch failed: {exception}");
                return false;
            }
        }
#else
        public bool IsAppSharingSupported => false;

        /// <summary>
        /// App sharing requires an Android player.
        /// </summary>
        public UniTask<bool> TryShareAppAsync()
        {
            return UniTask.FromException<bool>(new PlatformNotSupportedException("App sharing requires an Android player outside the Unity Editor."));
        }
#endif
    }
}
