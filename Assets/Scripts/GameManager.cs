using UnityEngine;

public class GameManager : MonoBehaviour
{
    SceneChange scene;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scene = FindAnyObjectByType<SceneChange>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            scene.LoadScene("Game");
        }
    }
}
