using UnityEngine;
using Photon.Pun;
using Photon.VR;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PhotonView))]
public class GunManager : MonoBehaviourPun
{
    public GameObject gunPrefab;
    public int allowedSceneIndex = 3; // Only activate in this scene

    private void Start()
    {
        if (!photonView.IsMine) return;

        if (SceneManager.GetActiveScene().buildIndex == allowedSceneIndex)
        {
            AttachGunToRightHand();
        }
        else
        {
            Debug.Log("🔒 GunManager disabled in this scene.");
            enabled = false;
        }
    }

    void AttachGunToRightHand()
    {
        Transform rightHand = PhotonVRManager.Manager?.RightHand;
        if (rightHand == null)
        {
            Debug.LogError("🛑 RightHand reference missing in PhotonVRManager.");
            return;
        }

        GameObject gunInstance = Instantiate(gunPrefab, rightHand);
        gunInstance.transform.localPosition = Vector3.zero;
        gunInstance.transform.localRotation = Quaternion.identity;

        Debug.Log("🔫 Gun prefab attached to right hand.");
    }
}