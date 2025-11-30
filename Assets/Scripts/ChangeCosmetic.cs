using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.VR;

public class ChangeCosmetic : MonoBehaviour
{
    public enum CosmeticType
    {
        Head,
        Face,
        Body,
        LeftHand,
        RightHand
    }

    [Header("Cosmetic Settings")]
    public CosmeticType type;
    public string cosmeticName;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("HandTag"))
        {
            Debug.Log($"[Codex] Triggered cosmetic change: Type={type}, Name={cosmeticName}");

            // Disable other cosmetics of the same type
            DisableOtherCosmetics(type);

            // Apply the new cosmetic
            PhotonVRManager.SetCosmetic(type.ToString(), cosmeticName);
            Debug.Log($"[Codex] Applied new cosmetic: {cosmeticName} to {type}");
        }
    }

    private void DisableOtherCosmetics(CosmeticType cosmeticType)
    {
        // Find all active cosmetics in the scene
        ChangeCosmetic[] allCosmetics = FindObjectsOfType<ChangeCosmetic>();

        foreach (ChangeCosmetic cosmetic in allCosmetics)
        {
            if (cosmetic != this && cosmetic.type == cosmeticType)
            {
                Debug.Log($"[Codex] Disabling cosmetic: {cosmetic.cosmeticName} of type {cosmeticType}");
                PhotonVRManager.SetCosmetic(cosmeticType.ToString(), ""); // Assuming empty string disables
            }
        }
    }
}