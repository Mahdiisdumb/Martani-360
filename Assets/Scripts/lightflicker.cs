using UnityEngine;
public class LightFlicker : MonoBehaviour
{
    public Light lightSource;
    public float TimeInterval = 0.1f;
    public float timeoff = 0;
    private float timer;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= TimeInterval)
        {
            lightSource.enabled = !lightSource.enabled;
            timer = -timeoff;
        }
    }
}