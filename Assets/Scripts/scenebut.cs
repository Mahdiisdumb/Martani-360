using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangerBut : MonoBehaviour
{
    [Tooltip("Name of the scene to load")]
    public string sceneName;

    public void ChangeScene()
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            SceneManager.LoadScene(sceneName);
            Debug.Log("Scene loaded: " + sceneName);
        }
        else
        {
            Debug.LogError("Scene not found in build settings: " + sceneName);
        }
    }
}