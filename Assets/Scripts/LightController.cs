using System.Collections;
using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("電気関係")]
    [SerializeField] private float fallSpeed = 5.0f;
    [SerializeField] private float groundY = 0.0f;
    [SerializeField] private float shakeDelay = 3f;
    private bool isFalling = false;
    private bool isShaking = false;
    [SerializeField] private float shakeTime = 2f;
    [SerializeField] private float shakeAmount = 0.1f;

    [Header("火関連")]
    [SerializeField] private FireCOntorol firePrefab;

    void Start()
    {
        StartCoroutine(WaitThenShakeAndFall());
    }

    void Update()
    {
        // 落下
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

    private IEnumerator WaitThenShakeAndFall()
    {
        yield return new WaitForSeconds(shakeDelay);
        yield return ShakeThenFall();
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
