using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// FanManager が管理しているファン数を UI に表示します。
/// </summary>
public class FanCountText : MonoBehaviour
{
    // ファン数を表示したい UI の Text コンポーネントを、Inspector でここに設定してください。
    [SerializeField] private TextMeshProUGUI fanCountText;

    // 数字の前に表示する文字です。数字だけ表示したい場合は空欄にしてください。
    [SerializeField] private string prefix = "ファン数";

    [Header("Rotation Animation")]
    [SerializeField, Min(0f)] private float rotateYRange = 15f;
    [SerializeField, Min(0f)] private float rotateYSpeed = 2f;

    [Header("Count Change Animation")]
    [SerializeField] private Color increaseColor = new Color(1f, 0.85f, 0.1f);
    [SerializeField] private Color decreaseColor = new Color(1f, 0.15f, 0.1f);
    [SerializeField, Min(0f)] private float colorDuration = 0.35f;
    [SerializeField, Min(0.01f)] private float increaseDuration = 0.28f;
    [SerializeField, Min(0f)] private float increaseScaleAmount = 0.22f;
    [SerializeField, Min(0.01f)] private float decreaseDuration = 0.28f;
    [SerializeField, Min(0f)] private float decreaseMoveDistance = 18f;

    [Header("Decrease Vignette")]
    [SerializeField] private Volume vignetteVolume;
    [SerializeField] private Color decreaseVignetteColor = new Color(1f, 0f, 0f);
    [SerializeField, Range(0f, 1f)] private float decreaseVignetteIntensity = 0.55f;
    [SerializeField, Range(0f, 1f)] private float decreaseVignetteSmoothness = 0.45f;

    private FanManager fanManager;
    private Quaternion baseLocalRotation;
    private float rotationTime;
    private RectTransform fanCountRectTransform;
    private Vector3 baseTextScale = Vector3.one;
    private Vector2 baseTextAnchoredPosition;
    private Color baseTextColor = Color.white;
    private Coroutine countChangeCoroutine;
    private int currentFanCount;
    private bool hasCurrentFanCount;
    private Vignette vignette;
    private Color baseVignetteColor;
    private float baseVignetteIntensity;
    private float baseVignetteSmoothness;
    private bool baseVignetteColorOverrideState;
    private bool baseVignetteIntensityOverrideState;
    private bool baseVignetteSmoothnessOverrideState;
    private bool hasVignette;

    private void OnEnable()
    {
        baseLocalRotation = transform.localRotation;
        rotationTime = 0f;
        hasCurrentFanCount = false;
        CacheTextBaseValues();
        CacheVignette();

        fanManager = FanManager.Instance;
        fanManager.FanCountChanged += UpdateText;
        UpdateText(fanManager.FanCount);
    }

