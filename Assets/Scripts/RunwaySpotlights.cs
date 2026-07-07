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

    [Header("Visible Beam")]
    [SerializeField] private bool showBeams = true;
    [SerializeField, Range(0.01f, 1f)] private float beamOpacity = 0.18f;
    [SerializeField, Range(0.01f, 1f)] private float beamEdgeSoftness = 0.4f;
    [SerializeField, Range(0f, 1f)] private float beamNoiseStrength = 0.12f;
    [SerializeField, Range(8, 48)] private int beamSegments = 24;

    private MusicManager musicManager;
    private Light[] leftLights;
    private Light[] rightLights;
    private MeshRenderer[] leftBeams;
    private MeshRenderer[] rightBeams;
    private Material beamMaterial;
    private Mesh beamMesh;
    private MaterialPropertyBlock beamProperties;
    private int activePair = -1;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int EdgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int NoiseStrengthId = Shader.PropertyToID("_NoiseStrength");

    private void Awake()
    {
        beamProperties = new MaterialPropertyBlock();
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

        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
        }
        if (beamMesh != null)
        {
            Destroy(beamMesh);
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
            UpdateBeam(leftLights[i], leftBeams[i]);
            UpdateBeam(rightLights[i], rightBeams[i]);
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
        leftBeams = new MeshRenderer[pairCount];
        rightBeams = new MeshRenderer[pairCount];

        if (showBeams)
        {
            Shader shader = Shader.Find("Custom/VolumetricSpotlight");
            if (shader != null)
            {
                beamMaterial = new Material(shader) { name = "Runtime Volumetric Spotlight" };
                beamMesh = CreateBeamMesh(beamSegments);
            }
            else
            {
                Debug.LogWarning("Custom/VolumetricSpotlight shader was not found. Beams are disabled.", this);
            }
        }

        for (int i = 0; i < pairCount; i++)
        {
            float z = firstPairZ + pairSpacing * i;
            Color color = i % 2 == 0 ? primaryColor : secondaryColor;
            leftLights[i] = CreateSpotlight($"Spotlight {i + 1:00} L", new Vector3(-sideOffset, height, z), color);
            rightLights[i] = CreateSpotlight($"Spotlight {i + 1:00} R", new Vector3(sideOffset, height, z), color);
            leftBeams[i] = CreateBeam(leftLights[i]);
            rightBeams[i] = CreateBeam(rightLights[i]);
        }
    }

    private MeshRenderer CreateBeam(Light spotlight)
    {
        if (beamMaterial == null || beamMesh == null)
        {
            return null;
        }

        GameObject beamObject = new GameObject("Visible Beam");
        beamObject.transform.SetParent(spotlight.transform, false);
        float radius = Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * range;
        beamObject.transform.localScale = new Vector3(radius, radius, range);

        MeshFilter filter = beamObject.AddComponent<MeshFilter>();
        filter.sharedMesh = beamMesh;
        MeshRenderer renderer = beamObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = beamMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    private void UpdateBeam(Light spotlight, MeshRenderer beam)
    {
        if (beam == null)
        {
            return;
        }

        float normalizedIntensity = peakIntensity > 0f ? spotlight.intensity / peakIntensity : 0f;
        beam.enabled = normalizedIntensity > 0.001f;
        if (!beam.enabled)
        {
            return;
        }

        beam.GetPropertyBlock(beamProperties);
        Color beamColor = spotlight.color;
        beamColor.a = beamOpacity;
        beamProperties.SetColor(ColorId, beamColor);
        beamProperties.SetFloat(IntensityId, normalizedIntensity);
        beamProperties.SetFloat(EdgeSoftnessId, beamEdgeSoftness);
        beamProperties.SetFloat(NoiseStrengthId, beamNoiseStrength);
        beam.SetPropertyBlock(beamProperties);
    }

    private static Mesh CreateBeamMesh(int segments)
    {
        // Unit cone pointing along local +Z. Transform scaling sets its radius and range.
        Vector3[] vertices = new Vector3[segments + 1];
        Vector3[] normals = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        normals[0] = Vector3.back;
        uvs[0] = Vector2.zero;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.PI * 2f * i / segments;
            Vector3 radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            vertices[i + 1] = radial + Vector3.forward;
            normals[i + 1] = new Vector3(radial.x, radial.y, -1f).normalized;
            uvs[i + 1] = new Vector2((float)i / segments, 1f);

            int triangle = i * 3;
            triangles[triangle] = 0;
            triangles[triangle + 1] = i + 1;
            triangles[triangle + 2] = (i + 1) % segments + 1;
        }

        Mesh mesh = new Mesh { name = "Runtime Spotlight Beam" };
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDisable()
    {
        if (leftBeams == null)
        {
            return;
        }

        foreach (MeshRenderer beam in leftBeams)
        {
            if (beam != null) beam.enabled = false;
        }
        foreach (MeshRenderer beam in rightBeams)
        {
            if (beam != null) beam.enabled = false;
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
