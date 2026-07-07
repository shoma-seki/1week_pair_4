using System.Collections;
using UnityEngine;

public class LightController : MonoBehaviour
{
    [Header("Fall")]
    [SerializeField, Min(0.01f)] private float fallSpeed = 5f;
    [SerializeField] private float groundY;
    [SerializeField, Min(0f)] private float delayBeforeShake = 3f;
    [SerializeField, Min(0f)] private float shakeTime = 2f;
    [SerializeField, Min(0f)] private float shakeAmount = 0.1f;

    [Header("Fire")]
    [SerializeField] private FireCOntorol firePrefab;
    [SerializeField] private Vector3 fireSpawnOffset = new Vector3(0f, 0.5f, 0f);

    public bool IsFalling { get; private set; }

    private bool isShaking;
    private bool hasLanded;

    private void Start()
    {
        StartCoroutine(FallSequence());
    }

    private void Update()
    {
        if (!IsFalling || hasLanded)
        {
            return;
        }

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        if (transform.position.y <= groundY)
        {
            Land();
        }
    }

    /// <summary>
    /// Starts the light's shake-and-fall sequence. Repeated calls are ignored.
    /// </summary>
    public void BeginFall()
    {
        if (IsFalling || isShaking || hasLanded)
        {
            return;
        }

        StartCoroutine(FallSequence());
    }

    private IEnumerator FallSequence()
    {
        isShaking = true;
        yield return new WaitForSeconds(delayBeforeShake);

        Vector3 startPosition = transform.position;
        float elapsed = 0f;

        while (elapsed < shakeTime)
        {
            elapsed += Time.deltaTime;
            float horizontalOffset = Mathf.Sin(elapsed * 20f) * shakeAmount;
            transform.position = startPosition + Vector3.right * horizontalOffset;
            yield return null;
        }

        transform.position = startPosition;
        isShaking = false;
        IsFalling = true;
    }

    private void Land()
    {
        hasLanded = true;
        IsFalling = false;

        Vector3 landingPosition = transform.position;
        landingPosition.y = groundY;
        transform.position = landingPosition;

        if (firePrefab != null)
        {
            Instantiate(firePrefab, landingPosition + fireSpawnOffset, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("LightController: Fire Prefab is not assigned.", this);
        }

        Destroy(gameObject);
    }

    private void OnValidate()
    {
        fallSpeed = Mathf.Max(0.01f, fallSpeed);
        delayBeforeShake = Mathf.Max(0f, delayBeforeShake);
        shakeTime = Mathf.Max(0f, shakeTime);
        shakeAmount = Mathf.Max(0f, shakeAmount);
    }
}
