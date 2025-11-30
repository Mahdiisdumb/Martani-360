using UnityEngine;
using System.Collections; //  Required for IEnumerator and coroutines
using UnityEngine.SceneManagement; // equired for scene management
public class SecondScene : MonoBehaviour
{
    public string sceneName = "YourSceneName";
    public float Seconds = 5f; // Replace with actual scene name
    void Start()
    {
        // Begin the ritual: wait 5 seconds, then shift realms
        StartCoroutine(ChangeSceneAfterDelay(Seconds));
    }

    IEnumerator ChangeSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName); //  Replace with actual scene name
    }
}