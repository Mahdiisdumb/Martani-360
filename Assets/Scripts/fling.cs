using UnityEngine;

public class Fling : MonoBehaviour
{
    public float flingForce = 1000f; // Codex-level launch power
    public string flingTag = "Player"; // Tag for valid targets

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(flingTag))
        {
            // Traverse upward to find the Rigidbody in the scene
            Rigidbody rb = other.GetComponentInParent<Rigidbody>();

            if (rb != null)
            {
                Vector3 flingDirection = (Vector3.up * 1.5f + (other.transform.position - transform.position).normalized).normalized;
                rb.AddForce(flingDirection * flingForce, ForceMode.Impulse);

                Debug.Log($"[Codex Entry] Fling ritual activated on {rb.name} at {Time.time}.");
            }
            else
            {
                Debug.LogWarning($"[Codex Fracture] No Rigidbody found in parent hierarchy of {other.name}");
            }
        }
    }
}