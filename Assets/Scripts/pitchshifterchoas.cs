using UnityEngine;

public class pitchshifterchoas : MonoBehaviour
{
public AudioSource audioSource;
public float[] Ranges;
    void Update()
    {
        audioSource.pitch = Random.Range(Ranges[0], Ranges[1]);
    }
}
