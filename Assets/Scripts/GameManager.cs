using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game Clear")]
    [SerializeField] private Player player;
    [SerializeField, Min(0.01f)] private float clearDistance = 100f;
    [SerializeField] private string clearSceneName = "Result";
    [SerializeField] private UnityEvent onGameCleared;

    private SceneChange scene;

    public bool IsGameCleared { get; private set; }

    private void Start()
    {
        scene = SceneChange.Instance != null ? SceneChange.Instance : FindAnyObjectByType<SceneChange>();

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }

        if (player != null)
        {
            player.DistanceChanged += CheckGameClear;
            CheckGameClear(player.DistanceTraveled);
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.DistanceChanged -= CheckGameClear;
        }
    }

    private void Update()
    {
        SceneChange activeSceneChange = SceneChange.Instance != null ? SceneChange.Instance : scene;

        if (player == null && Input.GetKeyDown(KeyCode.G) && activeSceneChange != null)
        {
            activeSceneChange.LoadScene("Game");
        }
    }

    private void CheckGameClear(float distance)
    {
        if (IsGameCleared || distance < clearDistance)
        {
            return;
        }

        IsGameCleared = true;
        player.StopMovement();
        onGameCleared?.Invoke();
        Debug.Log($"Game Clear! Distance: {distance:F1}", this);

        SceneChange activeSceneChange = SceneChange.Instance != null ? SceneChange.Instance : scene;

        if (activeSceneChange != null)
        {
            activeSceneChange.LoadScene(clearSceneName);
        }
        else
        {
            Debug.LogError("SceneChange was not found. Cannot load the clear scene.", this);
        }
    }

    private void OnValidate()
    {
        clearDistance = Mathf.Max(0.01f, clearDistance);
    }
}
