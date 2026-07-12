using UnityEngine;
using System.Collections;

public class RadiusAudioTriggerForHorror : MonoBehaviour
{
    [Header("Setup")]
    public string targetTag = "Player";
    public Transform centerPoint;
    public float zoneSize = 10f;

    [Header("Horror Audio Zones")]
    public AudioClip farAudio;
    public AudioClip closeAudio;
    public AudioClip closerAudio;
    public AudioClip chaseAudio;

    [Header("Crossfade Settings")]
    public float fadeTime = 2f;
    public float maxVolume = 1f;

    private AudioClip[] allAudio;

    private AudioSource sourceA;
    private AudioSource sourceB;

    private AudioSource activeSource;
    private AudioSource fadingSource;

    private int currentZone = -1;
    private Coroutine fadeRoutine;


    void Start()
    {
        allAudio = new AudioClip[]
        {
            chaseAudio,
            closerAudio,
            closeAudio,
            farAudio
        };


        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        SetupSource(sourceA);
        SetupSource(sourceB);

        activeSource = sourceA;
        fadingSource = sourceB;
    }


    void SetupSource(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0;
        source.spatialBlend = 1f; // 3D sound
    }


    void Update()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);

        bool foundPlayer = false;

        foreach (GameObject target in targets)
        {
            float distance = Vector3.Distance(
                centerPoint.position,
                target.transform.position
            );


            if (distance <= zoneSize * allAudio.Length)
            {
                foundPlayer = true;

                int zoneIndex = Mathf.FloorToInt(distance / zoneSize);
                zoneIndex = Mathf.Clamp(zoneIndex, 0, allAudio.Length - 1);


                if (zoneIndex != currentZone)
                {
                    currentZone = zoneIndex;
                    ChangeAudio(zoneIndex);
                }

                break;
            }
        }


        if (!foundPlayer && currentZone != -1)
        {
            currentZone = -1;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(FadeOutAll());
        }
    }


    void ChangeAudio(int index)
    {
        if (allAudio[index] == null)
            return;


        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);


        fadeRoutine = StartCoroutine(
            CrossFade(allAudio[index])
        );
    }


    IEnumerator CrossFade(AudioClip newClip)
    {
        fadingSource = activeSource;
        activeSource = (activeSource == sourceA) ? sourceB : sourceA;


        activeSource.clip = newClip;
        activeSource.volume = 0;
        activeSource.Play();


        float timer = 0;


        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float t = timer / fadeTime;

            activeSource.volume = Mathf.Lerp(
                0,
                maxVolume,
                t
            );

            fadingSource.volume = Mathf.Lerp(
                maxVolume,
                0,
                t
            );

            yield return null;
        }


        activeSource.volume = maxVolume;
        fadingSource.Stop();
        fadingSource.volume = 0;
    }


    IEnumerator FadeOutAll()
    {
        float timer = 0;

        float startA = sourceA.volume;
        float startB = sourceB.volume;


        while (timer < fadeTime)
        {
            timer += Time.deltaTime;

            float t = timer / fadeTime;

            sourceA.volume = Mathf.Lerp(startA, 0, t);
            sourceB.volume = Mathf.Lerp(startB, 0, t);

            yield return null;
        }


        sourceA.Stop();
        sourceB.Stop();

        sourceA.volume = 0;
        sourceB.volume = 0;
    }


    void OnDrawGizmos()
    {
        if (centerPoint == null)
            return;


        Color[] colors =
        {
            Color.red,
            new Color(1f,0.5f,0.2f),
            Color.yellow,
            Color.blue
        };


        for (int i = 0; i < colors.Length; i++)
        {
            Gizmos.color = colors[i];

            Gizmos.DrawWireSphere(
                centerPoint.position,
                zoneSize * (i + 1)
            );
        }
    }
}