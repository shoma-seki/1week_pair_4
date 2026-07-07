using UnityEngine;

public class AudienceSpawn : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Player player;
    [SerializeField] private Audience audiencePrefab;
    [SerializeField] private Transform audienceParent;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.01f)] private float spawnInterval = 10f;
    [SerializeField, Min(1)] private int spawnCount = 1;
    [SerializeField] private float forwardOffset = 15f;
    [SerializeField] private Vector2 horizontalRange = new Vector2(-5f, 5f);
    [SerializeField] private float spawnHeight;

    private float nextSpawnDistance;

    private void Start()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        nextSpawnDistance = spawnInterval;

        if (player != null)
        {
            player.DistanceChanged += HandleDistanceChanged;
            HandleDistanceChanged(player.DistanceTraveled);
        }
        else
        {
            Debug.LogWarning("AudienceSpawn: Playerが見つかりません。", this);
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.DistanceChanged -= HandleDistanceChanged;
        }
    }

    private void HandleDistanceChanged(float distance)
    {
        while (distance >= nextSpawnDistance)
        {
            SpawnAudience();
            nextSpawnDistance += spawnInterval;
        }
    }

    private void SpawnAudience()
    {
        if (audiencePrefab == null)
        {
            Debug.LogWarning("AudienceSpawn: Audience Prefabが設定されていません。", this);
            return;
        }

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 position = player.transform.position + new Vector3(
                Random.Range(horizontalRange.x, horizontalRange.y),
                spawnHeight,
                forwardOffset
            );

            Instantiate(audiencePrefab, position, audiencePrefab.transform.rotation, audienceParent);
        }
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        spawnCount = Mathf.Max(1, spawnCount);

        if (horizontalRange.x > horizontalRange.y)
        {
            horizontalRange = new Vector2(horizontalRange.y, horizontalRange.x);
        }
    }
}
