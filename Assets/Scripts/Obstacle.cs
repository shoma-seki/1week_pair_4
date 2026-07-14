using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField, Min(1)] private int fanDecreaseAmount = 1;
    [SerializeField] private Vector3 moveDirection = Vector3.left;　　//飛ぶ向き

    private Vector3 startPosition;
    private bool isMoving = false;
    private bool isShrinking = false;
    private bool hasHitPlayer = false;
    private Vector3 visibleScale;

    [SerializeField] private GameObject child;
    MeshRenderer meshRenderer;

    [Header("Rotation")]
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 0f, 0f);
    //[SerializeField] private GameObject child;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = TryGetComponent<MeshRenderer>(out var renderer) ? renderer : child.GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        startPosition = transform.position;
        visibleScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {

            transform.position += moveDirection * moveSpeed * Time.deltaTime;
            child.transform.Rotate(rotationSpeed * Time.deltaTime);
        }

        //if (isShrinking)
        //{
        //    transform.localScale = Vector3.Lerp(
        //        transform.localScale,
        //        Vector3.zero,
        //        Time.deltaTime * 2f);

        //    if (transform.localScale.magnitude < 0.05f)
        //    {
        //        Destroy(gameObject);
        //    }
        //}
    }

    //当たり判定とプレイヤーがぶつかたら
    public void StartMove()
    {

        if (isMoving) return;

        meshRenderer.enabled = true;
        isMoving = true;
        StartCoroutine(PlayEntrance());
        Destroy(gameObject, lifeTime);
    }

    private System.Collections.IEnumerator PlayEntrance()
    {
        float duration = 0.22f;
        float elapsed = 0f;
        transform.localScale = visibleScale * 0.15f;
        PresentationDirector.Instance?.SpawnBurst(transform.position, new Color(1f, 0.25f, 0.18f), 12, 0.25f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float rate = Mathf.Clamp01(elapsed / duration);
            float overshoot = 1f + Mathf.Sin(rate * Mathf.PI) * 0.3f;
            transform.localScale = visibleScale * Mathf.Lerp(0.15f, overshoot, Mathf.SmoothStep(0f, 1f, rate));
            yield return null;
        }

        transform.localScale = visibleScale;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!hasHitPlayer)
            {
                hasHitPlayer = true;
                FanManager.Instance?.RemoveFan(fanDecreaseAmount);
                PresentationDirector director = PresentationDirector.Instance;
                if (director != null)
                {
                    director.SpawnBurst(transform.position, new Color(1f, 0.12f, 0.08f), 36, 0.65f);
                    director.Flash(new Color(1f, 0f, 0f, 0.34f), 0.5f);
                    director.ShakeCamera(0.32f, 0.45f);
                    director.PlayTone(95f, 0.28f, 0.24f);
                }
            }

            meshRenderer.enabled = false;
            Destroy(gameObject, 0.06f);

        }

    }

    private void OnValidate()
    {
        fanDecreaseAmount = Mathf.Max(1, fanDecreaseAmount);
    }

    //private void ReverseDirection()
    //{
    //    if (hasReversed) return;

    //    hasReversed = true;
    //    moveDirection = -moveDirection;

    //    isShrinking = true;
    //}
}
