using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FanManager が管理しているファン数を UI に表示します。
/// </summary>
public class FanCountText : MonoBehaviour
{
    // ファン数を表示したい UI の Text コンポーネントを、Inspector でここに設定してください。
    [SerializeField] private Text fanCountText;

    // 数字の前に表示する文字です。数字だけ表示したい場合は空欄にしてください。
    [SerializeField] private string prefix = "ファン数: ";

    private FanManager fanManager;

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
        if (fanCountText != null)
        {
            fanCountText.text = prefix + fanCount;
        }
    }

    private void OnValidate()
    {
        if (fanCountText == null)
        {
            fanCountText = GetComponent<Text>();
        }
    }
}
