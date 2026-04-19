using UnityEngine;
using UnityEngine.SceneManagement;

public class Save_Trigger : MonoBehaviour
{
    [Header("Trigger changes scene or objects permanently")]

    [Tooltip("Save key for PlayerPrefs")]
    public string StateTrigger = "SaveState";

    [Tooltip("Whether this has been triggered")]
    public bool Triggered = false;

    [Tooltip("Object to activate after trigger")]
    public GameObject Replacement;

    [Tooltip("Original object to disable")]
    public GameObject Orgin;

    [Tooltip("Tag required to activate trigger")]
    public string TagToCheck = "Player";

    [Tooltip("Is this a scene change trigger?")]
    public bool sceneChangeTrigger = false;

    [Tooltip("Scene to load if triggered")]
    public string sceneToChange = "somthing";

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(TagToCheck)) return;

        Triggered = true;

        PlayerPrefs.SetInt(StateTrigger, 1);
        PlayerPrefs.Save();

        ApplyChanges();

        if (sceneChangeTrigger)
        {
            SceneManager.LoadScene(sceneToChange);
        }
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(StateTrigger, 0) == 1)
        {
            Triggered = true;
            ApplyChanges();
        }
    }

    void ApplyChanges()
    {
        if (Replacement != null && Orgin != null)
        {
            Orgin.SetActive(false);
            Replacement.SetActive(true);
        }
    }
}