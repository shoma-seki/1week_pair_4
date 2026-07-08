using System.Collections;
using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private float detectionDistance = 8f;

    [Header("Descent")]
    [SerializeField] private float descentDistance = 2f;
    [SerializeField] private float descentSpeed = 1.5f;

    [Header("Shake")]
    [SerializeField] private float shakeTime = 2f;
    [SerializeField] private float shakeAmount = 0.1f;
    [SerializeField] private float shakeSpeed = 20f;

    [Header("Fall")]
    [SerializeField] private float initialFallSpeed = 1f;
    [SerializeField] private float fallAcceleration = 9.8f;
    [SerializeField] private float groundY = 0f;

    [Header("Fire")]
    [SerializeField] private FireCOntorol firePrefab;

    private Player player;
    private bool sequenceStarted;
    private bool isFalling;
    private float currentFallSpeed;
    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        SetVisible(false);
    }

    private void Start()
    {
        player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (!sequenceStarted)
        {
            TryStartSequence();
        }

        if (!isFalling)
        {
            return;
        }

        currentFallSpeed += fallAcceleration * Time.deltaTime;
        transform.position += Vector3.down * currentFallSpeed * Time.deltaTime;

        if (transform.position.y <= groundY)
        {
            SpawnFire();
        }
    }

    private void TryStartSequence()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<Player>();
            if (player == null)
            {
                return;
            }
        }

        // The player advances along the Z axis. The light is intentionally placed
        // far to the side on X, so a planar distance check can never become small.
        float distanceAlongRunway = Mathf.Abs(transform.position.z - player.transform.position.z);

        if (distanceAlongRunway <= detectionDistance)
        {
            sequenceStarted = true;
            SetVisible(true);
            StartCoroutine(DescendShakeAndFall());
        }
    }

    private void SetVisible(bool isVisible)
    {
        foreach (Renderer targetRenderer in renderers)
        {
            targetRenderer.enabled = isVisible;
        }
    }

    private IEnumerator DescendShakeAndFall()
    {
        Vector3 descentStart = transform.position;
        Vector3 descentEnd = descentStart + Vector3.down * descentDistance;

        while (Vector3.Distance(transform.position, descentEnd) > 0.001f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                descentEnd,
                descentSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = descentEnd;
        yield return Shake();
        currentFallSpeed = initialFallSpeed;
        isFalling = true;
    }

    private IEnumerator Shake()
    {
        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;
            float xOffset = Mathf.Sin(elapsed * shakeSpeed) * shakeAmount;
            transform.position = startPosition + Vector3.right * xOffset;
            yield return null;
        }

        transform.position = startPosition;
    }

    private void SpawnFire()
    {
        Vector3 spawnPosition = transform.position + Vector3.up * 4.5f;
        Quaternion rotation = Quaternion.Euler(90f, 0f, 180f);
        Instantiate(firePrefab, spawnPosition, rotation);
        Destroy(gameObject);
    }
}
