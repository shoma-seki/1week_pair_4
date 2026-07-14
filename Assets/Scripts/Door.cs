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
    private bool openingEffectPlayed;

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
        if (!openingEffectPlayed)
        {
            openingEffectPlayed = true;
            PresentationDirector director = PresentationDirector.Instance;
            if (director != null)
            {
                director.ShowBanner("OPEN!", new Color(0.35f, 0.9f, 1f), 0.7f);
                director.Flash(new Color(0.3f, 0.8f, 1f, 0.22f), 0.45f);
                director.SpawnBurst(transform.position + Vector3.up * 2f, new Color(0.3f, 0.85f, 1f), 30, 0.45f);
                director.PlayTone(740f, 0.24f, 0.2f);
                director.ShakeCamera(0.08f, 0.3f);
            }
        }
        Debug.Log("小便が当たり、ドアを開きます", this);
    }
}

