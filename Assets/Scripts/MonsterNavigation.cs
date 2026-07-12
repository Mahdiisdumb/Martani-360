using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class MonsterNavigation : MonoBehaviour
{
    public float DetectionRange = 5f;
    public float MonsterSpeedWander = 5f;
    public float MonsterSpeedChase = 7.5f;
    public NavMeshAgent agent;
    public Transform[] points;
    public string tagString = "Player";
    public AudioClip ChaseMusic;
    public AudioClip WanderMusic;
    public AudioClip TransitionBeforeChase;
    public AudioSource audioSource;

    private enum MonsterState { Wander, Transitioning, Chase }
    private MonsterState currentState = MonsterState.Wander;
    private Coroutine transitionRoutine;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = MonsterSpeedWander;
        Wander();

        // Kick off wander music immediately.
        PlayMusic(WanderMusic, true);
    }

    void Update()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            agent.enabled = true;
            GameObject[] players = GameObject.FindGameObjectsWithTag(tagString);

            GameObject target = null;

            // Set the target to the closest player within range
            if (players.Length > 0)
            {
                float minDistance = float.MaxValue;
                foreach (GameObject player in players)
                {
                    float distance = Vector3.Distance(transform.position, player.transform.position);
                    if (distance < DetectionRange && distance < minDistance)
                    {
                        minDistance = distance;
                        target = player;
                    }
                }
            }

            if (target != null)
            {
                agent.speed = MonsterSpeedChase;
                Chase(target.transform);
                if (currentState == MonsterState.Wander)
                {
                    BeginChaseSequence();
                }
            }
            else
            {
                if (!agent.pathPending && agent.remainingDistance < 0.5f)
                {
                    agent.speed = MonsterSpeedWander;
                    Wander();
                }
                if (currentState != MonsterState.Wander)
                {
                    BackToWander();
                }
            }
        }
        else
        {
            agent.enabled = false;
        }
    }

    void Chase(Transform target)
    {
        agent.destination = target.position;
    }

    void Wander()
    {
        if (points.Length == 0)
            return;

        int destPoint = Random.Range(0, points.Length);
        agent.destination = points[destPoint].position;
    }

    void BeginChaseSequence()
    {
        currentState = MonsterState.Transitioning;

        if (transitionRoutine != null)
            StopCoroutine(transitionRoutine);

        transitionRoutine = StartCoroutine(TransitionToChaseRoutine());
    }

    IEnumerator TransitionToChaseRoutine()
    {
        if (TransitionBeforeChase != null)
        {
            PlayMusic(TransitionBeforeChase, false);
            yield return new WaitForSeconds(TransitionBeforeChase.length);
        }

        currentState = MonsterState.Chase;
        PlayMusic(ChaseMusic, true);
        transitionRoutine = null;
    }

    void BackToWander()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        currentState = MonsterState.Wander;
        PlayMusic(WanderMusic, true);
    }

    void PlayMusic(AudioClip clip, bool loop)
    {
        if (audioSource == null || clip == null)
            return;
        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.clip = clip;
        audioSource.loop = loop;
        audioSource.Play();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, DetectionRange);
    }
}