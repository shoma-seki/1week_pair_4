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
        fanManager.FanCountChanged += UpdateText;
        UpdateText(fanManager.FanCount);
    }

    private void OnDisable()
    {
        if (fanManager != null)
        {
            fanManager.FanCountChanged -= UpdateText;
        }
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
