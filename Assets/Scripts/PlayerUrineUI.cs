using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's urine resource using a filled UI Image.
/// </summary>
[RequireComponent(typeof(Image))]
public class PlayerUrineUI : MonoBehaviour
{
    [SerializeField] private Player player;

    private Image resourceImage;

    private void Awake()
    {
        resourceImage = GetComponent<Image>();

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }
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
        resourceImage.fillAmount = maxUrine > 0f
            ? Mathf.Clamp01(currentUrine / maxUrine)
            : 0f;
    }
}
