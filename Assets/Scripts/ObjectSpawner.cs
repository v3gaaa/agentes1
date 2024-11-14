using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject barrelPrefab;
    public GameObject crateShortPrefab;
    public GameObject crateLongPrefab;
    public Vector3 spawnAreaMin;
    public Vector3 spawnAreaMax;

    // Start is called before the first frame update
    void Start()
    {
        SpawnObjects(barrelPrefab, 3);
        SpawnObjects(crateShortPrefab, 3);
        SpawnObjects(crateLongPrefab, 3);
    }

    void SpawnObjects(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                spawnAreaMin.y, // Keep Y constant
                Random.Range(spawnAreaMin.z, spawnAreaMax.z)
            );
            Instantiate(prefab, randomPosition, Quaternion.identity);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
