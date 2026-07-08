using UnityEngine;

/// <summary>
/// マウスで指定した地点へ、指定した高さの放物線で発射します。
/// 発射時には LineRenderer で液体状の軌跡も表示します。
/// </summary>
public class PlayerBallisticLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraRaycaster cameraRaycaster;
    [SerializeField] private PlaneHitFollower planeHitFollower;
    [SerializeField] private Player player;
    [SerializeField] private Transform launchPoint;

    [Header("Trajectory")]
    [Tooltip("発射点と着地点のうち、高い方から測った放物線の高さ")]
    [SerializeField, Min(0.01f)] private float arcHeight = 3f;
    [SerializeField] private Vector3 launchOffset;

    [Header("Urine Stream")]
    [Tooltip("未設定の場合は実行時に自動生成します。")]
    [SerializeField] private LineRenderer streamRenderer;
    [SerializeField, Min(2)] private int streamSegments = 32;
    [SerializeField, Min(0f)] private float streamWidth = 0.09f;
    [SerializeField, Min(0f)] private float wobbleAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float wobbleFrequency = 5f;
    [SerializeField, Min(0f)] private float wobbleSpeed = 7f;
    [SerializeField, Range(0f, 1f)] private float secondStageWobbleMultiplier = 0.6f;
    [SerializeField, Range(0f, 1f)] private float thirdStageWobbleMultiplier = 0.3f;
    [SerializeField, Min(0.01f)] private float disappearDuration = 0.35f;
    [SerializeField] private Color streamColor = new Color(1f, 0.82f, 0.12f, 0.9f);

    [Tooltip("三段階目の間だけ再生するプレハブ。LineRenderer があれば放物線として更新します。")]

    [Header("Input")]
    [SerializeField, Min(0)] private int mouseButton;

    private Material generatedStreamMaterial;
    private float disappearProgress = 1f;
    private bool wasFiring;
    private ParticleSystem[] childParticleSystems;

    private void Awake()
    {
        if (cameraRaycaster == null)
        {
            cameraRaycaster = FindAnyObjectByType<CameraRaycaster>();
        }

        if (planeHitFollower == null)
        {
            planeHitFollower = FindAnyObjectByType<PlaneHitFollower>();
        }

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        PrepareStreamRenderer();
        CacheChildParticleSystems();
    }

    private void Update()
    {
        if (Input.GetMouseButton(mouseButton) && player != null && player.CanUrinate)
        {
            DrawHeldStream();
        }
        else
        {
            StopStream();
        }
    }

    private void OnDisable()
    {
        if (streamRenderer != null)
        {
            streamRenderer.enabled = false;
        }

        SetChildParticlesPlaying(false);
    }

    private void OnDestroy()
    {
        if (generatedStreamMaterial != null)
        {
            Destroy(generatedStreamMaterial);
        }
    }

    public bool LaunchAtPlaneHit()
    {
        if (player == null || !player.CanUrinate ||
            cameraRaycaster == null ||
            !cameraRaycaster.TryGetPlaneHitPoint(out Vector3 targetPosition))
        {
            return false;
        }

        return Launch(targetPosition);
    }

    public bool Launch(Vector3 targetPosition)
    {
        if (player == null || !player.CanUrinate)
        {
            HideRenderers();
            return false;
        }

        if (Physics.gravity.y >= 0f)
        {
            Debug.LogWarning("Physics.gravity.y は負の値にしてください。", this);
            return false;
        }

        targetPosition = ConstrainTargetPosition(targetPosition);
        Vector3 startPosition = GetLaunchPosition();
        float stageArcHeight = GetCurrentArcHeight();
        Vector3 launchVelocity = CalculateLaunchVelocity(
            startPosition,
            targetPosition,
            stageArcHeight,
            Physics.gravity.y);

        float flightTime = CalculateFlightTime(startPosition, targetPosition, stageArcHeight, Physics.gravity.y);
        DrawStream(streamRenderer, startPosition, launchVelocity, flightTime, Time.time, GetStageMultiplier());
        return true;
    }

    private Vector3 GetLaunchPosition()
    {
        return (launchPoint != null ? launchPoint.position : transform.position) + launchOffset;
    }

    private void PrepareStreamRenderer()
    {
        if (streamRenderer == null)
        {
            GameObject streamObject = new GameObject("Urine Stream");
            streamObject.transform.SetParent(transform, false);
            streamRenderer = streamObject.AddComponent<LineRenderer>();
        }

        streamRenderer.useWorldSpace = true;
        streamRenderer.textureMode = LineTextureMode.Stretch;
        streamRenderer.alignment = LineAlignment.View;
        streamRenderer.numCapVertices = 4;
        streamRenderer.numCornerVertices = 2;
        streamRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.8f, 0.8f),
            new Keyframe(1f, 0.2f));

        if (streamRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                generatedStreamMaterial = new Material(shader)
                {
                    name = "Generated Urine Stream Material"
                };
                streamRenderer.sharedMaterial = generatedStreamMaterial;
            }
        }

        streamRenderer.enabled = false;
    }

    private void DrawHeldStream()
    {
        if (cameraRaycaster == null ||
            !cameraRaycaster.TryGetPlaneHitPoint(out Vector3 targetPosition))
        {
            HideRenderers();
            return;
        }

        targetPosition = ConstrainTargetPosition(targetPosition);

        float multiplier = GetStageMultiplier();
        if (streamRenderer == null)
        {
            return;
        }

        wasFiring = true;
        disappearProgress = 0f;

        bool playThirdStageParticles = player.CurrentUrineStage == Player.UrineStage.Third;
        if (playThirdStageParticles)
        {
            AimChildParticleSystemsAt(targetPosition);
        }
        SetChildParticlesPlaying(playThirdStageParticles);

        Vector3 startPosition = GetLaunchPosition();
        float stageArcHeight = GetCurrentArcHeight();
        Vector3 launchVelocity = CalculateLaunchVelocity(startPosition, targetPosition, stageArcHeight, Physics.gravity.y);
        float flightTime = CalculateFlightTime(startPosition, targetPosition, stageArcHeight, Physics.gravity.y);
        DrawStream(streamRenderer, startPosition, launchVelocity, flightTime, Time.time, multiplier);
    }

    private Vector3 ConstrainTargetPosition(Vector3 targetPosition)
    {
        return planeHitFollower != null
            ? planeHitFollower.ConstrainTargetPosition(targetPosition)
            : targetPosition;
    }

    private void DrawStream(
        LineRenderer renderer,
        Vector3 startPosition,
        Vector3 launchVelocity,
        float flightTime,
        float animationTime,
        float strengthMultiplier)
    {
        renderer.enabled = true;
        renderer.positionCount = Mathf.Max(2, streamSegments);
        renderer.widthMultiplier = streamWidth * strengthMultiplier;
        renderer.startColor = streamColor;
        renderer.endColor = new Color(streamColor.r, streamColor.g, streamColor.b, 0.35f);
        renderer.widthCurve = CreateVisibleWidthCurve(0f);

        int count = renderer.positionCount;
        Vector3 horizontalDirection = new Vector3(launchVelocity.x, 0f, launchVelocity.z).normalized;
        Vector3 sideways = Vector3.Cross(Vector3.up, horizontalDirection);
        if (sideways.sqrMagnitude < 0.001f)
        {
            sideways = transform.right;
        }

        for (int i = 0; i < count; i++)
        {
            float normalizedTime = i / (float)(count - 1);
            float t = flightTime * normalizedTime;
            Vector3 position = startPosition
                + launchVelocity * t
                + 0.5f * Physics.gravity * t * t;

            // 両端は固定し、中央だけを滑らかに揺らします。
            float endpointMask = Mathf.Sin(normalizedTime * Mathf.PI);
            float noise = Mathf.PerlinNoise(
                normalizedTime * wobbleFrequency,
                animationTime * wobbleSpeed) - 0.5f;
            position += sideways * noise * 2f * wobbleAmplitude * GetStageWobbleMultiplier() * endpointMask;
            renderer.SetPosition(i, position);
        }
    }

    private float GetStageWobbleMultiplier()
    {
        if (player == null) return 1f;

        return player.CurrentUrineStage switch
        {
            Player.UrineStage.Third => thirdStageWobbleMultiplier,
            Player.UrineStage.Second => secondStageWobbleMultiplier,
            _ => 1f
        };
    }

    private float GetStageMultiplier()
    {
        return player != null ? player.GetChargeStrengthMultiplier() : 1f;
    }

    private float GetCurrentArcHeight()
    {
        return arcHeight * (player != null ? player.CurrentArcHeightMultiplier : 1f);
    }

    private void CacheChildParticleSystems()
    {
        childParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particles in childParticleSystems)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void SetChildParticlesPlaying(bool playing)
    {
        if (childParticleSystems == null) return;

        foreach (ParticleSystem particles in childParticleSystems)
        {
            if (particles == null) continue;

            if (playing)
            {
                if (!particles.isPlaying) particles.Play(true);
            }
            else if (particles.isPlaying)
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void AimChildParticleSystemsAt(Vector3 targetPosition)
    {
        if (childParticleSystems == null) return;

        foreach (ParticleSystem particles in childParticleSystems)
        {
            if (particles == null) continue;

            Vector3 direction = targetPosition - particles.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                particles.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }
        }
    }

    private void HideRenderers()
    {
        if (streamRenderer != null) streamRenderer.enabled = false;
        SetChildParticlesPlaying(false);
    }

    private void StopStream()
    {
        SetChildParticlesPlaying(false);

        if (!wasFiring || streamRenderer == null || !streamRenderer.enabled)
        {
            return;
        }

        disappearProgress += Time.deltaTime / Mathf.Max(0.01f, disappearDuration);
        if (disappearProgress >= 1f)
        {
            wasFiring = false;
            streamRenderer.enabled = false;
            return;
        }

        streamRenderer.widthCurve = CreateVisibleWidthCurve(disappearProgress);
    }

    private static AnimationCurve CreateVisibleWidthCurve(float hiddenFromStart)
    {
        float edge = Mathf.Clamp01(hiddenFromStart);
        if (edge <= 0f)
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.8f, 0.8f),
                new Keyframe(1f, 0.2f));
        }

        float featherEnd = Mathf.Min(1f, edge + 0.08f);
        return new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(edge, 0f),
            new Keyframe(featherEnd, Mathf.Lerp(1f, 0.2f, featherEnd)),
            new Keyframe(1f, 0.2f));
    }

    public static Vector3 CalculateLaunchVelocity(
        Vector3 startPosition,
        Vector3 targetPosition,
        float height,
        float gravityY)
    {
        float totalTime = CalculateFlightTime(startPosition, targetPosition, height, gravityY);
        float peakY = Mathf.Max(startPosition.y, targetPosition.y) + Mathf.Max(0.01f, height);
        float upwardSpeed = Mathf.Sqrt(2f * -gravityY * (peakY - startPosition.y));

        Vector3 horizontalDisplacement = targetPosition - startPosition;
        horizontalDisplacement.y = 0f;

        Vector3 velocity = horizontalDisplacement / totalTime;
        velocity.y = upwardSpeed;
        return velocity;
    }

    private static float CalculateFlightTime(
        Vector3 startPosition,
        Vector3 targetPosition,
        float height,
        float gravityY)
    {
        float peakY = Mathf.Max(startPosition.y, targetPosition.y) + Mathf.Max(0.01f, height);
        float upwardSpeed = Mathf.Sqrt(2f * -gravityY * (peakY - startPosition.y));
        float timeToPeak = upwardSpeed / -gravityY;
        float timeFromPeak = Mathf.Sqrt(2f * (peakY - targetPosition.y) / -gravityY);
        return timeToPeak + timeFromPeak;
    }
}
