using UnityEngine;
using Photon.Pun;

namespace taging
{
    [RequireComponent(typeof(PhotonView))]
    public class TaggablePlayer : MonoBehaviourPun
    {
        public Renderer bodyRenderer;
        private bool isTagger = false;

        public void SetTagger(bool value)
        {
            isTagger = value;
            UpdateVisuals();
        }

        public void DisableTagging()
        {
            isTagger = false;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (photonView.IsMine) return; // Local visuals handled by PhotonVRManager.SetColour
            if (bodyRenderer != null)
                bodyRenderer.material.color = isTagger ? Color.red : Color.blue;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine || !isTagger || TagManager.Instance == null) return;
            TagManager.Instance.TryTag(other.gameObject);
        }
    }
}