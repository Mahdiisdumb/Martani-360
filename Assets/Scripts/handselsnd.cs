using UnityEngine;

public class SoundTestSelectorHandTag : MonoBehaviour
{
    public SoundTestManager manager;

    [Header("Tags")]
    public string leftHandTag = "LeftHand";
    public string rightHandTag = "RightHand";

    [Header("Layers (optional)")]
    public LayerMask leftLayer;
    public LayerMask rightLayer;

    [Header("Cooldown")]
    public float inputCooldown = 0.3f;

    private float lastInputTime = 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastInputTime < inputCooldown)
            return;

        // TAG check
        if (other.CompareTag(leftHandTag))
        {
            manager.Prev();
            lastInputTime = Time.time;
            return;
        }

        if (other.CompareTag(rightHandTag))
        {
            manager.Next();
            lastInputTime = Time.time;
            return;
        }

        // LAYER check
        if ((leftLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            manager.Prev();
            lastInputTime = Time.time;
            return;
        }

        if ((rightLayer.value & (1 << other.gameObject.layer)) != 0)
        {
            manager.Next();
            lastInputTime = Time.time;
            return;
        }
    }
}