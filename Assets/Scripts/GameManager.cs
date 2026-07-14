using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [Header("Game Clear")]
    [SerializeField] private Player player;
    [SerializeField, Min(0.01f)] private float clearDistance = 100f;
    [SerializeField] private string clearSceneName = "Result";
    [SerializeField] private UnityEvent onGameCleared;
    [SerializeField, Min(0f)] private float clearPresentationDuration = 1.6f;

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

        StartCoroutine(PlayClearAndLoadResult());
    }

    private IEnumerator PlayClearAndLoadResult()
    {
        GameAudioManager.Instance?.StopUrineLoop();
        player.PlayClearCelebration(clearPresentationDuration);
        PresentationDirector director = PresentationDirector.Instance;
        if (director != null)
        {
            director.ShowBanner("CLEAR!", new Color(1f, 0.82f, 0.18f), clearPresentationDuration);
            director.Flash(new Color(1f, 0.78f, 0.2f, 0.4f), 0.8f);
            director.SpawnBurst(player.transform.position + Vector3.up * 2f, new Color(1f, 0.55f, 0.2f), 55, 0.8f);
            director.ShakeCamera(0.22f, 0.55f);
            director.PlayTone(1046.5f, 0.35f, 0.24f);
        }

        yield return new WaitForSeconds(clearPresentationDuration);

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
        clearPresentationDuration = Mathf.Max(0f, clearPresentationDuration);
    }
}
