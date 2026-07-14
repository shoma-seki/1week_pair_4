using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's urine resource using a filled UI Image.
/// </summary>
[RequireComponent(typeof(Image))]
public class PlayerUrineUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField, Min(0.01f)] private float fillSmoothTime = 0.08f;
    [SerializeField] private Color normalColor = new Color(0.1f, 0.65f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.18f, 0.12f);
    [SerializeField] private Color chargedColor = new Color(1f, 0.82f, 0.18f);

    private Image resourceImage;
    private RectTransform resourceRect;
    private float targetFill = 1f;
    private float fillVelocity;
    private float pulseTime;
    private float lastTargetFill = 1f;
    private Player.UrineStage previousStage;
    private Vector3 baseScale;

    private void Awake()
    {
        resourceImage = GetComponent<Image>();
        resourceRect = resourceImage.rectTransform;
        baseScale = resourceRect.localScale;

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }
    }

    private void Start()
    {
        if (player != null)
        {
            previousStage = player.CurrentUrineStage;
        }
    }

    private void Update()
    {
        resourceImage.fillAmount = Mathf.SmoothDamp(resourceImage.fillAmount, targetFill, ref fillVelocity, fillSmoothTime);

        if (player != null && player.CurrentUrineStage != previousStage)
        {
            previousStage = player.CurrentUrineStage;
            pulseTime = 0.32f;
        }

        if (pulseTime > 0f)
        {
            pulseTime -= Time.deltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(pulseTime / 0.32f) * Mathf.PI);
            resourceRect.localScale = baseScale * (1f + pulse * 0.18f);
        }
        else
        {
            resourceRect.localScale = Vector3.Lerp(resourceRect.localScale, baseScale, Time.deltaTime * 12f);
        }

        Color stageColor = player != null && player.CurrentUrineStage == Player.UrineStage.Third
            ? chargedColor
            : Color.Lerp(dangerColor, normalColor, targetFill);
        resourceImage.color = Color.Lerp(resourceImage.color, stageColor, Time.deltaTime * 10f);
    }

    private void OnEnable()
    {
        if (player == null)
        {
            return;
        }

        player.UrineChanged += UpdateDisplay;
        UpdateDisplay(player.CurrentUrine, player.MaxUrine);
    }

    private void OnDisable()
    {
        if (player != null)
        {
            player.UrineChanged -= UpdateDisplay;
        }
    }

    private void UpdateDisplay(float currentUrine, float maxUrine)
    {
        targetFill = maxUrine > 0f
            ? Mathf.Clamp01(currentUrine / maxUrine)
            : 0f;

        if (targetFill > lastTargetFill + 0.2f || targetFill <= 0.001f)
        {
            pulseTime = 0.32f;
        }
        lastTargetFill = targetFill;
    }
}
