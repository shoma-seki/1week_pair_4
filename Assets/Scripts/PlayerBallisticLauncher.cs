using System.Collections;
using UnityEngine;

/// <summary>
/// マウスで指定した地点へ、指定した高さの放物線で発射します。
/// 発射時には LineRenderer で液体状の軌跡も表示します。
/// </summary>
public class PlayerBallisticLauncher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraRaycaster cameraRaycaster;
    [SerializeField] private Transform launchPoint;

    [Header("Trajectory")]
    [Tooltip("発射点と着地点のうち、高い方から測った放物線の高さ")]
    [SerializeField, Min(0.01f)] private float arcHeight = 3f;
    [SerializeField] private Vector3 launchOffset;

    [Header("Urine Stream")]
    [Tooltip("未設定の場合は実行時に自動生成します。")]
    [SerializeField] private LineRenderer streamRenderer;
    [SerializeField, Min(2)] private int streamSegments = 32;
    [SerializeField, Min(0.01f)] private float streamDuration = 0.45f;
    [SerializeField, Min(0f)] private float streamWidth = 0.09f;
    [SerializeField, Min(0f)] private float wobbleAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float wobbleFrequency = 5f;
    [SerializeField, Min(0f)] private float wobbleSpeed = 7f;
    [SerializeField] private Color streamColor = new Color(1f, 0.82f, 0.12f, 0.9f);

    [Header("Input")]
    [SerializeField] private bool launchOnMouseButtonUp = true;
    [SerializeField, Min(0)] private int mouseButton;

    private Coroutine streamCoroutine;
    private Material generatedStreamMaterial;

    private void Awake()
    {
        if (cameraRaycaster == null)
        {
            cameraRaycaster = FindAnyObjectByType<CameraRaycaster>();
        }

        PrepareStreamRenderer();
    }

    private void Update()
    {
        if (launchOnMouseButtonUp && Input.GetMouseButtonUp(mouseButton))
        {
            LaunchAtPlaneHit();
        }
    }

    private void OnDisable()
    {
        if (streamRenderer != null)
        {
            streamRenderer.enabled = false;
        }
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
        if (cameraRaycaster == null ||
            !cameraRaycaster.TryGetPlaneHitPoint(out Vector3 targetPosition))
        {
            return false;
        }

        return Launch(targetPosition);
    }

    public bool Launch(Vector3 targetPosition)
    {
        if (Physics.gravity.y >= 0f)
        {
            Debug.LogWarning("Physics.gravity.y は負の値にしてください。", this);
            return false;
        }

        Vector3 startPosition = GetLaunchPosition();
        Vector3 launchVelocity = CalculateLaunchVelocity(
            startPosition,
            targetPosition,
            arcHeight,
            Physics.gravity.y);

        float flightTime = CalculateFlightTime(startPosition, targetPosition, arcHeight, Physics.gravity.y);
        PlayStream(startPosition, launchVelocity, flightTime);
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

    private void PlayStream(Vector3 startPosition, Vector3 launchVelocity, float flightTime)
    {
        if (streamRenderer == null)
        {
            return;
        }

        if (streamCoroutine != null)
        {
            StopCoroutine(streamCoroutine);
        }

        streamCoroutine = StartCoroutine(StreamRoutine(startPosition, launchVelocity, flightTime));
    }

    private IEnumerator StreamRoutine(Vector3 startPosition, Vector3 launchVelocity, float flightTime)
    {
        streamRenderer.enabled = true;
        streamRenderer.positionCount = Mathf.Max(2, streamSegments);
        streamRenderer.widthMultiplier = streamWidth;
        streamRenderer.startColor = streamColor;
        streamRenderer.endColor = new Color(streamColor.r, streamColor.g, streamColor.b, 0.35f);

        float elapsed = 0f;
        while (elapsed < streamDuration)
        {
            DrawStream(startPosition, launchVelocity, flightTime, elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        streamRenderer.enabled = false;
        streamCoroutine = null;
    }

    private void DrawStream(
        Vector3 startPosition,
        Vector3 launchVelocity,
        float flightTime,
        float animationTime)
    {
        int count = streamRenderer.positionCount;
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
            position += sideways * noise * 2f * wobbleAmplitude * endpointMask;
            streamRenderer.SetPosition(i, position);
        }
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
