using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Beat Movement")]
    [SerializeField, Min(0f)] private float distancePerBeat = 2.5f;
    [SerializeField, Range(0.05f, 1f)] private float moveDurationInBeats = 0.5f;
    [SerializeField] private bool requireMouseButton = true;

    private MusicManager musicManager;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private float moveElapsed;
    private float moveDuration;
    private bool isMoving;

    private void Start()
    {
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
        if (!isMoving)
        {
            return;
        }

        moveElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(moveElapsed / moveDuration);
        transform.position = Vector3.Lerp(moveStart, moveTarget, Mathf.SmoothStep(0f, 1f, progress));

        if (progress >= 1f)
        {
            isMoving = false;
        }
    }

    private void OnBeat(int beatIndex)
    {
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
