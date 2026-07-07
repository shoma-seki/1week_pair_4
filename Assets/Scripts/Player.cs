using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Urine Resource")]
    [SerializeField, Min(0.01f)] private float maxUrine = 100f;
    [SerializeField, Min(0f)] private float urineConsumptionPerSecond = 20f;

    [Header("Beat Movement")]
    [SerializeField, Min(0f)] private float distancePerBeat = 2.5f;
    [SerializeField, Range(0.05f, 1f)] private float moveDurationInBeats = 0.5f;
    [SerializeField] private bool requireMouseButton = true;

    [Header("Puni Puni Motion")]
    [SerializeField, Range(0f, 0.5f)] private float stretchAmount = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float squashAmount = 0.18f;

    private MusicManager musicManager;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private Vector3 originalScale;
    private float moveElapsed;
    private float moveDuration;
    private bool isMoving;
    private float currentUrine;
    private Vector3 startPosition;
    private float distanceTraveled;
    private bool canMove = true;

    public float CurrentUrine => currentUrine;
    public float MaxUrine => maxUrine;
    public float UrineNormalized => maxUrine > 0f ? currentUrine / maxUrine : 0f;
    public float DistanceTraveled => distanceTraveled;
    public bool CanMove => canMove;

    public event Action<float, float> UrineChanged;
    public event Action<float> DistanceChanged;

    private void Awake()
    {
        currentUrine = maxUrine;
    }

    private void Start()
    {
        startPosition = transform.position;
        originalScale = transform.localScale;
        musicManager = FindAnyObjectByType<MusicManager>();

        if (musicManager == null)
        {
            Debug.LogWarning("MusicManagerが見つからないため、プレイヤーは拍に合わせて移動できません。", this);
            return;
        }

        musicManager.Beat += OnBeat;
    }

    private void OnDestroy()
    {
        if (musicManager != null)
        {
            musicManager.Beat -= OnBeat;
        }
    }

    private void Update()
    {
        UpdateDistanceTraveled();
        UpdateUrineResource();

        if (!isMoving)
        {
            return;
        }

        moveElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(moveElapsed / moveDuration);
        float jump = Mathf.Sin(progress * Mathf.PI);
        float landing = Mathf.Sin(Mathf.InverseLerp(0.7f, 1f, progress) * Mathf.PI);

        transform.position = Vector3.Lerp(moveStart, moveTarget, Mathf.SmoothStep(0f, 1f, progress));

        float verticalScale = 1f + jump * stretchAmount - landing * squashAmount;
        float horizontalScale = 1f - jump * stretchAmount * 0.35f + landing * squashAmount * 0.5f;
        transform.localScale = Vector3.Scale(originalScale, new Vector3(horizontalScale, verticalScale, horizontalScale));

        if (progress >= 1f)
        {
            isMoving = false;
            transform.position = moveTarget;
            transform.localScale = originalScale;
        }
    }

    public void StopMovement()
    {
        canMove = false;
        isMoving = false;
        transform.localScale = originalScale;
    }

    private void UpdateDistanceTraveled()
    {
        float nextDistance = Mathf.Max(0f, transform.position.z - startPosition.z);

        if (Mathf.Approximately(nextDistance, distanceTraveled))
        {
            return;
        }

        distanceTraveled = nextDistance;
        DistanceChanged?.Invoke(distanceTraveled);
    }

    private void UpdateUrineResource()
    {
        float nextUrine = Input.GetMouseButton(0)
            ? Mathf.Max(0f, currentUrine - urineConsumptionPerSecond * Time.deltaTime)
            : maxUrine;

        if (Mathf.Approximately(nextUrine, currentUrine))
        {
            return;
        }

        currentUrine = nextUrine;
        UrineChanged?.Invoke(currentUrine, maxUrine);
    }

    private void OnValidate()
    {
        maxUrine = Mathf.Max(0.01f, maxUrine);
        urineConsumptionPerSecond = Mathf.Max(0f, urineConsumptionPerSecond);

        if (Application.isPlaying && currentUrine > maxUrine)
        {
            currentUrine = maxUrine;
            UrineChanged?.Invoke(currentUrine, maxUrine);
        }
    }

    private void OnBeat(int beatIndex)
    {
        if (!canMove)
        {
            return;
        }

        if (requireMouseButton && !Input.GetMouseButton(0))
        {
            return;
        }

        moveStart = transform.position;
        moveTarget = moveStart + Vector3.forward * distancePerBeat;
        moveElapsed = 0f;
        moveDuration = Mathf.Max(0.01f, (float)musicManager.SecondsPerBeat * moveDurationInBeats);
        isMoving = true;
    }
}
