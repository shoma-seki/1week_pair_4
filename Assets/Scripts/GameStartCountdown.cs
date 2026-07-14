using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-1000)]
public class GameStartCountdown : MonoBehaviour
{
    [Header("TextMeshPro")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField, Min(1f)] private float fontSize = 180f;

    [Header("Timing")]
    [SerializeField, Min(0.1f)] private float numberDuration = 1f;
    [SerializeField, Min(0.1f)] private float goDuration = 0.75f;

    [Header("GO Flash")]
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField, Min(0.05f)] private float flashDuration = 0.35f;
    [SerializeField, Min(1f)] private float lightIntensityMultiplier = 2.5f;

    private Player player;
    private MusicManager musicManager;
    private bool playerWasEnabled;
    private bool musicWasEnabled;
    private TextMeshProUGUI countdownText;
    private Image flashImage;
    private Light[] sceneLights;
    private float[] originalLightIntensities;

    private void Awake()
    {
        player = FindAnyObjectByType<Player>();
        musicManager = FindAnyObjectByType<MusicManager>();

        if (player != null)
        {
            playerWasEnabled = player.enabled;
            player.enabled = false;
        }

        if (musicManager != null)
        {
            musicWasEnabled = musicManager.enabled;
            musicManager.enabled = false;
        }

        CreateCountdownText();
    }

    private void Start()
    {
        StartCoroutine(PlayCountdown());
    }

    private IEnumerator PlayCountdown()
    {
        yield return ShowText("3", numberDuration);
        yield return ShowText("2", numberDuration);
        yield return ShowText("1", numberDuration);

        countdownText.text = "GO";
        StartGameplay();
        StartCoroutine(PlayGoFlash());
        yield return new WaitForSecondsRealtime(goDuration);

        Destroy(countdownText.transform.parent.gameObject);
    }

    private IEnumerator ShowText(string value, float duration)
    {
        countdownText.text = value;
        yield return new WaitForSecondsRealtime(duration);
    }

    private void StartGameplay()
    {
        if (player != null)
        {
            player.enabled = playerWasEnabled;
        }

        if (musicManager != null)
        {
            musicManager.enabled = musicWasEnabled;
        }
    }

    private void CreateCountdownText()
    {
        GameObject canvasObject = new GameObject("CountdownCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject flashObject = new GameObject("GoFlash", typeof(RectTransform), typeof(Image));
        flashObject.transform.SetParent(canvasObject.transform, false);

        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;

        flashImage = flashObject.GetComponent<Image>();
        flashImage.color = Color.clear;
        flashImage.raycastTarget = false;

        GameObject textObject = new GameObject("CountdownText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        countdownText = textObject.GetComponent<TextMeshProUGUI>();
        countdownText.font = font != null ? font : TMP_Settings.defaultFontAsset;
        countdownText.color = textColor;
        countdownText.fontSize = fontSize;
        countdownText.fontStyle = FontStyles.Bold;
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownText.raycastTarget = false;
    }

    private IEnumerator PlayGoFlash()
    {
        CacheSceneLights();

        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = originalLightIntensities[i] * lightIntensityMultiplier;
            }
        }

        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float strength = 1f - Mathf.Clamp01(elapsed / flashDuration);
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashColor.a * strength);

            for (int i = 0; i < sceneLights.Length; i++)
            {
                if (sceneLights[i] != null)
                {
                    sceneLights[i].intensity = Mathf.Lerp(
                        originalLightIntensities[i],
                        originalLightIntensities[i] * lightIntensityMultiplier,
                        strength);
                }
            }

            yield return null;
        }

        flashImage.color = Color.clear;
        RestoreSceneLights();
    }

    private void CacheSceneLights()
    {
        sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        originalLightIntensities = new float[sceneLights.Length];

        for (int i = 0; i < sceneLights.Length; i++)
        {
            originalLightIntensities[i] = sceneLights[i].intensity;
        }
    }

    private void RestoreSceneLights()
    {
        if (sceneLights == null || originalLightIntensities == null)
        {
            return;
        }

        for (int i = 0; i < sceneLights.Length; i++)
        {
            if (sceneLights[i] != null)
            {
                sceneLights[i].intensity = originalLightIntensities[i];
            }
        }
    }

    private void OnDestroy()
    {
        RestoreSceneLights();
        StartGameplay();
    }

    private void OnValidate()
    {
        fontSize = Mathf.Max(1f, fontSize);
        numberDuration = Mathf.Max(0.1f, numberDuration);
        goDuration = Mathf.Max(0.1f, goDuration);
        flashDuration = Mathf.Max(0.05f, flashDuration);
        lightIntensityMultiplier = Mathf.Max(1f, lightIntensityMultiplier);
    }
}
