using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneChanger : MonoBehaviour
{
    public string targetSceneName; // Set this in the Inspector

    void Start()
    {
        // Get the Button component attached to this GameObject
        Button button = GetComponent<Button>();

        // Wire up the click listener
        if (button != null)
        {
            button.onClick.AddListener(ChangeScene);
        }
        else
        {
            Debug.LogWarning("SceneChanger attached to a GameObject without a Button.");
        }
    }

    void ChangeScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }
}