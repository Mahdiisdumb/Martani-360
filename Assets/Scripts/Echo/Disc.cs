using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ZeroGravityDisc : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private float hitMultiplier = 1.25f;
    [SerializeField] private float maxSpeed = 35f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = false;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;
    }

    private void OnCollisionEnter(Collision collision)
    {
        VRPhysicsHand hand =
            collision.collider.GetComponent<VRPhysicsHand>();

        if (hand == null)
            return;

        Vector3 hitVelocity = hand.Velocity;

        if (hitVelocity.sqrMagnitude < 0.01f)
            return;

        rb.AddForce(
            hitVelocity * hitMultiplier,
            ForceMode.Impulse
        );

        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity =
                rb.linearVelocity.normalized * maxSpeed;
        }
    }
}