using UnityEngine;

public class teleportfuckassplace: MonoBehaviour
{
    public Transform GorillaPlayer;
    public Transform TeleportLocation;
    public LayerMask defaultLayerMask;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            var player = GorillaLocomotion.Player.Instance;
            var rb = GorillaPlayer.GetComponent<Rigidbody>();

            player.locomotionEnabledLayers = defaultLayerMask;
            player.headCollider.enabled = false;
            player.bodyCollider.enabled = false;
            rb.isKinematic = true;

            GorillaPlayer.position = TeleportLocation.position;
            rb.linearVelocity = Vector3.zero;

            player.locomotionEnabledLayers = ~0;
            player.headCollider.enabled = true;
            player.bodyCollider.enabled = true;
            rb.isKinematic = false;
        }
    }
}