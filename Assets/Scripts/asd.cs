using UnityEngine;

public class VRPhysicsHand : MonoBehaviour
{
    public Transform target;

    private Rigidbody rb;

    private Vector3 previousPosition;
    private Vector3 handVelocity;

    public Vector3 Velocity => handVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousSpeculative;

        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        if (target == null)
            return;

        Vector3 currentPosition = target.position;

        handVelocity =
            (currentPosition - previousPosition) /
            Time.fixedDeltaTime;

        rb.MovePosition(currentPosition);
        rb.MoveRotation(target.rotation);

        previousPosition = currentPosition;
    }
}