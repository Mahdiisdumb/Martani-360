using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class XRJump : MonoBehaviour
{
    [Header("Jump Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;

    CharacterController controller;
    InputDevice rightHandDevice;
    Vector3 velocity;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        TryInitializeRightHand();
    }

    void Update()
    {
        // Re-acquire device if lost
        if (!rightHandDevice.isValid)
            TryInitializeRightHand();

        // Infinite jump: reset vertical velocity on button press
        if (IsAButtonPressed())
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("Jump button pressed — infinite jump triggered.");
        }

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Move character
        controller.Move(velocity * Time.deltaTime);
    }

    bool IsAButtonPressed()
    {
        if (rightHandDevice.isValid &&
            rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
        {
            if (pressed)
                Debug.Log("Jump button detected (A/primaryButton) on right controller.");
            return pressed;
        }
        return false;
    }

    void TryInitializeRightHand()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
            rightHandDevice = devices[0];
    }
}