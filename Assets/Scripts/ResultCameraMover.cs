using UnityEngine;

/// <summary>Resultカメラを加速させながら+Z方向へ指定距離だけ移動します。</summary>
public class ResultCameraMover : MonoBehaviour
{
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0f)] private float initialSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 4f;
    [SerializeField, Min(0f)] private float moveDistance = 320f;
    [SerializeField] private bool playOnStart = true;
    [Header("Result Input")]
    [SerializeField, Min(0f)] private float skipInputDelay = 0.5f;
    [SerializeField] private string titleSceneName = "Title";

    public float TravelledDistance { get; private set; }
    public bool IsMoving { get; private set; }

    private Vector3 startPosition;
    private float currentSpeed;
    private float delayRemaining;
    private float resultStartedAt;

    private void Awake() => startPosition = transform.position;

    private void Start()
    {
        resultStartedAt = Time.time;
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }

        if (!IsMoving) return;

        if (delayRemaining > 0f)
        {
            delayRemaining -= Time.deltaTime;
            return;
        }

        currentSpeed += acceleration * Time.deltaTime;
        TravelledDistance = Mathf.Min(TravelledDistance + currentSpeed * Time.deltaTime, moveDistance);
        transform.position = startPosition + Vector3.forward * TravelledDistance;
        if (TravelledDistance >= moveDistance) IsMoving = false;
    }

    private void HandleClick()
    {
        if (Time.time - resultStartedAt < skipInputDelay) return;

        if (IsMoving)
        {
            SkipToEnd();
            return;
        }

        if (SceneChange.Instance != null)
        {
            SceneChange.Instance.LoadScene(titleSceneName);
        }
    }

    [ContextMenu("Skip To End")]
    public void SkipToEnd()
    {
        TravelledDistance = moveDistance;
        transform.position = startPosition + Vector3.forward * TravelledDistance;
        delayRemaining = 0f;
        IsMoving = false;

        RunwaySpotlights[] spotlightControllers = FindObjectsByType<RunwaySpotlights>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (RunwaySpotlights controller in spotlightControllers)
        {
            controller.TurnOnAll();
        }
    }

    [ContextMenu("Play")]
    public void Play()
    {
        startPosition = transform.position;
        TravelledDistance = 0f;
        currentSpeed = initialSpeed;
        delayRemaining = startDelay;
        IsMoving = moveDistance > 0f;
    }

    [ContextMenu("Reset Position")]
    public void ResetPosition()
    {
        transform.position = startPosition;
        TravelledDistance = 0f;
        currentSpeed = initialSpeed;
        delayRemaining = startDelay;
        IsMoving = false;
    }
}
