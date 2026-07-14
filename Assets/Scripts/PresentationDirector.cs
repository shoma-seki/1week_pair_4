using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Provides lightweight, asset-independent presentation effects for every scene.
/// The object is created automatically after a scene is loaded.
/// </summary>
[DefaultExecutionOrder(-500)]
public sealed class PresentationDirector : MonoBehaviour
{
    public static PresentationDirector Instance { get; private set; }

    private Canvas canvas;
    private Image flashImage;
    private TextMeshProUGUI bannerText;
    private TextMeshProUGUI promptText;
    private AudioSource audioSource;
    private AudioSource musicSource;
    private AudioClip runtimeMusicClip;
    private Coroutine bannerRoutine;
    private Coroutine flashRoutine;
    private string sceneName;
    private float titlePulseTime;
    private RectTransform[] titleGraphics;
    private Vector3[] titleGraphicScales;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CreateForScene(scene);

    private static void CreateForScene(Scene scene)
    {
        if (!scene.isLoaded || FindAnyObjectByType<PresentationDirector>() != null)
        {
            return;
        }

        GameObject host = new GameObject("Presentation Director");
        SceneManager.MoveGameObjectToScene(host, scene);
        host.AddComponent<PresentationDirector>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        sceneName = SceneManager.GetActiveScene().name;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        BuildOverlay();
    }

