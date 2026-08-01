using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Drives a 3D rheostat handle using World Space coordinates.
/// Ignores parent scaling issues (like scale 81.1898).
/// </summary>
public class RheostatController : MonoBehaviour
{
    public enum MovementAxis { WorldX, WorldY, WorldZ }

    [Header("Handle")]
    [SerializeField] private Transform handle;
    [SerializeField] private Collider handleCollider;

    [Header("Axis Configuration")]
    [Tooltip("Which world axis does the slider move along? Try WorldX first.")]
    [SerializeField] private MovementAxis axis = MovementAxis.WorldX;

    [Header("Travel Limits")]
    [SerializeField] private Transform minLimitPoint;
    [SerializeField] private Transform maxLimitPoint;

    [Header("Input Settings")]
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private LayerMask handleLayerMask = ~0;
    [SerializeField] private bool ignoreUIBlocking = true;

    [Header("Snap / Detection")]
    [Tooltip("Distance in world units to trigger 'near' detection (e.g. 0.05 to 0.2)")]
    [SerializeField] private float snapThreshold = 0.1f;

    public System.Action<float> OnValueChanged;

    private bool canInteract = true;
    private bool isDragging = false;
    private float dragOffsetWorld;

    public float MinWorldVal => (minLimitPoint != null && maxLimitPoint != null)
        ? Mathf.Min(GetWorldAxisValue(minLimitPoint.position), GetWorldAxisValue(maxLimitPoint.position))
        : 0f;

    public float MaxWorldVal => (minLimitPoint != null && maxLimitPoint != null)
        ? Mathf.Max(GetWorldAxisValue(minLimitPoint.position), GetWorldAxisValue(maxLimitPoint.position))
        : 0f;

    public float NormalizedValue { get; private set; }

    private void Awake()
    {
        if (handle == null)
            handle = transform;

        if (handleCollider == null)
            handleCollider = handle.GetComponent<Collider>();

        if (raycastCamera == null)
            raycastCamera = Camera.main;

        ClampHandleToBounds();
    }

    private void Start()
    {
        UpdateNormalizedValue();
    }

    private void Update()
    {
        if (!canInteract)
            return;

        var activeTouch = GetActiveTouch();

        if (activeTouch.HasValue)
            HandleTouch(activeTouch.Value);
        else
            HandleMouse();
    }

    private float GetWorldAxisValue(Vector3 worldPos)
    {
        return axis switch
        {
            MovementAxis.WorldY => worldPos.y,
            MovementAxis.WorldZ => worldPos.z,
            _ => worldPos.x
        };
    }

    private Vector3 SetWorldAxisValue(Vector3 worldPos, float value)
    {
        switch (axis)
        {
            case MovementAxis.WorldY: worldPos.y = value; break;
            case MovementAxis.WorldZ: worldPos.z = value; break;
            default: worldPos.x = value; break;
        }
        return worldPos;
    }

    public float GetNormalizedValueForTransform(Transform target)
    {
        if (target == null)
            return 0f;

        float worldVal = GetWorldAxisValue(target.position);
        float range = MaxWorldVal - MinWorldVal;
        return range > 0f ? Mathf.InverseLerp(MinWorldVal, MaxWorldVal, worldVal) : 0f;
    }

    /// <summary>
    /// Sets the handle position directly using a normalized value (0 to 1).
    /// Called during AutoFill operations.
    /// </summary>
    public void SetNormalizedValue(float targetNorm)
    {
        targetNorm = Mathf.Clamp01(targetNorm);
        float targetWorldVal = Mathf.Lerp(MinWorldVal, MaxWorldVal, targetNorm);
        handle.position = SetWorldAxisValue(handle.position, targetWorldVal);
        UpdateNormalizedValue();
    }

    private TouchState? GetActiveTouch()
    {
        if (Touchscreen.current == null)
            return null;

        foreach (var touch in Touchscreen.current.touches)
        {
            var phase = touch.phase.ReadValue();
            if (phase == UnityEngine.InputSystem.TouchPhase.None)
                continue;

            return new TouchState
            {
                touchId = touch.touchId.ReadValue(),
                phase = phase,
                position = touch.position.ReadValue()
            };
        }

        return null;
    }

