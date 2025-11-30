using UnityEngine;
using Photon.Pun;
using TMPro;

public class NetworkedTextDisplay : Photon.Pun.MonoBehaviourPunCallbacks
{
    [SerializeField] private TMP_Text tmpText3D; // TextMeshPro compnoent in 3D

    private string currentMessage;

    /// <summary>Call this locally to update and broadcast text.</summary>
    public void SetText(string newText)
    {
        if (photonView.IsMine)
        {
            currentMessage = newText;
            tmpText3D.text = newText;
            photonView.RPC("SyncText", RpcTarget.Others, newText);
        }
    }

    [PunRPC]
    private void SyncText(string newText)
    {
        currentMessage = newText;
        tmpText3D.text = newText;
    }
}