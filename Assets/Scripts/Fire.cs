using UnityEngine;

public class Fire : MonoBehaviour
{
    [Header("Life")]
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private float extinguishTime = 2f;   // è¡âŒÇ…ïKóvÇ»éûä‘

    [Header("Grow")]
    [SerializeField] private float growSpeed = 1f;
    [SerializeField] private float maxScale = 2f;

    [SerializeField] private MeshRenderer meshRenderer;
    private bool isActive = false;
    private float urineContactTime = 0f;

    [Header("Shake")]
    [SerializeField] private float shakeAmount = 0.05f;     // óhÇÍÇÈïù
    [SerializeField] private float shakeSpeed = 8f;         // óhÇÍÇÈë¨Ç≥

    private Vector3 startPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.enabled = false;

        transform.localScale = Vector3.zero;

        startPosition = transform.position;

        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isActive)
            return;

        if (transform.localScale.x < maxScale)
        {
            transform.localScale += Vector3.one * growSpeed * Time.deltaTime;

            if (transform.localScale.x > maxScale)
            {
                transform.localScale = Vector3.one * maxScale;
            }
        }

        if (isActive)
        {
            float x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;

            transform.position = startPosition + new Vector3(x, 0f, 0f);
        }
    }

    public void ActivateFire()
    {
        if (isActive)
            return;

        isActive = true;

        meshRenderer.enabled = true;

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Shoben"))
            return;

        urineContactTime += Time.deltaTime;

        if (urineContactTime >= extinguishTime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Shoben"))
            return;

        urineContactTime = 0f;
    }
}
