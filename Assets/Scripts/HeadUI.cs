using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeadUI : MonoBehaviour
{
    public Rigidbody rb;
    public Text speed;
    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {
        speed.text = ((Mathf.Round(rb.linearVelocity.magnitude * 10.0f) * 0.1f)).ToString("F1"); 
    }
}
