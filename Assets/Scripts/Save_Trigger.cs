using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class Save_Trigger : MonoBehaviour
{
    [Header("Persistent Trigger")]

    public string StateTrigger = "SaveState";
    public string TagToCheck = "Player";

    [Header("Object Swap")]
    public GameObject Replacement;
    public GameObject Orgin;

    [Header("Scene Change")]
    public bool sceneChangeTrigger = false;
    public string sceneToChange = "";

    private bool triggered;

    private void Start()
    {
        triggered = PlayerPrefs.GetInt(StateTrigger, 0) == 1;

        if (!triggered)
            return;

        ApplyChanges();

        if (sceneChangeTrigger && !string.IsNullOrEmpty(sceneToChange))
        {
            SceneManager.LoadScene(sceneToChange);
            return;
        }

        GetComponent<Collider>().enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (!other.CompareTag(TagToCheck))
            return;

        triggered = true;

        // Save only. Do NOT apply changes now.
        PlayerPrefs.SetInt(StateTrigger, 1);
        PlayerPrefs.Save();

        // Prevent multiple saves this session.
        GetComponent<Collider>().enabled = false;
    }

    private void ApplyChanges()
    {
        if (Orgin != null)
            Orgin.SetActive(false);

        if (Replacement != null)
            Replacement.SetActive(true);
    }
}