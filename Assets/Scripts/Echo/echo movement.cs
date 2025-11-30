using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class EchoMovementEnhanced : MonoBehaviour
{
    [Header("Player")]
    public Rigidbody playerBody;
    public Transform cameraRig;

    [Header("Hands")]
    public Transform leftHand;
    public Transform rightHand;
    public float grabRadius = 0.3f;
    public LayerMask grabbableMask;

    [Header("Thruster Controls")]
    public float thrusterForce = 20f;

    [Header("Boost Controls")]
    public float boostForce = 100f;
    public float boostCooldown = 1f;
    private float lastBoostTime = -1f;

    [Header("Brake Controls")]
    public float brakingStrength = 10f;

    [Header("Launch Tuning")]
    public float launchMultiplier = 3f;
    public float maxLaunchSpeed = 20f;
    public bool invertLaunchDirection = true;
    public float spinMultiplier = 10f;

    [Header("Rotation Tuning")]
    public float rotationTorque = 5f;

    private InputDevice leftDevice;
    private InputDevice rightDevice;

    private bool leftGrabbing = false;
    private bool rightGrabbing = false;

    private Vector3 leftAnchor;
    private Vector3 rightAnchor;
    private Vector3 previousLeftPos;
    private Vector3 previousRightPos;

    private Queue<Vector3> leftVelocities = new();
    private Queue<Vector3> rightVelocities = new();
    private int velocitySampleSize = 5;

    void Start()
    {
        CacheDevice(ref leftDevice, XRNode.LeftHand);
        CacheDevice(ref rightDevice, XRNode.RightHand);
    }

    void Update()
    {
        CacheDevice(ref leftDevice, XRNode.LeftHand);
        CacheDevice(ref rightDevice, XRNode.RightHand);

        AddVelocitySample(leftVelocities, leftHand.position, ref previousLeftPos);
        AddVelocitySample(rightVelocities, rightHand.position, ref previousRightPos);

        HandleGrabbing();
        ApplyPlantMovement(leftGrabbing, leftHand, leftAnchor, previousLeftPos);
        ApplyPlantMovement(rightGrabbing, rightHand, rightAnchor, previousRightPos);

        HandleThrusters();
        HandleBoost();
        HandleBrake();
        HandleRotation();

        previousLeftPos = leftHand.position;
        previousRightPos = rightHand.position;
    }

    void CacheDevice(ref InputDevice device, XRNode node)
    {
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(node);
    }

    bool GetGripState(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        return device.TryGetFeatureValue(CommonUsages.gripButton, out var grip) && grip;
    }

    void HandleGrabbing()
    {
        bool leftGrip = GetGripState(XRNode.LeftHand);
        bool rightGrip = GetGripState(XRNode.RightHand);

        if (leftGrip && !leftGrabbing)
            TryPlant(leftHand, ref leftGrabbing, ref leftAnchor);
        if (rightGrip && !rightGrabbing)
            TryPlant(rightHand, ref rightGrabbing, ref rightAnchor);

        if (!leftGrip && leftGrabbing)
            ReleaseGrab(ref leftGrabbing, GetAverageVelocity(leftVelocities), leftHand);
        if (!rightGrip && rightGrabbing)
            ReleaseGrab(ref rightGrabbing, GetAverageVelocity(rightVelocities), rightHand);
    }

    void TryPlant(Transform hand, ref bool grabbingFlag, ref Vector3 anchor)
    {
        Collider[] hits = Physics.OverlapSphere(hand.position, grabRadius, grabbableMask);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Disc")) continue;
            anchor = hit.ClosestPoint(hand.position);
            grabbingFlag = true;

            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
            break;
        }
    }

    void ReleaseGrab(ref bool grabbingFlag, Vector3 handVelocity, Transform hand)
    {
        if (invertLaunchDirection)
            handVelocity = -handVelocity;

        if (handVelocity.magnitude > maxLaunchSpeed)
            handVelocity = handVelocity.normalized * maxLaunchSpeed;

        playerBody.AddForce(handVelocity * launchMultiplier, ForceMode.Impulse);

        Vector3 launchTorque = Vector3.Cross(hand.forward, handVelocity) * spinMultiplier;
        playerBody.AddTorque(launchTorque, ForceMode.Impulse);

        grabbingFlag = false;
    }

    void ApplyPlantMovement(bool isGrabbing, Transform hand, Vector3 anchor, Vector3 previousHandPos)
    {
        if (!isGrabbing) return;

        Vector3 handDelta = hand.position - previousHandPos;
        playerBody.MovePosition(playerBody.position - handDelta);

        playerBody.linearVelocity = Vector3.zero;
        playerBody.angularVelocity = Vector3.zero;

        Debug.DrawLine(hand.position, anchor, Color.yellow);
    }

    void HandleThrusters()
    {
        bool thrusterLeft = leftDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out var yKey) && yKey;
        bool thrusterRight = rightDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out var bKey) && bKey;

        if (thrusterLeft)
            playerBody.AddForce(leftHand.forward * thrusterForce, ForceMode.Impulse);
        if (thrusterRight)
            playerBody.AddForce(rightHand.forward * thrusterForce, ForceMode.Impulse);
    }

    void HandleBoost()
    {
        bool boostPressed = leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out var boostClick) && boostClick;

        if (boostPressed && Time.time - lastBoostTime > boostCooldown)
        {
            Vector3 boostDir = cameraRig.forward;
            playerBody.AddForce(boostDir * boostForce, ForceMode.Impulse);
            lastBoostTime = Time.time;
        }
    }

    void HandleBrake()
    {
        bool brakePressed = rightDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out var brakeClick) && brakeClick;
        if (brakePressed)
            playerBody.AddForce(-playerBody.linearVelocity * brakingStrength * Time.deltaTime, ForceMode.Acceleration);
    }

    void HandleRotation()
    {
        if (leftDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out var input))
        {
            playerBody.AddTorque(cameraRig.up * input.x * rotationTorque);
            playerBody.AddTorque(cameraRig.right * -input.y * rotationTorque);
        }
    }

    void AddVelocitySample(Queue<Vector3> queue, Vector3 currentPos, ref Vector3 previousPos)
    {
        Vector3 velocity = (currentPos - previousPos) / Time.deltaTime;
        if (queue.Count >= velocitySampleSize) queue.Dequeue();
        queue.Enqueue(velocity);
    }

    Vector3 GetAverageVelocity(Queue<Vector3> queue)
    {
        if (queue.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var v in queue) sum += v;
        return sum / queue.Count;
    }

    void OnDrawGizmosSelected()
    {
        if (leftHand)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(leftHand.position, grabRadius);
            Gizmos.DrawLine(leftHand.position, leftAnchor);
        }
        if (rightHand)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(rightHand.position, grabRadius);
            Gizmos.DrawLine(rightHand.position, rightAnchor);
        }
    }
}