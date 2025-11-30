using UnityEngine;
using Photon.Pun;
using UnityEngine.XR;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(PhotonView))]
public class GunScript : MonoBehaviourPun
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireForce = 1000f;
    public float cooldown = 0.5f;

    private bool canFire = true;
    private InputDevice rightHand;

    void Start()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0) rightHand = devices[0];
    }

    void Update()
    {
        if (!photonView.IsMine || !canFire || !rightHand.isValid) return;

        bool triggerPressed;
        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
        {
            photonView.RPC("FireBullet", RpcTarget.All, firePoint.position, firePoint.forward);
            StartCoroutine(FireCooldown());
        }
    }

    [PunRPC]
    void FireBullet(Vector3 position, Vector3 direction)
    {
        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        rb?.AddForce(direction * fireForce);
    }

    private IEnumerator FireCooldown()
    {
        canFire = false;
        yield return new WaitForSeconds(cooldown);
        canFire = true;
    }
}