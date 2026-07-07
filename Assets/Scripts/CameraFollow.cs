using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Stop Camera Effect")]
    [SerializeField, Range(0f, 1f)] private float zoomInAmount = 0.2f;
    [SerializeField, Min(0.01f)] private float zoomInDuration = 0.15f;
    [SerializeField, Min(0.01f)] private float returnDuration = 1.2f;

    private Vector3 offset;
    private Player playerController;
    private bool waitingForStop;
    private bool isPlayingStopEffect;
    private float effectElapsed;

    private void Start()
    {
        if (player == null)
        {
            Player targetPlayer = FindAnyObjectByType<Player>();

            if (targetPlayer != null)
            {
                player = targetPlayer.transform;
            }
        }

        if (player != null)
        {
            offset = transform.position - player.position;
            playerController = player.GetComponent<Player>();
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            waitingForStop = true;
        }

        if (waitingForStop && (playerController == null || !playerController.IsMoving))
        {
            waitingForStop = false;
            isPlayingStopEffect = true;
            effectElapsed = 0f;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        float zoom = GetStopEffectZoom();
        transform.position = player.position + offset * (1f - zoom);
    }

    private float GetStopEffectZoom()
    {
        if (!isPlayingStopEffect)
        {
            return 0f;
        }

        effectElapsed += Time.deltaTime;

        if (effectElapsed < zoomInDuration)
        {
            float progress = effectElapsed / zoomInDuration;
            return Mathf.SmoothStep(0f, zoomInAmount, progress);
        }

        float returnProgress = (effectElapsed - zoomInDuration) / returnDuration;
        if (returnProgress >= 1f)
        {
            isPlayingStopEffect = false;
            return 0f;
        }

        return Mathf.SmoothStep(zoomInAmount, 0f, returnProgress);
    }
}
