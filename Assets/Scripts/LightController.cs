using System.Collections;
using UnityEngine;


public class LightController : MonoBehaviour
{
    [Header("“d‹CŠÖŒW")]
    [SerializeField] private float fallSpeed = 5.0f;
    [SerializeField] private float groundY = 0.0f;
    private bool isFalling = false;
    private bool isShaking = false;
    [SerializeField] private float shakeTime = 2f;
    [SerializeField] private float shakeAmount = 0.1f;


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
        if (Input.GetKeyDown(KeyCode.Space) && !isFalling && !isShaking)
        {
            //isFalling = true;

            StartCoroutine(ShakeThenFall());
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

    private IEnumerator ShakeThenFall()
    {
        isShaking = true;

        Vector3 startPos = transform.position;
        float timer = 0f;

        while (timer < shakeTime)
        {
            timer += Time.deltaTime;

            float x = Mathf.Sin(timer * 20f) * shakeAmount;
            transform.position = startPos + new Vector3(x, 0, 0);

            yield return null;
        }

        transform.position = startPos;

        isShaking = false;
        isFalling = true;
    }
}
