using Photon.Pun;
using UnityEngine;
using System.Collections;
using Photon.VR;
using System.Collections.Generic;
using Photon.Realtime;

public class TagManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView PTView;
    public float EndTime;
    public int PeopleNeedToStartRound;
    public string QueueName;
    public PhotonVRManager PTManager;

    [Header("NONO SPOT")]
    public bool IsMaster;
    public TagHitbox[] HitBoxes;
    public int TaggedPeople;
    public bool CanStartNewRound = true;
    public int RandomStartPlayerNumb = -1;
    public TagHitbox NewTaggedPlayer;
    public int TotalPlayers;
    public bool IsInTagQueue;
    public List<Tagger> TaggingHands;

    private void FixedUpdate()
    {
        IsInTagQueue = PTManager.DefaultQueue == QueueName;
        EnableTagState(IsInTagQueue);

        if (IsInTagQueue)
        {
            CheckIfMaster();
            if (IsMaster)
            {
                TotalPlayers = PhotonNetwork.CurrentRoom?.Players?.Count ?? 0;
                CheckTaggedPlayers();
                CheckIfRoundShouldEnd();
                CheckIfRoundShouldStart();
            }
        }
    }

    private void EnableTagState(bool state)
    {
        if (TaggingHands != null)
        {
            foreach (Tagger t in TaggingHands)
                if (t != null) t.enabled = state;
        }

        HitBoxes = FindObjectsOfType<TagHitbox>();
        foreach (TagHitbox t in HitBoxes)
            if (t != null) t.enabled = state;
    }

    public void CheckIfMaster()
    {
        IsMaster = PhotonNetwork.IsMasterClient;
        Debug.Log(IsMaster ? "I Like Men" : "I Don't Like Men");
    }

    public void CheckTaggedPlayers()
    {
        TaggedPeople = 0;
        HitBoxes = FindObjectsOfType<TagHitbox>();
        foreach (TagHitbox t in HitBoxes)
        {
            if (t != null && t.IsTag && t.gameObject.activeInHierarchy && t.enabled)
            {
                TaggedPeople++;
            }
        }
    }

    public void CheckIfRoundShouldStart()
    {
        if (TaggedPeople == 0 && CanStartNewRound && TotalPlayers >= PeopleNeedToStartRound)
        {
            StartCoroutine(StartRound());
        }
    }

    public IEnumerator StartRound()
    {
        if (!CanStartNewRound || HitBoxes == null || HitBoxes.Length == 0)
        {
            Debug.LogWarning("Cannot start round. Either CanStartNewRound is false or HitBoxes array is empty.");
            yield break;
        }

        RandomStartPlayerNumb = (RandomStartPlayerNumb < 0 || RandomStartPlayerNumb >= HitBoxes.Length)
            ? Random.Range(0, HitBoxes.Length)
            : RandomStartPlayerNumb;

        NewTaggedPlayer = HitBoxes[RandomStartPlayerNumb];
        if (NewTaggedPlayer != null)
        {
            PhotonView newTaggedPV = NewTaggedPlayer.GetComponent<PhotonView>();
            if (newTaggedPV != null)
            {
                newTaggedPV.RPC(nameof(TagHitbox.OnHit), RpcTarget.AllBuffered);
            }
        }

        yield return null;
    }

    public void CheckIfRoundShouldEnd()
    {
        if (TotalPlayers > 0 && TotalPlayers <= TaggedPeople)
        {
            EndRound();
        }
    }

    public void EndRound()
    {
        RandomStartPlayerNumb = -1;
        foreach (TagHitbox t in HitBoxes)
        {
            if (t != null)
            {
                PhotonView hitboxPV = t.GetComponent<PhotonView>();
                if (hitboxPV != null)
                {
                    hitboxPV.RPC(nameof(TagHitbox.EndRound), RpcTarget.AllBuffered);
                }
            }
        }
        StartCoroutine(EndRoundWait());
    }

    private IEnumerator EndRoundWait()
    {
        CanStartNewRound = false;
        yield return new WaitForSeconds(EndTime);
        CanStartNewRound = true;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        foreach (TagHitbox hitbox in FindObjectsOfType<TagHitbox>())
        {
            PhotonView pv = hitbox.GetComponent<PhotonView>();
            if (pv != null && pv.OwnerActorNr == otherPlayer.ActorNumber)
            {
                hitbox.IsTag = false;
            }
        }
        TaggingHands.RemoveAll(hand => hand.photonView.OwnerActorNr == otherPlayer.ActorNumber);
        CheckTaggedPlayers();
        Debug.Log($"Player {otherPlayer.NickName} left. Phantom tagger state cleared.");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Optional: add network sync here
    }
}