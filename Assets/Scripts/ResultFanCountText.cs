using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// リザルト画面に獲得ファン数を表示します。
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class ResultFanCountText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI fanCountText;
    [SerializeField] private string prefix = "ファン数: ";
    [SerializeField] private string suffix = "人";

    private FanManager fanManager;
    private Coroutine countRoutine;
    private int displayedCount;

    private void Awake()
    {
        if (fanCountText == null)
        {
            fanCountText = GetComponent<TextMeshProUGUI>();
        }
    }

    private void OnEnable()
    {
        fanManager = FanManager.Instance;
        if (fanManager == null)
        {
            UpdateText(0);
            return;
        }

        fanManager.FanCountChanged += AnimateTo;
        displayedCount = 0;
        countRoutine = StartCoroutine(CountUp(fanManager.FanCount));
    }

    private void OnDisable()
    {
        if (fanManager != null)
        {
            fanManager.FanCountChanged -= AnimateTo;
        }

        if (countRoutine != null) StopCoroutine(countRoutine);
    }

    private void AnimateTo(int fanCount)
    {
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountUp(fanCount));
    }

    private IEnumerator CountUp(int targetCount)
    {
        int startCount = displayedCount;
        float duration = Mathf.Clamp(0.75f + Mathf.Abs(targetCount - startCount) * 0.025f, 0.75f, 2.2f);
        float elapsed = 0f;
        int previousShown = startCount;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rate = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            displayedCount = Mathf.RoundToInt(Mathf.Lerp(startCount, targetCount, rate));
            UpdateText(displayedCount);

            if (displayedCount != previousShown && (displayedCount % 5 == 0 || targetCount <= 10))
            {
                PresentationDirector.Instance?.PlayTone(520f + displayedCount * 5f, 0.045f, 0.055f);
                previousShown = displayedCount;
            }
            yield return null;
        }

        displayedCount = targetCount;
        UpdateText(displayedCount);
        fanCountText.rectTransform.localScale = Vector3.one * 1.18f;
        float settle = 0f;
        while (settle < 0.25f)
        {
            settle += Time.deltaTime;
            fanCountText.rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.18f, Vector3.one, settle / 0.25f);
            yield return null;
        }
        fanCountText.rectTransform.localScale = Vector3.one;
        countRoutine = null;
    }

    private void UpdateText(int fanCount)
    {
        fanCountText.text = prefix + fanCount + suffix;
    }

    private void OnValidate()
    {
        if (fanCountText == null)
        {
            fanCountText = GetComponent<TextMeshProUGUI>();
        }
    }
}
