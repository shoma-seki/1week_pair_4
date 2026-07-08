using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRaycaster : MonoBehaviour
{
    private const string PlaneTag = "Plane";

    [SerializeField] private float maxDistance = 1000f;

    [Header("Aim Speed By Urine Stage")]
    [SerializeField] private Player player;

    private Camera targetCamera;
    private Vector2 aimScreenPosition;
    private bool aimPositionInitialized;

    public bool HasPlaneHit { get; private set; }
    public Vector3 PlaneHitPoint { get; private set; }
    public Vector2 AimScreenPosition => aimPositionInitialized
        ? aimScreenPosition
        : (Vector2)Input.mousePosition;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }
    }

    private void Update()
    {
        UpdateAimScreenPosition();
        HasPlaneHit = TryGetPlaneHitPoint(out Vector3 hitPoint);

        if (HasPlaneHit)
        {
            PlaneHitPoint = hitPoint;
        }
    }

    public bool TryGetPlaneHitPoint(out Vector3 hitPoint)
    {
        hitPoint = default;

        Ray ray = targetCamera.ScreenPointToRay(AimScreenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance);
        float nearestDistance = float.PositiveInfinity;
        bool foundPlane = false;

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.CompareTag(PlaneTag) || hit.distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = hit.distance;
            hitPoint = hit.point;
            foundPlane = true;
        }

        return foundPlane;
    }

    private void UpdateAimScreenPosition()
    {
        Vector2 currentMousePosition = Input.mousePosition;

        if (!aimPositionInitialized)
        {
            aimScreenPosition = currentMousePosition;
            aimPositionInitialized = true;
            return;
        }

        float speedMultiplier = GetAimSpeedMultiplier();
        if (Mathf.Approximately(speedMultiplier, 1f))
        {
            aimScreenPosition = currentMousePosition;
        }
        else
        {
            // Always move toward the actual mouse position while slowing the aim by stage.
            float followRate = 60f * speedMultiplier;
            float followAmount = 1f - Mathf.Exp(-followRate * Time.unscaledDeltaTime);
            aimScreenPosition = Vector2.Lerp(aimScreenPosition, currentMousePosition, followAmount);
        }

        aimScreenPosition.x = Mathf.Clamp(aimScreenPosition.x, 0f, Screen.width);
        aimScreenPosition.y = Mathf.Clamp(aimScreenPosition.y, 0f, Screen.height);
    }

    private float GetAimSpeedMultiplier()
    {
        return player != null ? player.CurrentAimSpeedMultiplier : 1f;
    }
}
