using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class PlaneHitFollower : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private CameraRaycaster cameraRaycaster;
    [SerializeField] private Vector3 positionOffset;

    [Header("Radius")]
    [SerializeField] private float secondStageTime = 0.5f;
    [SerializeField] private float thirdStageTime = 1.5f;
    [SerializeField] private float firstStageMultiplier = 1.25f;
    [SerializeField] private float secondStageMultiplier = 1.5f;
    [SerializeField] private float thirdStageMultiplier = 2f;

    private CapsuleCollider capsuleCollider;
    private float initialRadius;
    private float clickDuration;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        initialRadius = capsuleCollider.radius;

        if (cameraRaycaster == null)
        {
            cameraRaycaster = FindAnyObjectByType<CameraRaycaster>();
        }
    }

    private void Update()
    {
        MoveToPlaneHitPoint();
        UpdateColliderRadius();
    }

    private void MoveToPlaneHitPoint()
    {
        if (cameraRaycaster == null)
        {
            return;
        }

        if (cameraRaycaster.TryGetPlaneHitPoint(out Vector3 hitPoint))
        {
            transform.position = hitPoint + positionOffset;
        }
    }

    private void UpdateColliderRadius()
    {
        if (Input.GetMouseButton(0))
        {
            clickDuration += Time.deltaTime;

            if (clickDuration >= thirdStageTime)
            {
                capsuleCollider.radius = initialRadius * thirdStageMultiplier;
            }
            else if (clickDuration >= secondStageTime)
            {
                capsuleCollider.radius = initialRadius * secondStageMultiplier;
            }
            else
            {
                capsuleCollider.radius = initialRadius * firstStageMultiplier;
            }

            return;
        }

        clickDuration = 0f;
        capsuleCollider.radius = initialRadius;
    }
}
