using UnityEngine;

public class RunwaySpotlights : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(1)] private int pairCount = 8;
    [SerializeField, Min(0f)] private float sideOffset = 10f;
    [SerializeField, Min(0f)] private float height = 14f;
    [SerializeField] private float firstPairZ = 35f;
    [SerializeField, Min(0.1f)] private float pairSpacing = 35f;

    [Header("Light")]
    [SerializeField] private Color primaryColor = new Color(1f, 0.18f, 0.55f);
    [SerializeField] private Color secondaryColor = new Color(0.15f, 0.65f, 1f);
    [SerializeField, Min(0f)] private float peakIntensity = 5000f;
    [SerializeField, Min(0.1f)] private float range = 30f;
    [SerializeField, Range(1f, 179f)] private float spotAngle = 42f;
    [SerializeField, Range(0.05f, 1f)] private float fadeDurationInBeats = 0.8f;

    private MusicManager musicManager;
    private Light[] leftLights;
    private Light[] rightLights;
    private int activePair = -1;

    private void Awake()
    {
        CreateLights();
    }

    private void Start()
    {
        musicManager = FindAnyObjectByType<MusicManager>();
        if (musicManager == null)
        {
            Debug.LogWarning("MusicManagerが見つからないため、スポットライトを同期できません。", this);
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
        if (musicManager == null || activePair < 0)
        {
            return;
        }

        float fadeDuration = Mathf.Max(0.01f, (float)musicManager.SecondsPerBeat * fadeDurationInBeats);
        float intensityStep = peakIntensity / fadeDuration * Time.deltaTime;

        for (int i = 0; i < pairCount; i++)
        {
            float target = i == activePair ? peakIntensity : 0f;
            FadeLight(leftLights[i], target, intensityStep);
            FadeLight(rightLights[i], target, intensityStep);
        }
    }

    private void OnBeat(int beatIndex)
    {
        activePair = beatIndex % pairCount;
        leftLights[activePair].enabled = true;
        rightLights[activePair].enabled = true;
    }

    private void CreateLights()
    {
        leftLights = new Light[pairCount];
        rightLights = new Light[pairCount];

        for (int i = 0; i < pairCount; i++)
        {
            float z = firstPairZ + pairSpacing * i;
            Color color = i % 2 == 0 ? primaryColor : secondaryColor;
            leftLights[i] = CreateSpotlight($"Spotlight {i + 1:00} L", new Vector3(-sideOffset, height, z), color);
            rightLights[i] = CreateSpotlight($"Spotlight {i + 1:00} R", new Vector3(sideOffset, height, z), color);
        }
    }

    private Light CreateSpotlight(string lightName, Vector3 localPosition, Color color)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.LookAt(transform.TransformPoint(new Vector3(0f, 0f, localPosition.z)));

        Light spotlight = lightObject.AddComponent<Light>();
        spotlight.type = LightType.Spot;
        spotlight.color = color;
        spotlight.intensity = 0f;
        spotlight.range = range;
        spotlight.spotAngle = spotAngle;
        spotlight.innerSpotAngle = spotAngle * 0.65f;
        spotlight.shadows = LightShadows.None;
        spotlight.enabled = false;
        return spotlight;
    }

    private static void FadeLight(Light spotlight, float target, float step)
    {
        spotlight.intensity = Mathf.MoveTowards(spotlight.intensity, target, step);
        if (target <= 0f && spotlight.intensity <= 0f)
        {
            spotlight.enabled = false;
        }
    }
}
