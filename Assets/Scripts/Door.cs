using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;
    [SerializeField] private float slideDistance = 1.5f;
    [SerializeField] private float slideSpeed = 1.5f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private bool isOpening = false;

    private void Awake()
    {
        foreach (Collider childCollider in GetComponentsInChildren<Collider>(true))
        {
            if (childCollider.transform == transform)
                continue;

            if (childCollider.TryGetComponent(out DoorTrigger trigger))
                trigger.Initialize(this);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Debug.Log("Door Start");
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        leftOpenPos = leftClosedPos + Vector3.left * slideDistance;
        rightOpenPos = rightClosedPos + Vector3.right * slideDistance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isOpening)
            return;

        leftDoor.localPosition = Vector3.MoveTowards(
            leftDoor.localPosition,
            leftOpenPos,
            slideSpeed * Time.deltaTime);

        rightDoor.localPosition = Vector3.MoveTowards(
            rightDoor.localPosition,
            rightOpenPos,
            slideSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryOpen(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryOpen(collision.collider);
    }

    public void TryOpen(Collider other)
    {
        if (other == null || !other.CompareTag("Shoben"))
            return;

        isOpening = true;
        Debug.Log("小便が当たり、ドアを開きます", this);
    }
}

