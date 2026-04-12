using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelGridSpawner : MonoBehaviour
{
    [Header("Prefabs / Models")]
    public List<GameObject> prefabs = new List<GameObject>();
    public List<GameObject> models = new List<GameObject>();

    [Header("Grid Settings")]
    public int columns = 10;
    public float spacingX = 2f;
    public float spacingZ = 2f;

    [Header("Batch Settings")]
    public int batchSize = 20;
    public float batchDelay = 10f;

    private List<GameObject> all = new List<GameObject>();
    private List<GameObject> currentBatch = new List<GameObject>();

    private int index = 0;

    void Start()
    {
        all.AddRange(prefabs);
        all.AddRange(models);

        StartCoroutine(BatchLoop());
    }

    IEnumerator BatchLoop()
    {
        if (all.Count == 0)
            yield break;

        while (true)
        {
            LoadBatch();
            yield return new WaitForSeconds(batchDelay);
            UnloadBatch();
        }
    }

    void LoadBatch()
    {
        currentBatch.Clear();

        int total = all.Count;

        for (int i = 0; i < batchSize; i++)
        {
            if (total == 0) return;

            GameObject prefab = all[index];

            int row = i / columns;
            int col = i % columns;

            Vector3 pos = new Vector3(
                col * spacingX,
                0,
                row * spacingZ
            );

            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

            StripEverythingExceptRender(obj);

            currentBatch.Add(obj);

            index++;
            if (index >= total)
                index = 0;
        }

        Debug.Log($"Loaded batch of {currentBatch.Count}");
    }

    void UnloadBatch()
    {
        for (int i = 0; i < currentBatch.Count; i++)
        {
            if (currentBatch[i] != null)
                Destroy(currentBatch[i]);
        }

        currentBatch.Clear();

        Debug.Log("Unloaded batch");
    }

    void StripEverythingExceptRender(GameObject obj)
    {
        if (obj == null) return;

        foreach (var s in obj.GetComponentsInChildren<MonoBehaviour>(true))
            if (s != null) s.enabled = false;

        foreach (var rb in obj.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) rb.isKinematic = true;

        foreach (var c in obj.GetComponentsInChildren<Collider>(true))
            if (c != null) c.enabled = false;

        foreach (var a in obj.GetComponentsInChildren<Animator>(true))
            if (a != null) a.enabled = false;
    }
}