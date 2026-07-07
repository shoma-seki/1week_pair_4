using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private Vector3 moveDirection = Vector3.left;　　//飛ぶ向き

    private Vector3 startPosition;
    private bool isMoving = false;
    private bool isShrinking = false;

    [SerializeField] private GameObject child;
    MeshRenderer meshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshRenderer = TryGetComponent<MeshRenderer>(out var renderer) ? renderer : child.GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        startPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (isMoving)
        {

            transform.position += moveDirection * moveSpeed * Time.deltaTime;

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

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            //
            //Debug.Log("あたた");
            Destroy(gameObject);

        }

    }

    //private void ReverseDirection()
    //{
    //    if (hasReversed) return;

    //    hasReversed = true;
    //    moveDirection = -moveDirection;

    //    isShrinking = true;
    //}
}
