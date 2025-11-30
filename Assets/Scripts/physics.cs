using UnityEngine;

public class PhysicsforlavscubesintagFUCKINGHELL : MonoBehaviour
{
    public float gravityStrength = 20f;
    public float bounceMultiplier = 1.4f;
    public float initialJumpForce = 10f;
    public LayerMask bounceLayerMask;

    private Vector3 velocity;

    void Awake()
    {
        // Launch immediately on enable
        velocity.y = initialJumpForce;
    }

    void Update()
    {
        velocity.y -= gravityStrength * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & bounceLayerMask) != 0)
        {
            ContactPoint contact = collision.contacts[0];
            Vector3 normal = contact.normal;
            velocity = Vector3.Reflect(velocity, normal) * bounceMultiplier;
        }
    }
}