using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessingDisabler : MonoBehaviour
{
    public Volume globalPostProcessingVolume;

    void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            if (globalPostProcessingVolume != null)
            {
                globalPostProcessingVolume.enabled = false;
                Debug.Log("Andriod proformance portocal activated.");
            }
            else
            {
                Debug.LogWarning("No Volume assigned. Assign your Global Volume in the inspector.");
            }
        }
    }
}