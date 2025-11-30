using UnityEngine;
using System.Diagnostics;
using System.IO;

public class AppLauncher : MonoBehaviour
{
    [Tooltip("Relative path from build root, e.g., DLC\\Example\\whateverapp.exe")]
    public string relativeAppPath = "DLC\\Example\\whateverapp.exe";

    public void LaunchApp()
    {
        // Get the folder where the game executable is running
        string buildRoot = Directory.GetParent(Application.dataPath).FullName;
        string fullAppPath = Path.Combine(buildRoot, relativeAppPath);

        if (File.Exists(fullAppPath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fullAppPath)
            {
                UseShellExecute = true
            };

            try
            {
                Process.Start(startInfo);
                UnityEngine.Debug.Log("Launched external app: " + fullAppPath);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError("Failed to launch app: " + ex.Message);
            }
        }
        else
        {
            UnityEngine.Debug.LogError("App path not found: " + fullAppPath);
        }
    }
}