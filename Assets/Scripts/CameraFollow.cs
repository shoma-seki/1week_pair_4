using UnityEngine;

[DefaultExecutionOrder(1000)]
[RequireComponent(typeof(Camera))]
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

    [Header("Obstacle Approach Camera Effect")]
    [SerializeField, Range(0f, 1f)] private float obstacleApproachZoomInAmount = 0.35f;
    [SerializeField, Min(0.01f)] private float obstacleApproachZoomInDuration = 0.1f;
    [SerializeField, Min(0f)] private float obstacleApproachHoldDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float obstacleApproachReturnDuration = 0.8f;

    [Header("Urine Stage Camera Shake")]
    [SerializeField, Min(0f)] private float stageChangeShakeAmplitude = 1f;
    [SerializeField, Min(0.01f)] private float stageChangeShakeDuration = 0.25f;
    [SerializeField, Min(0f)] private float thirdStageContinuousShakeAmplitude = 0.35f;
    [SerializeField, Min(0.01f)] private float shakeFrequency = 2f;

    [Header("Obstacle Hit Camera Shake (Adjust Here)")]
    [SerializeField, Min(0f), Tooltip("Strength of the camera shake when an obstacle hits the player.")]
    private float obstacleHitShakeAmplitude = 2f;
    [SerializeField, Min(0.01f), Tooltip("Duration of the obstacle-hit camera shake in seconds.")]
    private float obstacleHitShakeDuration = 0.3f;
    [SerializeField, Min(0.01f), Tooltip("Speed of the obstacle-hit camera shake. Higher values shake faster.")]
    private float obstacleHitShakeFrequency = 20f;

    private Vector3 offset;
    private Player playerComponent;
    private bool isPlayingStopEffect;
    private float effectElapsed;
    private float currentZoomInAmount;
    private float currentZoomInDuration;
    private float currentHoldDuration;
    private float currentReturnDuration;
    private Quaternion baseCameraRotation;
    private Player.UrineStage previousUrineStage;
    private float stageChangeShakeElapsed = float.PositiveInfinity;
    private float obstacleHitShakeElapsed = float.PositiveInfinity;
    private float shakeSeed;

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
            playerComponent.ObstacleApproached += PlayObstacleApproachZoomEffect;
            previousUrineStage = playerComponent.CurrentUrineStage;
        }

        baseCameraRotation = transform.rotation;
        shakeSeed = Random.Range(0f, 1000f);

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
            playerComponent.ObstacleApproached -= PlayObstacleApproachZoomEffect;
        }

    }

    private void PlayObstacleApproachZoomEffect()
    {
        PlayZoomEffect(
            obstacleApproachZoomInAmount,
            obstacleApproachZoomInDuration,
            obstacleApproachHoldDuration,
            obstacleApproachReturnDuration);
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
        UpdateCameraShake();
    }

    public void PlayObstacleHitShake()
    {
        obstacleHitShakeElapsed = 0f;
        shakeSeed = Random.Range(0f, 1000f);
    }

    private void UpdateCameraShake()
    {
        float shakeAmplitude = 0f;

        if (playerComponent != null)
        {
            Player.UrineStage currentStage = playerComponent.CurrentUrineStage;
            if (currentStage > previousUrineStage)
            {
                stageChangeShakeElapsed = 0f;
                shakeSeed = Random.Range(0f, 1000f);
            }
            previousUrineStage = currentStage;

            stageChangeShakeElapsed += Time.deltaTime;
            float transitionProgress = Mathf.Clamp01(stageChangeShakeElapsed / stageChangeShakeDuration);
            float transitionShake = stageChangeShakeAmplitude * (1f - transitionProgress) * (1f - transitionProgress);
            float continuousShake = currentStage == Player.UrineStage.Third
                ? thirdStageContinuousShakeAmplitude
                : 0f;
            shakeAmplitude = transitionShake + continuousShake;
        }

        // The obstacle hit stop can set Time.timeScale to zero.  Keep the impact
        // shake running during that pause so both effects remain visible.
        obstacleHitShakeElapsed += Time.unscaledDeltaTime;
        float obstacleHitProgress = Mathf.Clamp01(obstacleHitShakeElapsed / obstacleHitShakeDuration);
        float obstacleHitShake = obstacleHitShakeAmplitude * (1f - obstacleHitProgress) * (1f - obstacleHitProgress);
        shakeAmplitude += obstacleHitShake;

        transform.rotation = baseCameraRotation;
        if (shakeAmplitude <= 0f)
        {
            return;
        }

        float activeShakeFrequency = obstacleHitShake > 0f
            ? obstacleHitShakeFrequency
            : shakeFrequency;
        float noiseTime = Time.unscaledTime * activeShakeFrequency;
        Vector3 positionNoise = new Vector3(
            SignedPerlin(noiseTime, shakeSeed),
            SignedPerlin(noiseTime * 1.17f, shakeSeed + 17.3f),
            SignedPerlin(noiseTime * 0.83f, shakeSeed + 31.7f));
        positionNoise = Vector3.Scale(positionNoise, new Vector3(0.18f, 0.18f, 0.05f)) * shakeAmplitude;

        Vector3 rotationNoise = new Vector3(
            SignedPerlin(noiseTime * 0.91f, shakeSeed + 47.1f) * 0.8f,
            SignedPerlin(noiseTime * 1.13f, shakeSeed + 63.5f) * 0.8f,
            SignedPerlin(noiseTime * 1.27f, shakeSeed + 79.9f) * 1.2f) * shakeAmplitude;

        transform.position += baseCameraRotation * positionNoise;
        transform.rotation = baseCameraRotation * Quaternion.Euler(rotationNoise);
    }

    private static float SignedPerlin(float x, float y)
    {
        return Mathf.PerlinNoise(x, y) * 2f - 1f;
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
