using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.VR;
using Photon.VR.Player;

public class PhotonVRDisconnector : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("Leave blank to skip scene change")]
    [SerializeField] private string nextSceneName = "";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            Debug.Log("🜏 HANDMARK CONTACT — Disconnecting from PhotonVR...");
            StartCoroutine(DisconnectAndTransition());
        }
    }

    private IEnumerator DisconnectAndTransition()
    {
        // Disable syncing scripts
        var player = FindObjectOfType<PhotonVRPlayer>();
        if (player != null && player.photonView.IsMine)
        {
            player.enabled = false;
            Debug.Log("PhotonVRPlayer disabled.");
        }

        // Clear manager references
        if (PhotonVRManager.Manager != null)
        {
            PhotonVRManager.Manager.Head = null;
            PhotonVRManager.Manager.LeftHand = null;
            PhotonVRManager.Manager.RightHand = null;
            Debug.Log("PhotonVRManager links nullified.");
        }

        // Leave room
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            while (PhotonNetwork.InRoom)
                yield return null;
        }

        // Disconnect fully
        PhotonNetwork.Disconnect();
        while (PhotonNetwork.IsConnected)
            yield return null;

        Debug.Log("🚪 PHOTON THREADS SEVERED");

        // Scene change (optional)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Debug.Log($"🌌 Scene change: {nextSceneName}");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}