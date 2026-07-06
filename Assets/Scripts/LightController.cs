using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("“d‹CŠÖŒW")]
    [SerializeField] private float fallSpeed = 5.0f;
    [SerializeField] private float groundY = 0.0f;
    private bool isFalling = false;

    [Header("‰ÎŠÖ˜A")]
    [SerializeField] private FireCOntorol firePrefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //‰¼
        if (Input.GetKeyDown(KeyCode.Space) && !isFalling)
        {
            isFalling = true;
        }
        
        //—Ž‰º
        if (isFalling)
        {
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            if (transform.position.y <= groundY)
            {
                SpawnFire();
            }
        }

    }

    void SpawnFire()
    {
        Vector3 spawnPosition = transform.position;
        spawnPosition.y += 0.5f;  

        Instantiate(firePrefab, spawnPosition, Quaternion.identity);
        Destroy(gameObject);
    }
}
