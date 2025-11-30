using GorillaLocomotion;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TagHitbox : MonoBehaviour, IPunObservable
{
    [Header("Photon Sync")]
    public PhotonView PTView;

    [Header("On Tag")]
    public List<AudioClip> TagSounds;

    [Header("Tag Freeze")]
    public bool UseTagFreeze;
    public string GorillPlayerName;
    public float TagFreezeTime;

    [Header("Speed Boost")]
    public bool UseSpeedBoost;
    public float maxJumpSpeed;
    public float jumpMultiplier;
    public float velocityLimit;

    [Header("When Tagged")]
    public List<GameObject> TagEnable;
    public List<GameObject> TagDisable;

    [Header("Material Change")]
    public List<SkinnedMeshRenderer> PlayerRender;
    public Material UnTaggedMaterial;
    public Material TaggedMaterial;

    [Header("When Not Tagged")]
    public List<GameObject> UnTagEnable;
    public List<GameObject> UnTagDisable;

    [Header("NONO SPOT")]
    public Player GorillaPlayer;
    public Tagger[] Hands;
    public bool IsTag;
    public AudioSource Player;
    public float OmaxJumpSpeed;
    public float OjumpMultiplier;
    public float OvelocityLimit;

    void Start()
    {
        Player = GetComponent<AudioSource>();
        PTView = GetComponent<PhotonView>();
        Hands = FindObjectsOfType<Tagger>();

        if (UseTagFreeze || UseSpeedBoost)
        {
            BindGorillaPlayer();
        }

        if (UseSpeedBoost && GorillaPlayer != null)
        {
            OvelocityLimit = GorillaPlayer.velocityLimit;
            OjumpMultiplier = GorillaPlayer.jumpMultiplier;
            OmaxJumpSpeed = GorillaPlayer.maxJumpSpeed;
        }
    }

    void BindGorillaPlayer()
    {
        GameObject gorillaObject = GameObject.Find(GorillPlayerName);
        if (gorillaObject != null)
        {
            GorillaPlayer = gorillaObject.GetComponent<Player>();
            Debug.Log($"[BindGorillaPlayer] Found GorillaPlayer by name: {GorillPlayerName}");
        }

        if (GorillaPlayer == null)
        {
            Debug.LogWarning($"[BindGorillaPlayer] GameObject '{GorillPlayerName}' not found. Attempting fallback search...");
            Player[] allPlayers = FindObjectsOfType<Player>();
            foreach (Player p in allPlayers)
            {
                if (p.CompareTag("Player")) // Adjust tag as needed
                {
                    GorillaPlayer = p;
                    Debug.Log($"[BindGorillaPlayer] Fallback found GorillaPlayer: {p.name}");
                    break;
                }
            }
        }

        if (GorillaPlayer == null)
        {
            Debug.LogError($"[BindGorillaPlayer] GorillaPlayer not found. Ritual binding failed.");
        }
    }

    [PunRPC]
    public void OnHit()
    {
        if (IsTag) return;

        // Play tag sound
        if (TagSounds != null && TagSounds.Count > 0)
        {
            int RIDX = Random.Range(0, TagSounds.Count);
            AudioClip clip = TagSounds[RIDX];
            if (clip != null)
            {
                Player.clip = clip;
                Player.Play();
            }
        }
        else
        {
            Debug.LogWarning($"[OnHit] No TagSounds available.");
        }

        IsTag = true;

        // Set hands to tagged
        if (Hands != null)
        {
            foreach (Tagger t in Hands)
            {
                if (t != null) t.IsTag = true;
            }
        }

        // Material swap
        if (PlayerRender != null && TaggedMaterial != null)
        {
            foreach (SkinnedMeshRenderer PR in PlayerRender)
            {
                if (PR != null) PR.material = TaggedMaterial;
            }
        }

        // Freeze movement
        if (UseTagFreeze && GorillaPlayer != null)
        {
            StartCoroutine(TagFreeze());
        }

        // Speed boost
        if (UseSpeedBoost && GorillaPlayer != null)
        {
            AddSpeedBoost();
        }

        ListED(TagEnable, true);
        ListED(TagDisable, false);
    }

    [PunRPC]
    public void EndRound()
    {
        IsTag = false;

        if (PlayerRender != null && UnTaggedMaterial != null)
        {
            foreach (SkinnedMeshRenderer PR in PlayerRender)
            {
                if (PR != null) PR.material = UnTaggedMaterial;
            }
        }

        if (Hands != null)
        {
            foreach (Tagger t in Hands)
            {
                if (t != null) t.IsTag = false;
            }
        }

        if (UseSpeedBoost && GorillaPlayer != null)
        {
            RemoveSpeedBoost();
        }

        ListED(UnTagEnable, true);
        ListED(UnTagDisable, false);
    }

    public void AddSpeedBoost()
    {
        GorillaPlayer.maxJumpSpeed = maxJumpSpeed;
        GorillaPlayer.jumpMultiplier = jumpMultiplier;
        GorillaPlayer.velocityLimit = velocityLimit;
    }

    public void RemoveSpeedBoost()
    {
        GorillaPlayer.maxJumpSpeed = OmaxJumpSpeed;
        GorillaPlayer.jumpMultiplier = OjumpMultiplier;
        GorillaPlayer.velocityLimit = OvelocityLimit;
    }

    public void ListED(List<GameObject> list, bool state)
    {
        if (list != null)
        {
            foreach (GameObject obj in list)
            {
                if (obj != null) obj.SetActive(state);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(IsTag);
        }
        else
        {
            IsTag = (bool)stream.ReceiveNext();
            foreach (SkinnedMeshRenderer r in PlayerRender)
            {
                if (r != null)
                {
                    r.material = IsTag ? TaggedMaterial : UnTaggedMaterial;
                }
            }
        }
    }

    public IEnumerator TagFreeze()
    {
        GorillaPlayer.disableMovement = true;
        yield return new WaitForSeconds(TagFreezeTime);
        GorillaPlayer.disableMovement = false;
    }
}