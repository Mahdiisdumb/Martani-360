using UnityEngine;

public class HandTagTeleporter : MonoBehaviour
{
    public Transform teleportDestination;
    public GameObject objectToTeleport;
    public string handTag = "HandTag";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(handTag) && teleportDestination != null)
        {
            Rigidbody rb = objectToTeleport.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = teleportDestination.position;
                rb.linearVelocity = Vector3.zero; // Optional: reset motion
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                Debug.LogWarning("No Rigidbody found on objectToTeleport.");
            }
        }
    }
}