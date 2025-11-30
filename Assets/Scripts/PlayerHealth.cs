using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private float health = 100f;

    [SerializeField]
    private float regenRate = 5f; // Health per second
    [SerializeField]
    private float regenDelay = 5f; // Seconds after last damage before regen starts

    private float timeSinceLastDamage = 0f;
    private bool tookDamage = false;

    private void Update()
    {
        // Update the timer
        if (tookDamage)
        {
            timeSinceLastDamage += Time.deltaTime;

            // If delay passed, begin regenerating
            if (timeSinceLastDamage >= regenDelay && health < 100f)
            {
                RegenerateHealth();
            }
        }
    }

    private void isPlayerAlive()
    {
        if (health <= 0)
        {
            Debug.Log("Player is dead");
            SceneManager.LoadScene(1);
        }
    }

    public float checkPlayerHealth()
    {
        return health;
    }

    public void lowerPlayerHealth(float amount)
    {
        health = Mathf.Max(health - amount, 0f);
        isPlayerAlive();

        // Reset regen timer
        tookDamage = true;
        timeSinceLastDamage = 0f;
    }

    private void RegenerateHealth()
    {
        health += regenRate * Time.deltaTime;
        health = Mathf.Clamp(health, 0f, 100f);
    }
}