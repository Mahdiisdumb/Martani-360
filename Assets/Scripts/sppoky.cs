// Relic: Radius-bound dialogue trigger
using UnityEngine;

public class RadiusDialogueTrigger : MonoBehaviour
{
    public string targetTag = "RelicTag";       // Tag of wandering relics
    public float activationRadius = 5f;         // Radius of awakening
    public Transform centerPoint;               // Ritual origin
    public GameObject dialogueObject;           // What awakens when relic enters radius

    void Update()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);
        bool relicInRange = false;

        foreach (GameObject obj in taggedObjects)
        {
            float distance = Vector3.Distance(centerPoint.position, obj.transform.position);

            if (distance <= activationRadius)
            {
                relicInRange = true;
                break; // One relic is enough to awaken the glyph
            }
        }

        if (dialogueObject != null)
        {
            dialogueObject.SetActive(relicInRange);
            // Log: Dialogue glyph toggled based on relic proximity
        }
    }

    void OnDrawGizmos()
    {
        if (centerPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(centerPoint.position, activationRadius);
            // Log: Radius glyph rendered in magenta
        }
    }
}