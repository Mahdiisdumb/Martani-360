using UnityEngine;
using UnityEngine.SceneManagement;

public static class GlobalErrorHandler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Application.logMessageReceived += OnLogMessageReceived;
    }

    static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Exception)
        {
            Debug.Log("Unhandled exception detected!");

            Application.logMessageReceived -= OnLogMessageReceived;

            SceneManager.LoadScene("Err");
        }
    }
}