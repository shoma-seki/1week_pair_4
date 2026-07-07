using UnityEngine;

/// <summary>Resultカメラを加速させながら+Z方向へ指定距離だけ移動します。</summary>
public class ResultCameraMover : MonoBehaviour
{
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0f)] private float initialSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 4f;
    [SerializeField, Min(0f)] private float moveDistance = 320f;
    [SerializeField] private bool playOnStart = true;

    public float TravelledDistance { get; private set; }
    public bool IsMoving { get; private set; }

    private Vector3 startPosition;
    private float currentSpeed;
    private float delayRemaining;

    private void Awake() => startPosition = transform.position;

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
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
