using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform[] spawnPoints; // 存储复活点的数组

    public Transform GetNearestSpawnPoint(Vector3 playerPosition)
    {
        Transform nearestSpawnPoint = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Transform spawnPoint in spawnPoints)
        {
            float distance = Vector3.Distance(playerPosition, spawnPoint.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSpawnPoint = spawnPoint;
            }
        }

        return nearestSpawnPoint;
    }
}
