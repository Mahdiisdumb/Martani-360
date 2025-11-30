using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class ButtonApp : MonoBehaviour
{
    public string relativeWindowsPath = "DLC/EXAMPLE/APP.exe";
    public string androidPackageName;  // Package name for Android

    void Start()
    {
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(LaunchApp);
        }
        else
        {
            Debug.LogWarning("AppLauncher attached to a GameObject without a Button.");
        }
    }

    void LaunchApp()
    {
#if UNITY_STANDALONE_WIN
        LaunchWindowsApp();
#elif UNITY_ANDROID
        LaunchAndroidApp();
#else
        Debug.LogWarning("App launching not supported on this platform.");
#endif

        // Close Unity app after launching
        CloseUnityApp();
    }

    void LaunchWindowsApp()
    {
        // Get the root of the build folder (one level up from Application.dataPath)
        string buildRoot = Directory.GetParent(Application.dataPath).FullName;
        string fullPath = Path.Combine(buildRoot, relativeWindowsPath);

        if (File.Exists(fullPath))
        {
            System.Diagnostics.Process.Start(fullPath);
            Debug.Log("Launched Windows app: " + fullPath);
        }
        else
        {
            Debug.LogError("Windows app path not found: " + fullPath);
        }
    }

    void LaunchAndroidApp()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            AndroidJavaObject launchIntent = packageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", androidPackageName);

            if (launchIntent != null)
            {
                currentActivity.Call("startActivity", launchIntent);
                Debug.Log("Launched Android app: " + androidPackageName);
            }
            else
            {
                Debug.LogError("Android app not found: " + androidPackageName);
            }
        }
    }

    void CloseUnityApp()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in Editor
#elif UNITY_STANDALONE
        Application.Quit(); // Closes standalone build
#elif UNITY_ANDROID
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        activity.Call("finish"); // Closes Unity activity
#endif
    }
}