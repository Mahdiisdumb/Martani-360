using Photon.Pun;
using UnityEngine;
using System.Collections;
using Photon.VR;
using System.Collections.Generic;
using Photon.Realtime;

public class TagManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public PhotonView PTView;
    public float EndTime; // now used as the round DURATION (classic tag round length), not "wait before next round"
    public int PeopleNeedToStartRound;
    public string QueueName;
    public PhotonVRManager PTManager;

    [Header("NONO SPOT")]
    public bool IsMaster;
    public TagHitbox[] HitBoxes;
    public int TaggedPeople; // should always be 0 or 1 in classic tag (0 = no round running, 1 = current "it")
    public bool CanStartNewRound = true;
    public bool IsRoundActive; // true while a round is in progress (someone is "it")
    public bool MatchStartRequested; // set true when the master clicks the "Start Match" button
    public int RandomStartPlayerNumb = -1;
    public TagHitbox NewTaggedPlayer;   // the player who most recently became "it"
    public TagHitbox CurrentItPlayer;   // the single player who is currently "it"
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
                CheckIfRoundShouldStart();
            }
        }
    }

    /// <summary>
    /// Hook this up to a UI Button's OnClick event. Only the master client's
    /// click actually does anything, so it's safe to leave the button visible
    /// to everyone (or show/hide it based on PhotonNetwork.IsMasterClient in
    /// your UI code).
    /// </summary>
    public void OnStartMatchButtonPressed()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("Only the master client can start the match.");
            return;
        }

        MatchStartRequested = true;
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
    }

    public void CheckTaggedPlayers()
    {
        TaggedPeople = 0;
        CurrentItPlayer = null;
        HitBoxes = FindObjectsOfType<TagHitbox>();

        foreach (TagHitbox t in HitBoxes)
        {
            if (t != null && t.IsTag && t.gameObject.activeInHierarchy && t.enabled)
            {
                TaggedPeople++;
                CurrentItPlayer = t;
            }
        }

        // Safety net: classic tag should only ever have ONE "it" at a time.
        // If more than one hitbox reports IsTag (e.g. a network hiccup or a
        // TagHitbox that didn't clear itself when tagging someone else),
        // force everyone except the most recently tagged player back to false.
        if (TaggedPeople > 1)
        {
            Debug.LogWarning("More than one player is tagged as 'it' — forcing to a single tagger. " +
                "Check that TagHitbox untags the previous 'it' player when a new tag happens.");

            foreach (TagHitbox t in HitBoxes)
            {
                if (t != null && t != NewTaggedPlayer && t.IsTag)
                {
                    PhotonView pv = t.GetComponent<PhotonView>();
                    if (pv != null)
                    {
                        pv.RPC(nameof(TagHitbox.EndRound), RpcTarget.AllBuffered);
                    }
                }
            }

            TaggedPeople = 1;
            CurrentItPlayer = NewTaggedPlayer;
        }
    }

    public void CheckIfRoundShouldStart()
    {
        // Only start once the master has clicked the Start Match button.
        // Everything else (enough players, not already mid-round) still applies
        // as a safety check, but automatic/implicit starting is gone.
        if (MatchStartRequested && !IsRoundActive && TaggedPeople == 0 &&
            CanStartNewRound && TotalPlayers >= PeopleNeedToStartRound)
        {
            MatchStartRequested = false; // consume the request
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

        IsRoundActive = true;

        yield return null;
    }

    /// <summary>
    /// The round has no automatic end condition — once started it keeps running
    /// (the "it" status just keeps getting passed between players) until the
    /// master explicitly calls EndRound(), e.g. from a "Stop Match" button.
    /// </summary>
    public void EndRound()
    {
        RandomStartPlayerNumb = -1;
        IsRoundActive = false;

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
        yield return new WaitForSeconds(1f); // short cooldown before the master can start another match
        CanStartNewRound = true;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        bool leavingPlayerWasIt = false;

        foreach (TagHitbox hitbox in FindObjectsOfType<TagHitbox>())
        {
            PhotonView pv = hitbox.GetComponent<PhotonView>();
            if (pv != null && pv.OwnerActorNr == otherPlayer.ActorNumber)
            {
                if (hitbox.IsTag) leavingPlayerWasIt = true;
                hitbox.IsTag = false;
            }
        }

        TaggingHands.RemoveAll(hand => hand.photonView.OwnerActorNr == otherPlayer.ActorNumber);
        CheckTaggedPlayers();

        // Rounds never end on their own, so if the "it" player disconnects,
        // hand "it" to a new random remaining player instead of stopping the match.
        if (leavingPlayerWasIt && IsRoundActive && IsMaster)
        {
            RandomStartPlayerNumb = -1;
            StartCoroutine(StartRound());
        }

        Debug.Log($"Player {otherPlayer.NickName} left. Tag state cleared.");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // Optional: add network sync here
    }
}