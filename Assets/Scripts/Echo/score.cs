using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class VRScoreOnHit : MonoBehaviour
{
    [Header("UI & Audio")]
    public TMP_Text scoreText;
    public GameObject scoreEffectPrefab;
    public AudioClip scoreSoundClip;
    public AudioClip twoPointAnnouncerClip;
    public AudioClip threePointAnnouncerClip;
    private AudioSource audioSource;

    [Header("Scoring Settings")]
    public float goalDelay = 5f;
    public bool isArenaGoal = false;

    [Header("FX Settings")]
    public float effectLifetime = 15f;
    public float flickerDuration = 2f;
    public float flickerFrequency = 0.1f;

    [Header("UI Pulse Settings")]
    public float pulseScale = 1.5f;
    public float pulseDuration = 0.2f;

    private int currentScore;
    private bool isScoringDelayed = false;

    void Start()
    {
        currentScore = 0;
        audioSource = GetComponent<AudioSource>();
        if (scoreText) scoreText.text = currentScore.ToString();
        else Debug.LogWarning("Score TextMeshPro reference is missing!");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isScoringDelayed) return;

        string discTag = isArenaGoal ? "Disc(Arena)" : "Disc";
        if (!collision.gameObject.CompareTag(discTag)) return;

        currentScore += 1;
        if (scoreText) scoreText.text = currentScore.ToString();

        StartCoroutine(ScoringCooldown(goalDelay));
        TriggerScoreEffects();
        TriggerAnnouncer(1);

        Debug.Log($"RelicScore → +1 pt | Total: {currentScore}");
    }

    void TriggerScoreEffects()
    {
        if (!scoreEffectPrefab) return;

        GameObject fx = Instantiate(scoreEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        fx.transform.parent = this.transform;

        StartCoroutine(ScoreEffectFlicker(fx));
        StartCoroutine(EffectTimeout(fx, effectLifetime));
        if (audioSource && scoreSoundClip) audioSource.PlayOneShot(scoreSoundClip);
        if (scoreText) StartCoroutine(ScoreTextPulse());

        Debug.Log($"RelicBurst → {fx.name} @ {transform.position}");
    }

    void TriggerAnnouncer(int points)
    {
        AudioClip clip = null;
        if (points == 2) clip = twoPointAnnouncerClip;
        else if (points == 3) clip = threePointAnnouncerClip;

        if (clip) StartCoroutine(DelayedAnnouncer(clip, 0.3f));
    }

    IEnumerator DelayedAnnouncer(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    IEnumerator ScoreTextPulse()
    {
        Vector3 original = scoreText.transform.localScale;
        Vector3 target = original * pulseScale;

        for (float t = 0f; t < pulseDuration; t += Time.deltaTime)
        {
            scoreText.transform.localScale = Vector3.Lerp(original, target, t / pulseDuration);
            yield return null;
        }

        for (float t = 0f; t < pulseDuration; t += Time.deltaTime)
        {
            scoreText.transform.localScale = Vector3.Lerp(target, original, t / pulseDuration);
            yield return null;
        }
    }

    IEnumerator ScoreEffectFlicker(GameObject obj)
    {
        float elapsed = 0f;
        while (elapsed < flickerDuration)
        {
            obj.SetActive(!obj.activeSelf);
            yield return new WaitForSeconds(flickerFrequency);
            elapsed += flickerFrequency;
        }
        obj.SetActive(true);
    }

    IEnumerator ScoringCooldown(float delay)
    {
        isScoringDelayed = true;
        yield return new WaitForSeconds(delay);
        isScoringDelayed = false;
    }

    IEnumerator EffectTimeout(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(obj);
    }
}