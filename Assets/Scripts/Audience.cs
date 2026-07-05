using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Audience : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Vector3 hitOffset;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private float returnDelay = 1f;
    [SerializeField] private InterpolationType interpolationType = InterpolationType.Linear;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private Color waitingColor = Color.black;

    public bool IsTouchingPlaneHitFollower { get; private set; }

    private Vector3 initialPosition;
    private Vector3 interpolationStartPosition;
    private Vector3 targetPosition;
    private float interpolationTime;
    private float timeAfterExit;
    private bool isReturning;
    private Color initialColor;

    private void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (targetRenderer != null)
        {
            initialColor = targetRenderer.color;
        }
    }

    private void Update()
    {
        if (IsTouchingPlaneHitFollower)
        {
            MoveToTarget();
            return;
        }

        timeAfterExit += Time.deltaTime;

        if (timeAfterExit >= returnDelay)
        {
            StartReturningIfNeeded();
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
        timeAfterExit = 0f;
        SetWaitingColor(false);
        BeginInterpolation(initialPosition + hitOffset);
    }

    private void HandleExit(Collider other)
    {
        if (!other.CompareTag("Shoben"))
        {
            return;
        }

        IsTouchingPlaneHitFollower = false;
        isReturning = false;
        timeAfterExit = 0f;
        SetWaitingColor(true);
    }

    private void StartReturningIfNeeded()
    {
        if (isReturning)
        {
            return;
        }

        isReturning = true;
        SetWaitingColor(false);
        BeginInterpolation(initialPosition);
    }

    private void SetWaitingColor(bool isWaiting)
    {
        if (targetRenderer == null)
        {
            return;
        }

        targetRenderer.color = isWaiting ? waitingColor : initialColor;
    }

    private void BeginInterpolation(Vector3 newTargetPosition)
    {
        interpolationStartPosition = transform.position;
        targetPosition = newTargetPosition;
        interpolationTime = 0f;
    }

    private void MoveToTarget()
    {
        if (moveDuration <= 0f)
        {
            transform.position = targetPosition;
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
    }
}
