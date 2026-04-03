using UnityEngine;
using UnityEngine.UI;

public class TribuiteHandler : MonoBehaviour
{
    [Header("Target to activate")]
    public GameObject targetObject;

    [Header("Parent containing all tribute objects")]
    public Transform tributeParent;

    [Header("UI Text to show music name")]
    public Text nowPlayingText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnButtonClick);
        else
            Debug.LogWarning("No Button component found!");
    }

    private void OnButtonClick()
    {
        if (targetObject == null || tributeParent == null)
        {
            Debug.LogWarning("Missing references!");
            return;
        }

        // Disable all children in the parent
        foreach (Transform child in tributeParent)
        {
            child.gameObject.SetActive(false);
        }

        // Enable selected object
        targetObject.SetActive(true);

        // Find and play audio
        AudioSource audio = targetObject.GetComponentInChildren<AudioSource>();
        if (audio != null)
        {
            audio.Play();

            // Update UI text
            if (nowPlayingText != null)
            {
                string clipName = audio.clip != null ? audio.clip.name : "Unknown Track";
                nowPlayingText.text = "Now Playing: " + clipName;
            }
        }
        else
        {
            Debug.LogWarning("No AudioSource found!");
        }
    }
}