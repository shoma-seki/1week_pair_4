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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        leftClosedPos = leftDoor.localPosition;
        rightClosedPos = rightDoor.localPosition;

        //leftOpenPos=leftClosedPos+Vector3
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
