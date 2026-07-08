using UnityEngine;

public class DoorSceneChange : MonoBehaviour
{
    [SerializeField] private SceneChange sceneChange;
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
            sceneChange.LoadScene("Game");
        }
    }
}
