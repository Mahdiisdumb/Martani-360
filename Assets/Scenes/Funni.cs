using UnityEngine;
using UnityEngine.UI;

public class TribuiteHandler : MonoBehaviour
{
    [Header("Assign the GameObject to activate")]
    public GameObject targetObject;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClick);
        else
            Debug.LogWarning("No Button component found on this GameObject!");
    }

    private void OnButtonClick()
    {
        if (targetObject != null)
        {
            // Activate the GameObject
            targetObject.SetActive(true);

            // Look for an AudioSource in the target object or its children
            AudioSource audio = targetObject.GetComponentInChildren<AudioSource>();
            if (audio != null)
            {
                audio.Play();
            }
            else
            {
                Debug.LogWarning("No AudioSource found on the target GameObject or its children!");
            }
        }
        else
        {
            Debug.LogWarning("Target GameObject is not assigned!");
        }
    }
}