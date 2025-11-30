using Photon.Pun;
using UnityEngine;

public class Tagger : MonoBehaviourPun
{
    public string HitboxTag;
    [Header("NONO SPOT")]
    public bool IsTag;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HitboxTag))
        {
            PhotonView otherPV = other.GetComponent<PhotonView>();
            if (otherPV != null && otherPV.Owner != PhotonNetwork.LocalPlayer)
            {
                TagHitbox hitbox = other.GetComponent<TagHitbox>();
                if (hitbox != null && !hitbox.IsTag && IsTag)
                {
                    otherPV.RPC(nameof(hitbox.OnHit), RpcTarget.AllBuffered);
                }
            }
        }
    }
}
