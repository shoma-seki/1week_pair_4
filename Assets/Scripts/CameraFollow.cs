using UnityEngine;
using Unity.Cinemachine;

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

    [Header("Check Collider Camera Effect")]
    [SerializeField, Range(0f, 1f)] private float checkColliderZoomInAmount = 0.35f;
    [SerializeField, Min(0.01f)] private float checkColliderZoomInDuration = 0.1f;
    [SerializeField, Min(0f)] private float checkColliderHoldDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float checkColliderReturnDuration = 0.8f;

    [Header("Urine Stage Camera Shake")]
    [SerializeField, Min(0f)] private float stageChangeShakeAmplitude = 1f;
    [SerializeField, Min(0.01f)] private float stageChangeShakeDuration = 0.25f;
    [SerializeField, Min(0f)] private float thirdStageContinuousShakeAmplitude = 0.35f;
    [SerializeField, Min(0.01f)] private float shakeFrequency = 2f;

    private Vector3 offset;
    private Player playerComponent;
    private bool isPlayingStopEffect;
    private float effectElapsed;
    private float currentZoomInAmount;
    private float currentZoomInDuration;
    private float currentHoldDuration;
    private float currentReturnDuration;
    private CinemachineBrain cinemachineBrain;
    private CinemachineCamera shakeCamera;
    private CinemachineBasicMultiChannelPerlin shakeNoise;
    private NoiseSettings runtimeNoiseProfile;
    private GameObject shakeCameraObject;
    private Quaternion baseCameraRotation;
    private Player.UrineStage previousUrineStage;
    private float stageChangeShakeElapsed = float.PositiveInfinity;
    private bool addedCinemachineBrain;

    private void Awake()
    {
        SetupCinemachineShake();
    }

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
            previousUrineStage = playerComponent.CurrentUrineStage;
        }

        baseCameraRotation = transform.rotation;

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

        if (shakeCameraObject != null) Destroy(shakeCameraObject);
        if (runtimeNoiseProfile != null) Destroy(runtimeNoiseProfile);
        if (addedCinemachineBrain && cinemachineBrain != null) Destroy(cinemachineBrain);
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
        UpdateCinemachineShake();
    }

    private void SetupCinemachineShake()
    {
        cinemachineBrain = GetComponent<CinemachineBrain>();
        if (cinemachineBrain == null)
        {
            cinemachineBrain = gameObject.AddComponent<CinemachineBrain>();
            addedCinemachineBrain = true;
        }
        cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;

        shakeCameraObject = new GameObject("Urine Stage Cinemachine Camera");
        shakeCamera = shakeCameraObject.AddComponent<CinemachineCamera>();
        shakeNoise = shakeCameraObject.AddComponent<CinemachineBasicMultiChannelPerlin>();
        shakeNoise.NoiseProfile = CreateRuntimeNoiseProfile();
        shakeNoise.AmplitudeGain = 0f;
        shakeNoise.FrequencyGain = shakeFrequency;

        Camera outputCamera = GetComponent<Camera>();
        shakeCamera.Lens = LensSettings.FromCamera(outputCamera);
        shakeCameraObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
    }

    private NoiseSettings CreateRuntimeNoiseProfile()
    {
        runtimeNoiseProfile = ScriptableObject.CreateInstance<NoiseSettings>();
        runtimeNoiseProfile.name = "Runtime Urine Stage Camera Shake";
        runtimeNoiseProfile.PositionNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 1f, Amplitude = 0.18f },
                Y = new NoiseSettings.NoiseParams { Frequency = 1.17f, Amplitude = 0.18f },
                Z = new NoiseSettings.NoiseParams { Frequency = 0.83f, Amplitude = 0.05f }
            }
        };
        runtimeNoiseProfile.OrientationNoise = new[]
        {
            new NoiseSettings.TransformNoiseParams
            {
                X = new NoiseSettings.NoiseParams { Frequency = 0.91f, Amplitude = 0.8f },
                Y = new NoiseSettings.NoiseParams { Frequency = 1.13f, Amplitude = 0.8f },
                Z = new NoiseSettings.NoiseParams { Frequency = 1.27f, Amplitude = 1.2f }
            }
        };
        return runtimeNoiseProfile;
    }

    private void UpdateCinemachineShake()
    {
        if (cinemachineBrain == null || shakeCamera == null || shakeNoise == null) return;

        if (playerComponent != null)
        {
            Player.UrineStage currentStage = playerComponent.CurrentUrineStage;
            if (currentStage > previousUrineStage)
            {
                stageChangeShakeElapsed = 0f;
                shakeNoise.ReSeed();
            }
            previousUrineStage = currentStage;

            stageChangeShakeElapsed += Time.deltaTime;
            float transitionProgress = Mathf.Clamp01(stageChangeShakeElapsed / stageChangeShakeDuration);
            float transitionShake = stageChangeShakeAmplitude * (1f - transitionProgress) * (1f - transitionProgress);
            float continuousShake = currentStage == Player.UrineStage.Third
                ? thirdStageContinuousShakeAmplitude
                : 0f;
            shakeNoise.AmplitudeGain = transitionShake + continuousShake;
            shakeNoise.FrequencyGain = shakeFrequency;
        }

        transform.rotation = baseCameraRotation;
        shakeCameraObject.transform.SetPositionAndRotation(transform.position, baseCameraRotation);
        cinemachineBrain.ManualUpdate();
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
