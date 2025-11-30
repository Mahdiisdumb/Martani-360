using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HandTrigger : MonoBehaviour
{
    public string HandTag;
    public string SceneToLoad;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            SceneManager.LoadScene(SceneToLoad);
        }
    }
}