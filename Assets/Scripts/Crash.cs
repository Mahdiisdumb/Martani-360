using UnityEngine;

public class Crash : MonoBehaviour
{
    [Header("Trigger Settings")]
    public string HandTag;

    private bool hasCrashed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasCrashed || !other.CompareTag(HandTag)) return;

        Debug.Log("💥 Crash triggered by: " + other.name);
        hasCrashed = true;

        // Simulate a crash (force an exception)
        CauseCrash();
    }

    private void CauseCrash()
    {
        // This will throw an exception and crash the game
        Application.Quit();
    }
}