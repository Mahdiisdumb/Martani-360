using UnityEngine;
using Photon.VR;

public class PhotonVRRigBinder : MonoBehaviour
{
    public Transform Head;
    public Transform LeftHand;
    public Transform RightHand;

    private void Start()
    {
        if (PhotonVRManager.Manager != null)
        {
            PhotonVRManager.Manager.Head = Head;
            PhotonVRManager.Manager.LeftHand = LeftHand;
            PhotonVRManager.Manager.RightHand = RightHand;

            Debug.Log("PhotonVRManager rig re-linked");
        }
    }
}