using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Door door;

    private void Awake()
    {
        if (door == null)
            door = GetComponentInParent<Door>();

        GetComponent<Collider>().isTrigger = true;
    }

    public void Initialize(Door targetDoor)
    {
        door = targetDoor;
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        door?.TryOpen(other);
    }
}
