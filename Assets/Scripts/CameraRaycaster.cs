using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraRaycaster : MonoBehaviour
{
    [SerializeField] private Collider planeCollider;
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

        if (planeCollider == null)
        {
            return false;
        }

        Ray ray = targetCamera.ScreenPointToRay(AimScreenPosition);

        if (!planeCollider.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            return false;
        }

        hitPoint = hit.point;
        return true;
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
