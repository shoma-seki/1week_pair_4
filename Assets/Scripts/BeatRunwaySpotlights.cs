using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// MusicManagerの拍に合わせて、ランウェイ左右のスポットライトと光線メッシュを順番に光らせる制御。
/// シーンに同じPrefabが複数あっても、最初の1つだけが自動生成・制御します。
/// </summary>
public class BeatRunwaySpotlights : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField, Min(1)] private int pairCount = 8;
    [SerializeField] private float centerX;
    [SerializeField] private float sideOffset = 10f;
    [SerializeField] private float height = 14f;
    [SerializeField] private float firstPairZ;
    [SerializeField] private float pairSpacing = 25f;
    [SerializeField] private float aimY;

    [Header("Light")]
    [SerializeField] private Color primaryColor = new Color(1f, 0.35f, 0.75f);
    [SerializeField] private Color secondaryColor = new Color(0.25f, 0.75f, 1f);
    [SerializeField, Min(0f)] private float peakIntensity = 5000f;
    [SerializeField, Min(0f)] private float idleIntensity;
    [SerializeField, Min(0.1f)] private float range = 100f;
    [SerializeField, Range(1f, 179f)] private float spotAngle = 130f;
    [SerializeField, Range(0f, 1f)] private float innerSpotAngleRatio = 0.6f;

    [Header("Visible Beam")]
    [SerializeField] private bool createVisibleBeams = true;
    [SerializeField, Range(0f, 1f)] private float beamPeakAlpha = 0.22f;
    [SerializeField, Range(0f, 1f)] private float beamIdleAlpha;
    [SerializeField, Min(0.1f)] private float beamLength = 22f;
    [SerializeField, Range(1f, 179f)] private float beamAngle = 36f;
    [SerializeField, Min(3)] private int beamSegments = 24;
    [SerializeField] private bool beamDoubleSided = true;

    [Header("Beat")]
    [SerializeField, Range(0.05f, 2f)] private float fadeDurationInBeats = 0.65f;
    [SerializeField, Range(0f, 1f)] private float neighborIntensityRate = 0.25f;
    [SerializeField] private bool useFallbackBeatWhenNoMusic = true;
    [SerializeField, Min(0.05f)] private float fallbackSecondsPerBeat = 0.5f;

    [Header("Safety")]
    [SerializeField] private bool useSingleController = true;

    private const string GeneratedPrefix = "Generated Beat Spotlight";
    private static BeatRunwaySpotlights activeController;

    private readonly List<Light> leftLights = new List<Light>();
    private readonly List<Light> rightLights = new List<Light>();
    private readonly List<MeshRenderer> leftBeams = new List<MeshRenderer>();
    private readonly List<MeshRenderer> rightBeams = new List<MeshRenderer>();

    private MusicManager musicManager;
    private int activePairIndex = -1;
    private float fallbackTimer;
    private Mesh beamMesh;

    private void Awake()
    {
        if (useSingleController && activeController != null && activeController != this)
        {
            enabled = false;
            return;
        }

        activeController = this;
        BuildLights();
        ApplyInstantIntensity(idleIntensity);
    }

    private void Start()
    {
        musicManager = FindAnyObjectByType<MusicManager>();
        if (musicManager != null)
        {
            musicManager.Beat += OnBeat;
        }
    }

    private void Update()
    {
        if (musicManager == null && useFallbackBeatWhenNoMusic)
        {
            fallbackTimer += Time.deltaTime;
            if (fallbackTimer >= fallbackSecondsPerBeat)
            {
                fallbackTimer -= fallbackSecondsPerBeat;
                OnBeat(activePairIndex + 1);
            }
        }

        float secondsPerBeat = musicManager != null && musicManager.SecondsPerBeat > 0d
            ? (float)musicManager.SecondsPerBeat
            : fallbackSecondsPerBeat;
        float fadeDuration = Mathf.Max(0.01f, secondsPerBeat * fadeDurationInBeats);
        float maxDelta = peakIntensity / fadeDuration * Time.deltaTime;

        for (int i = 0; i < leftLights.Count; i++)
        {
            float targetIntensity = GetTargetIntensity(i);
            FadeLight(leftLights[i], targetIntensity, maxDelta);
            FadeLight(rightLights[i], targetIntensity, maxDelta);
            UpdateBeamAlpha(leftLights[i], leftBeams[i]);
            UpdateBeamAlpha(rightLights[i], rightBeams[i]);
        }
    }

    private void OnDestroy()
    {
        if (musicManager != null)
        {
            musicManager.Beat -= OnBeat;
        }

        if (activeController == this)
        {
            activeController = null;
        }
    }

    private void OnBeat(int beatIndex)
    {
        if (pairCount <= 0)
        {
            return;
        }

        activePairIndex = Mathf.Abs(beatIndex) % pairCount;
    }

    [ContextMenu("Rebuild Runtime Lights")]
    private void BuildLights()
    {
        ClearGeneratedLights();
        leftLights.Clear();
        rightLights.Clear();
        leftBeams.Clear();
        rightBeams.Clear();

        if (createVisibleBeams)
        {
            beamMesh = CreateBeamMesh();
        }

        for (int i = 0; i < pairCount; i++)
        {
            float z = firstPairZ + pairSpacing * i;
            Color color = i % 2 == 0 ? primaryColor : secondaryColor;

            leftLights.Add(CreateSpotlight($"{GeneratedPrefix} L {i + 1}", centerX - sideOffset, z, color, leftBeams));
            rightLights.Add(CreateSpotlight($"{GeneratedPrefix} R {i + 1}", centerX + sideOffset, z, color, rightBeams));
        }
    }

    private Light CreateSpotlight(string objectName, float x, float z, Color color, List<MeshRenderer> beamRenderers)
    {
        GameObject lightObject = new GameObject(objectName);
        lightObject.transform.SetParent(transform, true);
        lightObject.transform.position = new Vector3(x, height, z);

        Vector3 target = new Vector3(centerX, aimY, z);
        Vector3 direction = target - lightObject.transform.position;
        if (direction.sqrMagnitude > 0.001f)
        {
            lightObject.transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        Light spotlight = lightObject.AddComponent<Light>();
        spotlight.type = LightType.Spot;
        spotlight.color = color;
        spotlight.intensity = idleIntensity;
        spotlight.range = range;
        spotlight.spotAngle = spotAngle;
        spotlight.innerSpotAngle = spotAngle * innerSpotAngleRatio;
        spotlight.shadows = LightShadows.None;

        beamRenderers.Add(createVisibleBeams ? CreateBeam(lightObject.transform, color) : null);
        return spotlight;
    }

    private MeshRenderer CreateBeam(Transform lightTransform, Color color)
    {
        GameObject beamObject = new GameObject("Visible Beam");
        beamObject.transform.SetParent(lightTransform, false);
        beamObject.transform.localPosition = Vector3.zero;
        beamObject.transform.localRotation = Quaternion.identity;
        beamObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter = beamObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = beamMesh;

        MeshRenderer meshRenderer = beamObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateBeamMaterial(color);
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        meshRenderer.allowOcclusionWhenDynamic = false;

        SetBeamAlpha(meshRenderer, beamIdleAlpha);
        return meshRenderer;
    }

    private Mesh CreateBeamMesh()
    {
        int sideVertexCount = beamSegments + 1;
        int tipIndex = 0;
        int centerIndex = sideVertexCount + 1;
        float radius = Mathf.Tan(beamAngle * 0.5f * Mathf.Deg2Rad) * beamLength;

        Vector3[] vertices = new Vector3[sideVertexCount + 2];
        vertices[tipIndex] = Vector3.zero;

        for (int i = 0; i < sideVertexCount; i++)
        {
            float t = i / (float)beamSegments;
            float angle = t * Mathf.PI * 2f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, beamLength);
        }

        vertices[centerIndex] = Vector3.forward * beamLength;

        List<int> triangles = new List<int>(beamSegments * 6);
        for (int i = 0; i < beamSegments; i++)
        {
            int current = i + 1;
            int next = i + 2;

            triangles.Add(tipIndex);
            triangles.Add(next);
            triangles.Add(current);

            triangles.Add(centerIndex);
            triangles.Add(current);
            triangles.Add(next);
        }

        Mesh mesh = new Mesh
        {
            name = "Generated Spotlight Beam"
        };
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private Material CreateBeamMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader)
        {
            name = "Generated Spotlight Beam Material",
            renderQueue = (int)RenderQueue.Transparent
        };

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.One);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 0f);
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", beamDoubleSided ? (float)CullMode.Off : (float)CullMode.Back);
        }

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHABLEND_ON");
        SetMaterialColor(material, color, beamIdleAlpha);
        return material;
    }

    private void ClearGeneratedLights()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(GeneratedPrefix))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private float GetTargetIntensity(int pairIndex)
    {
        if (activePairIndex < 0)
        {
            return idleIntensity;
        }

        if (pairIndex == activePairIndex)
        {
            return peakIntensity;
        }

        int previousPair = activePairIndex <= 0 ? pairCount - 1 : activePairIndex - 1;
        int nextPair = activePairIndex >= pairCount - 1 ? 0 : activePairIndex + 1;
        if (pairIndex == previousPair || pairIndex == nextPair)
        {
            return Mathf.Lerp(idleIntensity, peakIntensity, neighborIntensityRate);
        }

        return idleIntensity;
    }

    private static void FadeLight(Light target, float targetIntensity, float maxDelta)
    {
        if (target == null)
        {
            return;
        }

        target.intensity = Mathf.MoveTowards(target.intensity, targetIntensity, maxDelta);
    }

    private void UpdateBeamAlpha(Light lightSource, MeshRenderer beamRenderer)
    {
        if (lightSource == null || beamRenderer == null)
        {
            return;
        }

        float normalizedIntensity = peakIntensity > 0f
            ? Mathf.Clamp01(lightSource.intensity / peakIntensity)
            : 0f;
        float alpha = Mathf.Lerp(beamIdleAlpha, beamPeakAlpha, normalizedIntensity);
        SetBeamAlpha(beamRenderer, alpha);
    }

    private static void SetBeamAlpha(MeshRenderer beamRenderer, float alpha)
    {
        if (beamRenderer == null || beamRenderer.sharedMaterial == null)
        {
            return;
        }

        Color color = beamRenderer.sharedMaterial.color;
        SetMaterialColor(beamRenderer.sharedMaterial, color, alpha);
        beamRenderer.enabled = alpha > 0.001f;
    }

    private static void SetMaterialColor(Material material, Color color, float alpha)
    {
        color.a = alpha;
        material.color = color;

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void ApplyInstantIntensity(float intensity)
    {
        foreach (Light target in leftLights)
        {
            if (target != null)
            {
                target.intensity = intensity;
            }
        }

        foreach (Light target in rightLights)
        {
            if (target != null)
            {
                target.intensity = intensity;
            }
        }

        for (int i = 0; i < leftLights.Count; i++)
        {
            UpdateBeamAlpha(leftLights[i], leftBeams[i]);
            UpdateBeamAlpha(rightLights[i], rightBeams[i]);
        }
    }

    private void OnValidate()
    {
        pairCount = Mathf.Max(1, pairCount);
        peakIntensity = Mathf.Max(0f, peakIntensity);
        idleIntensity = Mathf.Max(0f, idleIntensity);
        range = Mathf.Max(0.1f, range);
        beamLength = Mathf.Max(0.1f, beamLength);
        beamSegments = Mathf.Max(3, beamSegments);
        fallbackSecondsPerBeat = Mathf.Max(0.05f, fallbackSecondsPerBeat);
    }
}
