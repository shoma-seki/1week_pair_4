using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private float destroyDistance = 20f;

    private Vector3 startPosition;
    private bool isMoving = false;
    private Vector3 moveDirection;
    private bool hasReversed = false;
    private bool isShrinking = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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

        if (isShrinking)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                Vector3.zero,
                Time.deltaTime * 2f);

            if (transform.localScale.magnitude < 0.05f)
            {
                Destroy(gameObject);
            }
        }
    }

    //“–‚½‚è”»’è‚ÆƒvƒŒƒCƒ„[‚ª‚Ô‚Â‚©‚½‚ç
    public void StartMove(Transform target)
    {
        if (isMoving) return;


        meshRenderer.enabled = true;

        moveDirection = (target.position - transform.position).normalized;
        Debug.Log(moveDirection);
        isMoving = true;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("‚ ‚½‚½");
           Destroy(gameObject);

        }

        if (!other.CompareTag("Shoben"))
            return;

        ReverseDirection();

        
    }

   

    private void ReverseDirection()
    {
        if (hasReversed) return;

        hasReversed = true;
        moveDirection = -moveDirection;

        isShrinking = true;
    }
}