    private void Start()
    {
        if (sceneName == "Title")
        {
            SetupTitlePresentation();
        }
        else if (sceneName == "Game")
        {
            StartCoroutine(PlayGameOpening());
        }
        else if (sceneName == "Result")
        {
            SetupResultPresentation();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        if (runtimeMusicClip != null) Destroy(runtimeMusicClip);
    }

    private void Update()
    {
        if (sceneName == "Title")
        {
            AnimateTitleGraphics();
        }

        if (sceneName == "Result" && promptText != null)
        {
            ResultCameraMover mover = FindAnyObjectByType<ResultCameraMover>();
            promptText.text = mover != null && mover.IsMoving ? "CLICK TO SKIP" : "CLICK TO TITLE";
            PulsePrompt();
        }
    }

    private void BuildOverlay()
    {
        GameObject canvasObject = new GameObject("Presentation Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        flashImage = CreateImage("Flash", canvas.transform, Color.clear);
        StretchFullScreen(flashImage.rectTransform);
        flashImage.raycastTarget = false;

        bannerText = CreateText("Banner", canvas.transform, 124f, TextAlignmentOptions.Center);
        bannerText.rectTransform.anchorMin = new Vector2(0.1f, 0.3f);
        bannerText.rectTransform.anchorMax = new Vector2(0.9f, 0.7f);
        bannerText.rectTransform.offsetMin = Vector2.zero;
        bannerText.rectTransform.offsetMax = Vector2.zero;
        bannerText.enabled = false;

        promptText = CreateText("Prompt", canvas.transform, 34f, TextAlignmentOptions.Center);
        promptText.rectTransform.anchorMin = new Vector2(0.2f, 0.04f);
        promptText.rectTransform.anchorMax = new Vector2(0.8f, 0.12f);
        promptText.rectTransform.offsetMin = Vector2.zero;
        promptText.rectTransform.offsetMax = Vector2.zero;
        promptText.enabled = false;
    }

    private void SetupTitlePresentation()
    {
        promptText.enabled = true;
        promptText.text = "HOLD LEFT CLICK TO RUN  /  AIM AT THE DOOR";
        promptText.color = new Color(1f, 0.92f, 0.45f, 0.95f);

        Graphic[] graphics = FindObjectsByType<Graphic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        int count = 0;
        foreach (Graphic graphic in graphics)
        {
            if (graphic.canvas != canvas && graphic.GetComponentInParent<SceneChange>() == null && graphic.rectTransform.rect.width > 250f)
            {
                count++;
            }
        }

        titleGraphics = new RectTransform[count];
        titleGraphicScales = new Vector3[count];
        int index = 0;
        foreach (Graphic graphic in graphics)
        {
            if (graphic.canvas == canvas || graphic.GetComponentInParent<SceneChange>() != null || graphic.rectTransform.rect.width <= 250f)
            {
                continue;
            }

            titleGraphics[index] = graphic.rectTransform;
            titleGraphicScales[index] = graphic.rectTransform.localScale;
            index++;
        }

        ShowBanner("SHOW TIME", new Color(1f, 0.45f, 0.85f), 1.2f);
        PlayTone(523f, 0.12f, 0.16f);
    }

    private void SetupResultPresentation()
    {
        promptText.enabled = true;
        promptText.color = new Color(1f, 0.88f, 0.35f, 0.95f);
        StartResultMusic();
        ShowBanner("RESULT", new Color(1f, 0.78f, 0.22f), 1.5f);
        StartCoroutine(PlayResultFanfare());
    }

    private IEnumerator PlayGameOpening()
    {
        Player player = null;
        while (player == null)
        {
            player = FindAnyObjectByType<Player>();
            yield return null;
        }

        player.SetMovementEnabled(false);
        yield return new WaitForSeconds(0.25f);

        string[] cues = { "3", "2", "1" };
        foreach (string cue in cues)
        {
            ShowBanner(cue, Color.white, 0.62f);
            PlayTone(440f + (3 - int.Parse(cue)) * 70f, 0.1f, 0.16f);
            yield return new WaitForSeconds(0.68f);
        }

        ShowBanner("GO!", new Color(1f, 0.82f, 0.2f), 0.75f);
        Flash(new Color(1f, 0.72f, 0.2f, 0.32f), 0.35f);
        PlayTone(880f, 0.2f, 0.22f);
        player.SetMovementEnabled(true);
    }

    private IEnumerator PlayResultFanfare()
    {
        yield return new WaitForSeconds(0.1f);
        PlayCrowdCheer();
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        foreach (float note in notes)
        {
            PlayTone(note, 0.22f, 0.17f);
            yield return new WaitForSeconds(0.16f);
        }
        Flash(new Color(1f, 0.75f, 0.2f, 0.25f), 0.6f);
    }

    private void StartResultMusic()
    {
        const int sampleRate = 22050;
        const float duration = 8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float[] chord = { 261.63f, 329.63f, 392f, 523.25f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            int beat = Mathf.FloorToInt(t * 2f) % chord.Length;
            float phase = t * chord[beat] * Mathf.PI * 2f;
            float beatEnvelope = Mathf.Lerp(0.28f, 0.08f, (t * 2f) % 1f);
            samples[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 0.5f) * 0.45f) * beatEnvelope * 0.12f;
        }

        runtimeMusicClip = AudioClip.Create("Runtime Result Music", sampleCount, 1, sampleRate, false);
        runtimeMusicClip.SetData(samples, 0);
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.volume = 0.35f;
        musicSource.clip = runtimeMusicClip;
        musicSource.Play();
    }

    private void PlayCrowdCheer()
    {
        const int sampleRate = 22050;
        const float duration = 1.8f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(20260710);
        float filteredNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float rate = i / (float)sampleCount;
            float noise = (float)(random.NextDouble() * 2d - 1d);
            filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.08f);
            float envelope = Mathf.Sin(rate * Mathf.PI) * (1f - rate * 0.35f);
            float voices = Mathf.Sin(i / (float)sampleRate * 310f * Mathf.PI * 2f) * 0.18f;
            samples[i] = (filteredNoise + voices) * envelope * 0.16f;
        }

