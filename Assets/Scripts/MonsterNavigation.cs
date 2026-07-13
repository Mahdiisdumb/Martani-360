using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
public class MonsterNavigation : MonoBehaviour
{
    [Header("Detection")]
    public float DetectionRange = 5f;
    public string tagString = "Player";
    [Header("Movement")]
    public float MonsterSpeedWander = 5f;
    public float MonsterSpeedChase = 12f;
    public int currentPoint = 0;
    private Vector3 lastTargetPosition;
    public float RepathDistance = 0.5f;
    public NavMeshAgent agent;
    public Transform[] points;
    [Header("Audio")]
    public AudioClip ChaseMusic;
    public AudioClip WanderMusic;
    public AudioClip TransitionBeforeChase;
    public float MusicFadeTime = 2f;
    private AudioSource musicA;
    private AudioSource musicB;
    private AudioSource activeMusic;
    private AudioSource fadingMusic;
    private Coroutine fadeRoutine;
    private Coroutine transitionRoutine;
    private enum MonsterState
    {
        Wander,
        Transitioning,
        Chase
    }
    private MonsterState currentState = MonsterState.Wander;
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = MonsterSpeedWander;
        agent.acceleration = 200f;
        agent.angularSpeed = 1200f;
        agent.autoBraking = false;
        agent.stoppingDistance = 1f;
        SetupAudio();
        StartCoroutine(InitializeMonster());
    }
    IEnumerator InitializeMonster()
    {
        yield return new WaitForSeconds(0.2f);
        PlaceOnNavMesh();
        Wander();
        CrossFadeMusic(WanderMusic, true);
    }
    void SetupAudio()
    {
        musicA = gameObject.AddComponent<AudioSource>();
        musicB = gameObject.AddComponent<AudioSource>();
        ConfigureAudio(musicA);
        ConfigureAudio(musicB);
        activeMusic = musicA;
        fadingMusic = musicB;
    }
    void ConfigureAudio(AudioSource source)
    {
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0;
        source.spatialBlend = 0;
    }
    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            if (agent.enabled)
                agent.enabled = false;
            return;
        }
        if (!agent.enabled)
        {
            agent.enabled = true;
        }
        if (!agent.isOnNavMesh)
        {
            PlaceOnNavMesh();
            return;
        }
        GameObject[] players =
            GameObject.FindGameObjectsWithTag(tagString);
        GameObject target = null;
        float closestDistance = Mathf.Infinity;
        foreach (GameObject player in players)
        {
            float distance =
                Vector3.Distance(
                    transform.position,
                    player.transform.position
                );
            if (distance < DetectionRange &&
               distance < closestDistance)
            {
                closestDistance = distance;
                target = player;
            }
        }
        if (target != null)
        {
            agent.speed = MonsterSpeedChase;
            Chase(target.transform);
            if (currentState == MonsterState.Wander)
            {
                if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    agent.ResetPath();
                    return;
                }
                BeginChaseSequence();
            }
        }
        else
        {
            agent.speed = MonsterSpeedWander;
            if (!agent.pathPending &&
               agent.remainingDistance <= 0.5f)
            {
                Wander();
            }
            if (currentState != MonsterState.Wander)
            {
                BackToWander();
            }
        }
    }
    void Chase(Transform target)
    {
        if (!agent.isOnNavMesh)
            return;
        if (Vector3.Distance(lastTargetPosition, target.position) > RepathDistance)
        {
            lastTargetPosition = target.position;
            agent.SetDestination(target.position);
        }
    }
    void Wander()
    {
        if (points == null || points.Length == 0)
            return;
        if (!agent.isOnNavMesh)
            return;
        agent.SetDestination(points[currentPoint].position);
        currentPoint++;
        if (currentPoint >= points.Length)
            currentPoint = 0;
    }
    void BeginChaseSequence()
    {
        currentState =
            MonsterState.Transitioning;
        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);
        transitionRoutine =
            StartCoroutine(
                ChaseTransition()
            );
    }
    IEnumerator ChaseTransition()
    {
        if (TransitionBeforeChase != null)
        {
            CrossFadeMusic(
                TransitionBeforeChase,
                false
            );
            yield return new WaitForSeconds(
                TransitionBeforeChase.length
            );
        }
        currentState =
            MonsterState.Chase;
        CrossFadeMusic(
            ChaseMusic,
            true
        );
    }
    void BackToWander()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }
        currentState =
            MonsterState.Wander;
        CrossFadeMusic(
            WanderMusic,
            true
        );
    }
    void CrossFadeMusic(AudioClip clip, bool loop)
    {
        if (clip == null)
            return;
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine =
            StartCoroutine(
                FadeMusic(clip, loop)
            );
    }
    IEnumerator FadeMusic(AudioClip clip, bool loop)
    {
        fadingMusic = activeMusic;
        activeMusic =
            activeMusic == musicA
            ? musicB
            : musicA;
        activeMusic.clip = clip;
        activeMusic.loop = loop;
        activeMusic.volume = 0;
        activeMusic.Play();
        float timer = 0;
        while (timer < MusicFadeTime)
        {
            timer += Time.deltaTime;
            float t =
                timer / MusicFadeTime;
            activeMusic.volume =
                Mathf.Lerp(0, 1, t);
            fadingMusic.volume =
                Mathf.Lerp(1, 0, t);
            yield return null;
        }
        activeMusic.volume = 1;
        fadingMusic.Stop();
        fadingMusic.volume = 0;
    }
    void PlaceOnNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(
            transform.position,
            out hit,
            5f,
            NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
        else
        {
            Debug.LogError(
                "Monster cannot find NavMesh!"
            );
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            DetectionRange
        );
    }
}