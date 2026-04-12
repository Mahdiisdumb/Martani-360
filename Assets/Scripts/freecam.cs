using UnityEngine;

public class ContinuousFreeCam : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public bool useCameraForward = true;

    [Header("Optional vertical control")]
    public bool allowVerticalLook = true;

    void Update()
    {
        MoveForward();
    }

    void MoveForward()
    {
        Vector3 forward;

        if (useCameraForward)
        {
            forward = Camera.main.transform.forward;
        }
        else
        {
            forward = transform.forward;
        }

        if (!allowVerticalLook)
        {
            forward.y = 0;
            forward.Normalize();
        }

        transform.position += forward * speed * Time.deltaTime;
    }
}