        AudioClip cheer = AudioClip.Create("Runtime Crowd Cheer", sampleCount, 1, sampleRate, false);
        cheer.SetData(samples, 0);
        audioSource.PlayOneShot(cheer);
        Destroy(cheer, duration + 0.2f);
    }

    private void AnimateTitleGraphics()
    {
        titlePulseTime += Time.deltaTime;
        if (titleGraphics != null)
        {
            for (int i = 0; i < titleGraphics.Length; i++)
            {
                if (titleGraphics[i] == null) continue;
                float pulse = 1f + Mathf.Sin(titlePulseTime * 2.3f + i * 0.7f) * 0.018f;
                titleGraphics[i].localScale = titleGraphicScales[i] * pulse;
            }
        }
        PulsePrompt();
    }

    private void PulsePrompt()
    {
        if (promptText == null || !promptText.enabled) return;
        Color color = promptText.color;
        color.a = Mathf.Lerp(0.48f, 1f, (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f);
        promptText.color = color;
    }

    public void ShowBanner(string message, Color color, float duration)
    {
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(message, color, duration));
    }

    private IEnumerator BannerRoutine(string message, Color color, float duration)
    {
        bannerText.text = message;
        bannerText.color = new Color(color.r, color.g, color.b, 0f);
        bannerText.rectTransform.localScale = Vector3.one * 1.7f;
        bannerText.enabled = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float rate = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            float alpha = Mathf.Sin(rate * Mathf.PI);
            bannerText.color = new Color(color.r, color.g, color.b, alpha);
            bannerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.7f, 0.92f, Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, rate * 2f)));
            yield return null;
        }

        bannerText.enabled = false;
        bannerRoutine = null;
    }

    public void ShowWarning(string message = "WARNING!")
    {
        ShowBanner(message, new Color(1f, 0.16f, 0.12f), 0.55f);
        Flash(new Color(1f, 0f, 0f, 0.28f), 0.35f);
        PlayTone(155f, 0.18f, 0.18f);
    }

    public void Flash(Color color, float duration)
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = color.a * (1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
            flashImage.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        flashImage.color = Color.clear;
        flashRoutine = null;
    }

    public void PlayTone(float frequency, float duration, float volume)
    {
        int sampleRate = 22050;
        int sampleCount = Mathf.Max(1, Mathf.CeilToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = Mathf.Sin(Mathf.PI * i / sampleCount);
            samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create("Runtime Presentation Tone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        audioSource.PlayOneShot(clip);
        Destroy(clip, duration + 0.2f);
    }

    public void SpawnBurst(Vector3 position, Color color, int count = 22, float size = 0.45f)
    {
        GameObject burstObject = new GameObject("Presentation Burst");
        burstObject.transform.position = position;
        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.45f, size);
        main.startColor = color;
        main.gravityModifier = 0.35f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
        {
            Material material = new Material(shader);
            material.color = color;
            renderer.material = material;
        }

        particles.Emit(count);
        particles.Play();
    }

    public void ShakeCamera(float amplitude, float duration)
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        CameraPresentationImpulse impulse = camera.GetComponent<CameraPresentationImpulse>();
        if (impulse == null) impulse = camera.gameObject.AddComponent<CameraPresentationImpulse>();
        impulse.Play(amplitude, duration);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        Image image = gameObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float size, TextAlignmentOptions alignment)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        text.outlineWidth = 0.18f;
        text.outlineColor = new Color32(20, 10, 30, 220);
        return text;
    }

    private static void StretchFullScreen(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

[DefaultExecutionOrder(2000)]
public sealed class CameraPresentationImpulse : MonoBehaviour
{
    private float amplitude;
    private float duration;
    private float elapsed;

    public void Play(float newAmplitude, float newDuration)
    {
        amplitude = Mathf.Max(amplitude, newAmplitude);
        duration = Mathf.Max(0.01f, newDuration);
        elapsed = 0f;
    }

    private void LateUpdate()
    {
        if (elapsed >= duration) return;
        elapsed += Time.deltaTime;
        float strength = amplitude * (1f - Mathf.Clamp01(elapsed / duration));
        transform.position += (Vector3)Random.insideUnitCircle * strength;
        transform.rotation *= Quaternion.Euler(0f, 0f, Random.Range(-strength, strength) * 2f);
    }
}
