using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField, Min(1)] private int fanDecreaseAmount = 1;
    [SerializeField, Min(0f)] private float activationZDistance = 9f;
    [SerializeField, Min(0f)] private float directionLockDistance = 5f;
    [SerializeField] private GameObject child;

    [Header("Pre Effect")]
    [SerializeField] private GameObject preEffectPrefab;
    [SerializeField] private Vector3 preEffectOffset = Vector3.zero;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationSpeed = Vector3.zero;

    private MeshRenderer meshRenderer;
    private Player player;
    private Vector3 moveDirection;
    private bool isMoving;
    private bool isDirectionLocked;
    private bool hasHitPlayer;

    private void Start()
    {
        meshRenderer = TryGetComponent(out MeshRenderer renderer)
            ? renderer
            : child.GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        player = FindAnyObjectByType<Player>();
        moveDirection = transform.position.x > 0f
            ? Vector3.left
            : transform.position.x < 0f
                ? Vector3.right
                : Vector3.zero;
    }

    private void Update()
    {
        if (!isMoving && IsPlayerWithinActivationDistance())
        {
            StartMove();
        }

        if (!isMoving)
        {
            return;
        }

        moveDirection = GetDirectionToPlayer();
        transform.position += moveDirection * (moveSpeed * Time.deltaTime);

        child.transform.Rotate(rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetDirectionToPlayer()
    {
        if (player == null || isDirectionLocked)
        {
            return moveDirection;
        }

        Vector3 directionToPlayer = player.transform.position - transform.position;
        Vector3 nextDirection = directionToPlayer.sqrMagnitude > Mathf.Epsilon
            ? directionToPlayer.normalized
            : moveDirection;

        if (directionToPlayer.sqrMagnitude <= directionLockDistance * directionLockDistance)
        {
            isDirectionLocked = true;
        }

        return nextDirection;
    }

    private void StartMove()
    {
        if (isMoving)
        {
            return;
        }

        player.NotifyObstacleApproached();
        PlayPreEffect();
        meshRenderer.enabled = true;
        isMoving = true;
        Destroy(gameObject, lifeTime);
    }

    private void PlayPreEffect()
    {
        if (preEffectPrefab == null)
        {
            Debug.LogWarning($"PreEffect prefab is not assigned on '{name}'.", this);
            return;
        }

        Instantiate(
            preEffectPrefab,
            player.transform.position + preEffectOffset,
            preEffectPrefab.transform.rotation);
    }

    private bool IsPlayerWithinActivationDistance()
    {
        if (player == null)
        {
            return false;
        }

        float zDistance = transform.position.z - player.transform.position.z;
        return zDistance >= 0f && zDistance <= activationZDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitPlayer(other);
    }

    private void TryHitPlayer(Collider other)
    {
        Player hitPlayer = other.GetComponentInParent<Player>();
        if (hitPlayer == null || !Input.GetMouseButton(0) || hasHitPlayer)
        {
            return;
        }

        hasHitPlayer = true;
        FanManager.Instance.RemoveFan(fanDecreaseAmount);
        hitPlayer.NotifyObstacleHit();
        Destroy(gameObject);
    }

    private void OnValidate()
    {
        fanDecreaseAmount = Mathf.Max(1, fanDecreaseAmount);
        activationZDistance = Mathf.Max(0f, activationZDistance);
        directionLockDistance = Mathf.Max(0f, directionLockDistance);
    }
}
