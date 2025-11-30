using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class lavaspawn : MonoBehaviour
{
    public GameObject lavaPrefab;
    public List<GameObject> spawnedLavas = new List<GameObject>();
    public int lavasPerSecond = 10;
    public float despawnTime = 10f;

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            for (int i = 0; i < lavasPerSecond; i++)
            {
                SpawnLava();
            }

            yield return new WaitForSeconds(1f); // Wait one second before the next wave
        }
    }

    public void SpawnLava()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 5f;
        GameObject lava = Instantiate(lavaPrefab, spawnPosition, Quaternion.identity);
        spawnedLavas.Add(lava);
        StartCoroutine(DespawnAfterTime(lava, despawnTime));
    }

    IEnumerator DespawnAfterTime(GameObject lava, float time)
    {
        yield return new WaitForSeconds(time);
        if (lava != null)
        {
            spawnedLavas.Remove(lava);
            Destroy(lava);
        }
    }
}