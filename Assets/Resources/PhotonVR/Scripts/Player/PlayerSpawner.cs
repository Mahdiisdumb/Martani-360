using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace Photon.VR.Player
{
    public class PlayerSpawner : MonoBehaviourPunCallbacks
    {
        [Tooltip("The location of the player prefab")]
        public string PrefabLocation = "PhotonVR/Player";

        private GameObject playerTemp;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override void OnJoinedRoom()
        {
            if (!string.IsNullOrEmpty(PrefabLocation))
            {
                try
                {
                    playerTemp = PhotonNetwork.Instantiate(PrefabLocation, Vector3.zero, Quaternion.identity);
                    if (playerTemp != null)
                    {
                        Debug.Log("✅ Player instantiated: " + playerTemp.name);
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ Photon instantiated player was null.");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("❌ Failed to instantiate player: " + ex.Message);
                }
            }
            else
            {
                Debug.LogError("PrefabLocation is empty. Cannot spawn player.");
            }
        }

        public override void OnLeftRoom()
        {
            if (playerTemp != null)
            {
                if (PhotonNetwork.IsConnectedAndReady)
                {
                    try
                    {
                        PhotonNetwork.Destroy(playerTemp);
                        Debug.Log("✅ playerTemp destroyed safely on leave.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("❌ Error destroying playerTemp: " + ex.Message);
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ Photon not ready. Skipping destroy.");
                }

                playerTemp = null; // Clean reference
            }
            else
            {
                Debug.Log("ℹ️ playerTemp was already null when leaving room.");
            }
        }
    }
}