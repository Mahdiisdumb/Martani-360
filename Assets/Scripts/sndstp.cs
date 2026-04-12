using UnityEngine;

public class SoundTestStopButton : MonoBehaviour
{
    public SoundTestManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            manager.Stop();
        }
    }
}