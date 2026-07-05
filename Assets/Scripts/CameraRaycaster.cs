using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRaycaster : MonoBehaviour
{
    [SerializeField] private Collider planeCollider;
    [SerializeField] private float maxDistance = 1000f;

    private Camera targetCamera;

    public bool HasPlaneHit { get; private set; }
    public Vector3 PlaneHitPoint { get; private set; }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        HasPlaneHit = TryGetPlaneHitPoint(out Vector3 hitPoint);

        if (HasPlaneHit)
        {
            PlaneHitPoint = hitPoint;
        }
    }

    public bool TryGetPlaneHitPoint(out Vector3 hitPoint)
    {
        hitPoint = default;

        if (planeCollider == null)
        {
            return false;
        }

        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

        if (!planeCollider.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            return false;
        }

        hitPoint = hit.point;
        return true;
    }
}
