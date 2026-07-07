using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageMouseFollower : MonoBehaviour
{
    [SerializeField] private CameraRaycaster cameraRaycaster;

    private RectTransform imageRectTransform;
    private RectTransform parentRectTransform;
    private Canvas parentCanvas;

    private void Awake()
    {
        imageRectTransform = GetComponent<RectTransform>();
        parentRectTransform = imageRectTransform.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        if (cameraRaycaster == null)
        {
            cameraRaycaster = FindAnyObjectByType<CameraRaycaster>();
        }
    }

    private void Update()
    {
        if (parentRectTransform == null || parentCanvas == null)
        {
            return;
        }

        Camera eventCamera = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : parentCanvas.worldCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRectTransform,
                cameraRaycaster != null
                    ? cameraRaycaster.AimScreenPosition
                    : (Vector2)Input.mousePosition,
                eventCamera,
                out Vector2 localPoint))
        {
            imageRectTransform.localPosition = localPoint;
        }
    }
}
