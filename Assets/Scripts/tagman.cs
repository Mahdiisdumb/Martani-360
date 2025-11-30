using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.VR;

namespace taging
{
    [RequireComponent(typeof(PhotonView))]
    public class TagManager : MonoBehaviourPun
    {
        public static TagManager Instance;

        private PhotonView view;
        private int currentTaggerActorNumber = -1;
        private bool canTag = true;
        private bool isActive = true;
        private const string tagableLayerName = "Tagable";

        private Coroutine colorMaintainLoop;
        private bool isLocalPlayerTagger = false;

        // Add this reference (assign in inspector or via code)
        public ColorScript colorScript;

        [System.Obsolete]
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            view = GetComponent<PhotonView>();
            if (view == null) Debug.LogError("🚨 TagManager missing PhotonView component.");

            // Try to auto-find if not set in inspector
            if (colorScript == null)
                colorScript = FindObjectOfType<ColorScript>();
        }

        private void Start()
        {
            StartCoroutine(InitializeTaggerRoutine());
        }

        private IEnumerator InitializeTaggerRoutine()
        {
            yield return new WaitForSeconds(1f); // Prevent race conditions during scene startup

            if (PhotonNetwork.IsMasterClient && isActive && PhotonNetwork.InRoom)
            {
                Photon.Realtime.Player firstPlayer = PhotonNetwork.PlayerList[0];
                int taggerActorNumber = firstPlayer.ActorNumber;

                view.RPC("SetTagger", RpcTarget.AllBuffered, taggerActorNumber);
                Debug.Log($"🟥 First player tagged on join — ActorNumber: {taggerActorNumber}");
            }
        }

        [System.Obsolete]
        private void OnDisable()
        {
            isActive = false;
            canTag = false;

            foreach (var player in FindObjectsOfType<TaggablePlayer>())
            {
                player.DisableTagging();
            }

            if (colorMaintainLoop != null) StopCoroutine(colorMaintainLoop);

            // Use ColorScript instead of PhotonVRManager
            if (colorScript != null)
            {
                colorScript.Red = 1f;
                colorScript.Green = 1f;
                colorScript.Blue = 1f;
                Debug.Log("🕊️ TagManager disabled — player color reset to white.");
            }
        }

        [PunRPC]
        [System.Obsolete]
        public void SetTagger(int actorNumber)
        {
            if (!isActive) return;

            currentTaggerActorNumber = actorNumber;

            foreach (var player in FindObjectsOfType<TaggablePlayer>())
            {
                bool isTagger = player.photonView.Owner.ActorNumber == actorNumber;
                player.SetTagger(isTagger);

                if (player.photonView.IsMine)
                {
                    isLocalPlayerTagger = isTagger;

                    if (colorMaintainLoop != null) StopCoroutine(colorMaintainLoop);
                    colorMaintainLoop = StartCoroutine(MaintainPlayerColorLoop());
                }
            }

            canTag = true;
        }

        private IEnumerator MaintainPlayerColorLoop()
        {
            while (isActive)
            {
                if (colorScript != null)
                {
                    if (isLocalPlayerTagger)
                    {
                        colorScript.Red = 9f;
                        colorScript.Green = 0f;
                        colorScript.Blue = 0f;
                    }
                    else
                    {
                        colorScript.Red = 0f;
                        colorScript.Green = 9f;
                        colorScript.Blue = 0f;
                    }
                }
                yield return new WaitForSeconds(1f); // Repeat to reassert color
            }
        }

        public void TryTag(GameObject target)
        {
            if (!isActive || !canTag || target.layer != LayerMask.NameToLayer(tagableLayerName)) return;

            PhotonView targetView = target.GetComponent<PhotonView>();
            if (targetView == null || targetView.Owner.ActorNumber == currentTaggerActorNumber) return;

            view.RPC("SetTagger", RpcTarget.AllBuffered, targetView.Owner.ActorNumber);
            StartCoroutine(TagCooldown());
        }

        private IEnumerator TagCooldown()
        {
            canTag = false;
            yield return new WaitForSeconds(5f);
            canTag = true;
        }
    }
}