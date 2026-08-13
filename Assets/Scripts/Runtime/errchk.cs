using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public static class GlobalErrorHandler
{
    private static bool handlingError = false;
    private static int exceptionCount = 0;
    private static float firstExceptionTime = 0f;

    // How many exceptions must happen before changing scenes
    private const int RequiredExceptions = 3;

    // Time window in which the exceptions must happen
    private const float ExceptionWindow = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Application.logMessageReceived += OnLogMessageReceived;
    }

    static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        if (handlingError || type != LogType.Exception)
            return;

        // Ignore some common non-fatal Unity exceptions
        if (condition.Contains("MissingReferenceException") ||
            condition.Contains("NullReferenceException"))
        {
            return;
        }

        float now = Time.realtimeSinceStartup;

        // Start a new exception window
        if (exceptionCount == 0 || now - firstExceptionTime > ExceptionWindow)
        {
            exceptionCount = 1;
            firstExceptionTime = now;
            return;
        }

        exceptionCount++;

        // Only switch scenes after multiple exceptions happen quickly
        if (exceptionCount >= RequiredExceptions)
        {
            handlingError = true;
            Application.logMessageReceived -= OnLogMessageReceived;

            Debug.LogWarning("Multiple unhandled exceptions detected. Loading error scene.");

            SceneManager.LoadScene("Err");
        }
    }
}