    private struct TouchState
    {
        public int touchId;
        public UnityEngine.InputSystem.TouchPhase phase;
        public Vector2 position;
    }

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (ignoreUIBlocking || !IsPointerOverUI(-1))
                TryBeginDrag(mousePos);
        }
        else if (Mouse.current.leftButton.isPressed && isDragging)
        {
            ContinueDrag(mousePos);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            EndDrag();
        }
    }

    private void HandleTouch(TouchState touch)
    {
        switch (touch.phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                if (ignoreUIBlocking || !IsPointerOverUI(touch.touchId))
                    TryBeginDrag(touch.position);
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:
            case UnityEngine.InputSystem.TouchPhase.Stationary:
                if (isDragging)
                    ContinueDrag(touch.position);
                break;

            case UnityEngine.InputSystem.TouchPhase.Ended:
            case UnityEngine.InputSystem.TouchPhase.Canceled:
                EndDrag();
                break;
        }
    }

    private bool IsPointerOverUI(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        return pointerId < 0
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private void TryBeginDrag(Vector2 screenPosition)
    {
        if (raycastCamera == null || handleCollider == null)
            return;

        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, handleLayerMask) && hit.collider == handleCollider)
        {
            isDragging = true;
            float pointerWorldVal = GetWorldPointFromScreen(screenPosition);
            dragOffsetWorld = GetWorldAxisValue(handle.position) - pointerWorldVal;
        }
    }

    private void ContinueDrag(Vector2 screenPosition)
    {
        float pointerWorldVal = GetWorldPointFromScreen(screenPosition);
        float targetWorldVal = pointerWorldVal + dragOffsetWorld;

        float minWorld = MinWorldVal;
        float maxWorld = MaxWorldVal;

        float clampedWorldVal = Mathf.Clamp(targetWorldVal, minWorld, maxWorld);

        handle.position = SetWorldAxisValue(handle.position, clampedWorldVal);
        UpdateNormalizedValue();
    }

    private void EndDrag()
    {
        isDragging = false;
    }

    private float GetWorldPointFromScreen(Vector2 screenPosition)
    {
        Ray ray = raycastCamera.ScreenPointToRay(screenPosition);
        Plane plane = new Plane(-raycastCamera.transform.forward, handle.position);

        if (plane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            return GetWorldAxisValue(worldPoint);
        }

        return GetWorldAxisValue(handle.position);
    }

    private void UpdateNormalizedValue()
    {
        float range = MaxWorldVal - MinWorldVal;
        float current = GetWorldAxisValue(handle.position);
        NormalizedValue = range > 0f ? Mathf.InverseLerp(MinWorldVal, MaxWorldVal, current) : 0f;
        OnValueChanged?.Invoke(NormalizedValue);
    }

    private void ClampHandleToBounds()
    {
        float current = GetWorldAxisValue(handle.position);
        float clamped = Mathf.Clamp(current, MinWorldVal, MaxWorldVal);
        handle.position = SetWorldAxisValue(handle.position, clamped);
    }

    public bool IsHandleNear(Transform target)
    {
        return IsHandleNear(target, snapThreshold);
    }

    public bool IsHandleNear(Transform target, float threshold)
    {
        if (target == null)
            return false;

        return Vector3.Distance(handle.position, target.position) <= threshold;
    }

    public void SetInteraction(bool enable)
    {
        canInteract = enable;
        if (!enable)
            isDragging = false;
    }

    public void SnapTo(Transform target)
    {
        if (target == null)
            return;

        float targetVal = Mathf.Clamp(GetWorldAxisValue(target.position), MinWorldVal, MaxWorldVal);
        handle.position = SetWorldAxisValue(handle.position, targetVal);
        UpdateNormalizedValue();
    }

    private void OnDrawGizmosSelected()
    {
        if (minLimitPoint == null || maxLimitPoint == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(minLimitPoint.position, 0.05f);
        Gizmos.DrawWireSphere(maxLimitPoint.position, 0.05f);
        Gizmos.DrawLine(minLimitPoint.position, maxLimitPoint.position);
    }
}