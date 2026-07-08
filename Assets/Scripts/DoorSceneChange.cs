using UnityEngine;

public class DoorSceneChange : MonoBehaviour
{
    [SerializeField] private SceneChange sceneChange;
    [SerializeField] private string sceneName = "Game";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneChange activeSceneChange = SceneChange.Instance != null ? SceneChange.Instance : sceneChange;

            if (activeSceneChange == null)
            {
                Debug.LogError("SceneChange was not found. Cannot load the scene.", this);
                return;
            }

            activeSceneChange.LoadScene(sceneName);
        }
    }
}
