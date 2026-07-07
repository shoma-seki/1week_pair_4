using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private FanManager fanManager;
    private Quaternion baseLocalRotation;
    private float rotationTime;

    private void OnEnable()
    {
        baseLocalRotation = transform.localRotation;
        rotationTime = 0f;

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
    }

    private void OnValidate()
    {
        if (fanCountText == null)
        {
            fanCountText = GetComponent<TextMeshProUGUI>();
        }
    }
}
