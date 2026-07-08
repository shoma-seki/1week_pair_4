using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    [Header("Stop Camera Effect")]
    [SerializeField, Range(0f, 1f)] private float zoomInAmount = 0.2f;
    [SerializeField, Min(0.01f)] private float zoomInDuration = 0.15f;
    [SerializeField] private InterpolationType zoomInInterpolationType = InterpolationType.SmoothStep;
    [SerializeField, Min(0f)] private float holdDuration = 0.12f;
    [SerializeField, Min(0.01f)] private float returnDuration = 1.2f;
    [SerializeField] private InterpolationType returnInterpolationType = InterpolationType.SmoothStep;

    [Header("Check Collider Camera Effect")]
    [SerializeField, Range(0f, 1f)] private float checkColliderZoomInAmount = 0.35f;
    [SerializeField, Min(0.01f)] private float checkColliderZoomInDuration = 0.1f;
    [SerializeField, Min(0f)] private float checkColliderHoldDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float checkColliderReturnDuration = 0.8f;

    private Vector3 offset;
    private Player playerComponent;
    private bool isPlayingStopEffect;
    private float effectElapsed;
    private float currentZoomInAmount;
    private float currentZoomInDuration;
    private float currentHoldDuration;
    private float currentReturnDuration;

    private void Start()
    {
        if (player == null)
        {
            Player targetPlayer = FindAnyObjectByType<Player>();

            if (targetPlayer != null)
            {
                playerComponent = targetPlayer;
                player = targetPlayer.transform;
            }
        }

        if (playerComponent == null && player != null)
        {
            playerComponent = player.GetComponentInParent<Player>();
        }

        if (playerComponent != null)
        {
            playerComponent.CheckColliderEntered += PlayCheckColliderZoomEffect;
        }

        if (player != null)
        {
            offset = transform.position - player.position;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            PlayZoomEffect(zoomInAmount, zoomInDuration, holdDuration, returnDuration);
        }
    }

    private void OnDestroy()
    {
        if (playerComponent != null)
        {
            playerComponent.CheckColliderEntered -= PlayCheckColliderZoomEffect;
        }
    }

    private void PlayCheckColliderZoomEffect()
    {
        PlayZoomEffect(
            checkColliderZoomInAmount,
            checkColliderZoomInDuration,
            checkColliderHoldDuration,
            checkColliderReturnDuration);
    }

    private void PlayZoomEffect(float amount, float zoomDuration, float effectHoldDuration, float effectReturnDuration)
    {
        currentZoomInAmount = amount;
        currentZoomInDuration = zoomDuration;
        currentHoldDuration = effectHoldDuration;
        currentReturnDuration = effectReturnDuration;
        isPlayingStopEffect = true;
        effectElapsed = 0f;
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

        if (effectElapsed < currentZoomInDuration)
        {
            float progress = effectElapsed / currentZoomInDuration;
            return InterpolationUtility.Interpolate(
                0f,
                currentZoomInAmount,
                progress,
                zoomInInterpolationType
            );
        }

        float holdElapsed = effectElapsed - currentZoomInDuration;
        if (holdElapsed < currentHoldDuration)
        {
            return currentZoomInAmount;
        }

        float returnProgress = (holdElapsed - currentHoldDuration) / currentReturnDuration;
        if (returnProgress >= 1f)
        {
            isPlayingStopEffect = false;
            return 0f;
        }

        return InterpolationUtility.Interpolate(
            currentZoomInAmount,
            0f,
            returnProgress,
            returnInterpolationType
        );
    }
}
