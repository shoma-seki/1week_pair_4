using UnityEngine;

public class FireAnimation : MonoBehaviour
{
    [SerializeField] private Renderer fireRenderer;
    [SerializeField] private Texture[] fireTextures;
    [SerializeField] private float frameRate = 0.1f;

    private Material materialInstance;
    private int currentFrame;
    private float timer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        materialInstance = fireRenderer.material;

        if (fireTextures.Length > 0)
        {
            materialInstance.mainTexture = fireTextures[0];
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (fireTextures.Length == 0)
            return;

        timer += Time.deltaTime;

        if (timer >= frameRate)
        {
            timer = 0f;

            currentFrame++;
            if (currentFrame >= fireTextures.Length)
                currentFrame = 0;

            materialInstance.mainTexture = fireTextures[currentFrame];
        }
    }
}
