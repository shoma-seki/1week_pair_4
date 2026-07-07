using UnityEngine.Animations;
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
    [Tooltip("forwardOffset に加える、前後方向のランダムなずれの範囲")]
    [SerializeField] private Vector2 forwardRandomRange = new Vector2(-2f, 2f);
    [Tooltip("x = 0 を挟んだ、左右の最も内側にいる Audience 同士の距離")]
    [SerializeField, Min(0f)] private float centerSpace = 2f;
    [Tooltip("同じ側に並ぶ Audience 同士の距離")]
    [SerializeField, Min(0f)] private float audienceSpacing = 2f;
    [SerializeField] private float spawnHeight;

    private float nextSpawnDistance;
    private Transform mainCameraTransform;

    private void Start()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCameraTransform = mainCamera.transform;
        }
        else
        {
            Debug.LogWarning("AudienceSpawn: MainCamera が見つかりません。", this);
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
            bool isRightSide = i % 2 == 0;
            int indexOnSide = i / 2;
            float distanceFromCenter = centerSpace * 0.5f + audienceSpacing * indexOnSide;
            float xPosition = isRightSide ? distanceFromCenter : -distanceFromCenter;

            Vector3 position = new Vector3(
                xPosition,
                player.transform.position.y + spawnHeight,
                player.transform.position.z + forwardOffset +
                    Random.Range(forwardRandomRange.x, forwardRandomRange.y)
            );

            Audience audience = Instantiate(
                audiencePrefab,
                position,
                audiencePrefab.transform.rotation,
                audienceParent
            );

            SetLookAtTarget(audience);
        }
    }

    private void SetLookAtTarget(Audience audience)
    {
        if (mainCameraTransform == null ||
            !audience.TryGetComponent(out LookAtConstraint lookAtConstraint))
        {
            return;
        }

        lookAtConstraint.SetSources(new System.Collections.Generic.List<ConstraintSource>
        {
            new ConstraintSource
            {
                sourceTransform = mainCameraTransform,
                weight = 1f
            }
        });
        lookAtConstraint.constraintActive = true;
    }

    private void OnValidate()
    {
        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        spawnCount = Mathf.Max(1, spawnCount);
        centerSpace = Mathf.Max(0f, centerSpace);
        audienceSpacing = Mathf.Max(0f, audienceSpacing);

        if (forwardRandomRange.x > forwardRandomRange.y)
        {
            forwardRandomRange = new Vector2(forwardRandomRange.y, forwardRandomRange.x);
        }
    }
}
