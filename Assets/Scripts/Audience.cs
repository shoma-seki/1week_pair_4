using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Audience : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3 hitOffset;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float returnDelay = 1f;
    [SerializeField] private InterpolationType interpolationType = InterpolationType.Linear;

    [Header("Dance Noise")]
    [SerializeField] private float danceAmplitude = 0.2f;
    [SerializeField] private float danceFrequency = 8f;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color targetColor = Color.white;
    [SerializeField] private Color waitingColor = Color.black;

    public bool IsTouchingPlaneHitFollower { get; private set; }

    private Vector3 initialPosition;
    private Vector3 interpolationStartPosition;
    private Vector3 targetPosition;
    private float interpolationTime;
    private float timeAfterExit;
    private bool isReturning;
    private bool isWaitingToReturn;
    private float noiseSeed;
    private Color interpolationStartColor;
    private Color targetInterpolationColor;
    private Material targetMaterial;

    private void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            targetMaterial.color = waitingColor;
        }

        interpolationStartColor = waitingColor;
        targetInterpolationColor = waitingColor;
        interpolationTime = moveDuration;
        noiseSeed = Random.value * 1000f;
    }

    private void Update()
    {
        if (IsTouchingPlaneHitFollower && !Input.GetMouseButton(0))
        {
            BeginReturnWait();
        }

        if (IsTouchingPlaneHitFollower)
        {
            MoveToTarget();
            ApplyDanceNoise();
            return;
        }

        if (isWaitingToReturn)
        {
            timeAfterExit += Time.deltaTime;
            MoveToTarget();

            bool hasReachedOffset = moveDuration <= 0f || interpolationTime >= moveDuration;

            if (timeAfterExit >= returnDelay && hasReachedOffset)
            {
                StartReturningIfNeeded();
                return;
            }

            if (timeAfterExit < returnDelay)
            {
                ApplyDanceNoise();
            }

            return;
        }

        if (isReturning)
        {
            MoveToTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleExit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleEnter(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        HandleExit(collision.collider);
    }

    private void HandleEnter(Collider other)
    {
        if (!other.CompareTag("Shoben"))
        {
            return;
        }

        IsTouchingPlaneHitFollower = true;
        isReturning = false;
        isWaitingToReturn = false;
        timeAfterExit = 0f;
        BeginInterpolation(initialPosition + hitOffset, targetColor);
    }

    private void HandleExit(Collider other)
    {
        if (!other.CompareTag("Shoben"))
        {
            return;
        }

        BeginReturnWait();
    }

    private void BeginReturnWait()
    {
        if (!IsTouchingPlaneHitFollower)
        {
            return;
        }

        IsTouchingPlaneHitFollower = false;
        isReturning = false;
        isWaitingToReturn = true;
        timeAfterExit = 0f;
    }

    private void StartReturningIfNeeded()
    {
        if (isReturning)
        {
            return;
        }

        isReturning = true;
        isWaitingToReturn = false;
        BeginInterpolation(initialPosition, waitingColor);
    }

    private void ApplyDanceNoise()
    {
        float noise = Mathf.PerlinNoise(
            noiseSeed,
            Time.time * danceFrequency
        );

        float verticalOffset = (noise * 2f - 1f) * danceAmplitude;
        transform.position += Vector3.up * verticalOffset;
    }

    private void BeginInterpolation(Vector3 newTargetPosition, Color newTargetColor)
    {
        interpolationStartPosition = transform.position;
        targetPosition = newTargetPosition;
        interpolationStartColor = targetMaterial != null
            ? targetMaterial.color
            : waitingColor;
        targetInterpolationColor = newTargetColor;
        interpolationTime = 0f;
    }

    private void MoveToTarget()
    {
        if (moveDuration <= 0f)
        {
            transform.position = targetPosition;

            if (targetMaterial != null)
            {
                targetMaterial.color = targetInterpolationColor;
            }

            return;
        }

        interpolationTime += Time.deltaTime;
        float rate = interpolationTime / moveDuration;

        transform.position = InterpolationUtility.Interpolate(
            interpolationStartPosition,
            targetPosition,
            rate,
            interpolationType
        );

        if (targetMaterial != null)
        {
            targetMaterial.color = InterpolationUtility.Interpolate(
                interpolationStartColor,
                targetInterpolationColor,
                rate,
                interpolationType
            );
        }
    }
}
