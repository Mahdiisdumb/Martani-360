using UnityEngine;
using System.Collections.Generic;

public class RadiusAudioTriggerForHorror : MonoBehaviour
{
    [Header("Setup")]
    public string targetTag = "Player";
    public Transform centerPoint;
    public float zoneSize = 10f;

    [Header("Horror Audio Zones")]
    public AudioSource farAudio;     // Ambient rustle / unease
    public AudioSource closeAudio;   // Whisper textures / static
    public AudioSource closerAudio;  // Drone distortions / proximity warnings
    public AudioSource chaseAudio;   // Panic heartbeat / aggression

    private AudioSource[] allAudio;
    private bool playerInside = false;

    void Start()
    {
        allAudio = new AudioSource[] { farAudio, closeAudio, closerAudio, chaseAudio };
    }

    void Update()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        bool anyInside = false;

        foreach (GameObject target in targets)
        {
            float distance = Vector3.Distance(centerPoint.position, target.transform.position);

            if (distance <= zoneSize * allAudio.Length)
            {
                anyInside = true;
                int zoneIndex = Mathf.FloorToInt(distance / zoneSize);
                zoneIndex = Mathf.Clamp(zoneIndex, 0, allAudio.Length - 1);

                if (!playerInside)
                {
                    playerInside = true;
                    TriggerZoneAudio(zoneIndex);
                }
                else
                {
                    TriggerZoneAudio(zoneIndex);
                }
                break; // Only handle the first player inside
            }
        }

        if (!anyInside && playerInside)
        {
            playerInside = false;
            StopAllAudio();
        }
    }

    void TriggerZoneAudio(int activeIndex)
    {
        for (int i = 0; i < allAudio.Length; i++)
        {
            if (allAudio[i] == null) continue;

            if (i == activeIndex && !allAudio[i].isPlaying)
            {
                allAudio[i].Play();
                Debug.Log($"👁️ Martani Zone {i} activated.");
            }
            else if (i != activeIndex && allAudio[i].isPlaying)
            {
                allAudio[i].Stop();
            }
        }
    }

    void StopAllAudio()
    {
        foreach (var audio in allAudio)
        {
            if (audio != null && audio.isPlaying)
                audio.Stop();
        }
    }

    void OnDrawGizmos()
    {
        if (centerPoint == null) return;

        // Emotional depth: core panic to outer ambient
        Color[] zoneColors = new Color[]
        {
            new Color(0.9f, 0f, 0f),     // Zone 0 (Chase) – blood red
            new Color(1f, 0.5f, 0.2f),   // Zone 1 (Closer) – threat orange
            new Color(1f, 0.9f, 0.4f),   // Zone 2 (Close) – static yellow
            new Color(0.3f, 0.4f, 0.8f)  // Zone 3 (Far) – ambient blue
        };

        for (int i = 0; i < zoneColors.Length; i++)
        {
            Gizmos.color = zoneColors[i];

            // Chase zone (index 0) should be zoneSize * 1, Far zone (index 3) is zoneSize * 4
            float radius = zoneSize * (i + 1);
            Gizmos.DrawWireSphere(centerPoint.position, radius);
        }
    }
}