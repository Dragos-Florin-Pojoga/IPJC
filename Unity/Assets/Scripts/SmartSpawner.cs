using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartSpawner : MonoBehaviour
{

    [Header("Settings")]
    [Tooltip("The object you want to spawn (Prefab).")]
    public GameObject objectToSpawn;

    [Tooltip("How many copies to spawn.")]
    public int numberOfCopies = 10;

    [Tooltip("Time in seconds between each spawn.")]
    public float spawnCooldown = 0.5f;


    [Header("Location Overrides")]
    [Tooltip("If true, the objects will spawn half their hight higher.")]
    public bool raiseFromTheGround = false;

    [Tooltip("If true, the spawner will move to 'Target Coordinates' before starting.")]
    public bool useSpecificCoordinates = false;

    [Tooltip("The specific 3 coordinates (X, Y, Z) to spawn from if above is checked.")]
    public Vector3 targetCoordinates;


    [Header("Spacing Logic")]
    [Tooltip("How much space to keep between objects.")]
    [Range(0f, 10f)]
    public float minSpacing = 1.5f;


    private int _currentSpawnCount = 0;


    void Start()
    {
        if (useSpecificCoordinates)
        {
            transform.position = targetCoordinates;
        }

        if (raiseFromTheGround && objectToSpawn != null)
        {
            Renderer r = objectToSpawn.GetComponent<Renderer>();

            // if the main object doesn't have a renderer, its children may have it (common for more complex prefabs)
            if (r == null)
            {
                r = objectToSpawn.GetComponentInChildren<Renderer>();
            }

            if (r != null)
            {
                float halfHeight = r.bounds.size.y / 2f;
                transform.position += new Vector3(0, halfHeight, 0);
            }
            else
            {
                Debug.LogWarning("SmartSpawner: Could not find a Renderer on the object to calculate height!");
            }
        }

        if (objectToSpawn != null)
        {
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogError("SmartSpawner: No Object Assigned to 'Object To Spawn'!");
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (_currentSpawnCount < numberOfCopies)
        {
            SpawnNextEntity();
            _currentSpawnCount++;
            
            if (spawnCooldown > 0)
            {
                yield return new WaitForSeconds(spawnCooldown);
            }
            else
            {
                yield return null;
            }
        }
    }

    void SpawnNextEntity()
    {
        Vector3 spawnOffset = CalculateSpiralPoint(_currentSpawnCount);
        Vector3 finalPosition = transform.position + spawnOffset;
        GameObject newObj = Instantiate(objectToSpawn, finalPosition, Quaternion.identity);
        newObj.transform.parent = transform;
    }

    Vector3 CalculateSpiralPoint(int index)
    {
        if (index == 0) return Vector3.zero;

        float goldenAngle = 2.39996f;

        float radius = minSpacing * Mathf.Sqrt(index);

        float angle = index * goldenAngle;

        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        return new Vector3(x, 0, z);
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = useSpecificCoordinates ? targetCoordinates : transform.position;

        int previewCount = numberOfCopies > 0 ? numberOfCopies : 10;

        for (int i = 0; i < previewCount; i++)
        {
            Vector3 point = center + CalculateSpiralPoint(i);
            Gizmos.DrawWireSphere(point, 0.3f);

            if (i < previewCount - 1)
            {
                Vector3 nextPoint = center + CalculateSpiralPoint(i + 1);
                /*Gizmos.DrawLine(point, nextPoint);*/
            }
        }
    }

}