    private void Update()
    {
        rotationTime += Time.deltaTime * rotateYSpeed;
        float yAngle = Mathf.Sin(rotationTime) * rotateYRange;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, yAngle, 0f);
    }

    private void OnDisable()
    {
        transform.localRotation = baseLocalRotation;
        StopCountChangeAnimation();
        ResetTextVisuals();
        ResetVignette();

        if (fanManager != null)
        {
            fanManager.FanCountChanged -= UpdateText;
        }
    }

    private void UpdateText(int fanCount)
    {
        if (fanCountText != null)
        {
            fanCountText.text = prefix + fanCount + "人";
        }

        if (!hasCurrentFanCount)
        {
            currentFanCount = fanCount;
            hasCurrentFanCount = true;
            return;
        }

        int changeAmount = fanCount - currentFanCount;
        currentFanCount = fanCount;

        if (changeAmount > 0)
        {
            PlayCountChangeAnimation(true);
        }
        else if (changeAmount < 0)
        {
            PlayCountChangeAnimation(false);
        }
    }

    private void PlayCountChangeAnimation(bool isIncrease)
    {
        if (!isActiveAndEnabled || fanCountText == null)
        {
            return;
        }

        StopCountChangeAnimation();
        ResetTextVisuals();
        ResetVignette();
        CacheTextBaseValues();
        countChangeCoroutine = StartCoroutine(AnimateCountChange(isIncrease));
    }

    private IEnumerator AnimateCountChange(bool isIncrease)
    {
        Color targetColor = isIncrease ? increaseColor : decreaseColor;
        float moveDuration = isIncrease ? increaseDuration : decreaseDuration;
        float duration = Mathf.Max(moveDuration, colorDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float moveRate = Mathf.Clamp01(elapsed / moveDuration);
            float colorRate = colorDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / colorDuration);

            fanCountText.color = Color.Lerp(targetColor, baseTextColor, EaseOut(colorRate));

            if (isIncrease)
            {
                float bounce = Mathf.Sin(moveRate * Mathf.PI);
                float stretch = 1f + increaseScaleAmount * bounce;
                float squash = 1f - increaseScaleAmount * 0.45f * bounce;
                fanCountRectTransform.localScale = Vector3.Scale(
                    baseTextScale,
                    new Vector3(stretch, squash, 1f)
                );
            }
            else
            {
                float drop = Mathf.Sin(moveRate * Mathf.PI) * decreaseMoveDistance;
                fanCountRectTransform.anchoredPosition = baseTextAnchoredPosition + Vector2.down * drop;
                ApplyDecreaseVignette(moveRate);
            }

            yield return null;
        }

        ResetTextVisuals();
        ResetVignette();
        countChangeCoroutine = null;
    }

    private void CacheVignette()
    {
        hasVignette = false;

        if (vignetteVolume == null)
        {
            vignetteVolume = FindAnyObjectByType<Volume>();
        }

        if (vignetteVolume == null || vignetteVolume.profile == null)
        {
            return;
        }

        if (!vignetteVolume.profile.TryGet(out vignette))
        {
            vignette = vignetteVolume.profile.Add<Vignette>(true);
        }

        baseVignetteColor = vignette.color.value;
        baseVignetteIntensity = vignette.intensity.value;
        baseVignetteSmoothness = vignette.smoothness.value;
        baseVignetteColorOverrideState = vignette.color.overrideState;
        baseVignetteIntensityOverrideState = vignette.intensity.overrideState;
        baseVignetteSmoothnessOverrideState = vignette.smoothness.overrideState;
        hasVignette = true;
    }

    private void ApplyDecreaseVignette(float rate)
    {
        if (!hasVignette || vignette == null)
        {
            return;
        }

        float pulse = Mathf.Sin(rate * Mathf.PI);
        float targetIntensity = Mathf.Max(baseVignetteIntensity, decreaseVignetteIntensity);
        float targetSmoothness = Mathf.Max(baseVignetteSmoothness, decreaseVignetteSmoothness);

        vignette.color.overrideState = true;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
        vignette.color.value = Color.Lerp(baseVignetteColor, decreaseVignetteColor, pulse);
        vignette.intensity.value = Mathf.Lerp(baseVignetteIntensity, targetIntensity, pulse);
        vignette.smoothness.value = Mathf.Lerp(baseVignetteSmoothness, targetSmoothness, pulse);
    }

    private void ResetVignette()
    {
        if (!hasVignette || vignette == null)
        {
            return;
        }

        vignette.color.value = baseVignetteColor;
        vignette.intensity.value = baseVignetteIntensity;
        vignette.smoothness.value = baseVignetteSmoothness;
        vignette.color.overrideState = baseVignetteColorOverrideState;
        vignette.intensity.overrideState = baseVignetteIntensityOverrideState;
        vignette.smoothness.overrideState = baseVignetteSmoothnessOverrideState;
    }

    private void CacheTextBaseValues()
    {
        if (fanCountText == null)
        {
            return;
        }

        fanCountRectTransform = fanCountText.rectTransform;
        baseTextScale = fanCountRectTransform.localScale;
        baseTextAnchoredPosition = fanCountRectTransform.anchoredPosition;
        baseTextColor = fanCountText.color;
    }

    private void StopCountChangeAnimation()
    {
        if (countChangeCoroutine == null)
        {
            return;
        }

        StopCoroutine(countChangeCoroutine);
        countChangeCoroutine = null;
    }

    private void ResetTextVisuals()
    {
        if (fanCountText == null || fanCountRectTransform == null)
        {
            return;
        }

        fanCountText.color = baseTextColor;
        fanCountRectTransform.localScale = baseTextScale;
        fanCountRectTransform.anchoredPosition = baseTextAnchoredPosition;
    }

    private void OnValidate()
    {
        if (fanCountText == null)
        {
            fanCountText = GetComponent<TextMeshProUGUI>();
        }
    }

    private static float EaseOut(float rate)
    {
        return 1f - Mathf.Pow(1f - rate, 3f);
    }
}
