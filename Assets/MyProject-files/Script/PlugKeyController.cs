using UnityEngine;
using UnityEngine.Events;

public class PlugKeyController : MonoBehaviour
{
    [Header("Click Settings")]
    [Tooltip("Only objects with this tag will trigger the click/toggle action.")]
    public string clickableTag = "Untagged";

    [Tooltip("If FALSE, user clicks will be ignored until a function unlocks movement.")]
    public bool allowClickToMove = false;

    [Header("Plug Positions")]
    public Transform plugInPosition;
    public Transform plugOutPosition;

    [Header("Movement Settings")]
    [Tooltip("How long the plug takes to fully move in or out (seconds).")]
    public float moveDuration = 0.5f;

    [Tooltip("Shapes the easing of the movement. Default: smooth ease-in/ease-out.")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Initial State")]
    [Tooltip("Check this if the plug should start already plugged in.")]
    public bool startPluggedIn = true;

    [Header("Events")]
    public UnityEvent onPlugIn;
    public UnityEvent onPlugOut;

    private bool isPluggedIn;
    private Transform targetTransform;

    private Vector3 startPos;
    private Quaternion startRot;

    private bool isMoving;
    private float moveTimer;

    private void Start()
    {
        if (plugInPosition == null || plugOutPosition == null)
        {
            Debug.LogWarning("PlugKeyController: Plug In/Out positions not assigned.");
            return;
        }

        isPluggedIn = startPluggedIn;

        // Snap instantly to the correct starting position/rotation without firing events on load
        targetTransform = isPluggedIn ? plugInPosition : plugOutPosition;
        transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
    }

    private void Update()
    {
        if (!isMoving || targetTransform == null)
            return;

        moveTimer += Time.deltaTime;

        float t = moveDuration > 0f ? Mathf.Clamp01(moveTimer / moveDuration) : 1f;
        float eased = moveCurve.Evaluate(t);

        transform.position = Vector3.Lerp(startPos, targetTransform.position, eased);
        transform.rotation = Quaternion.Slerp(startRot, targetTransform.rotation, eased);

        if (t >= 1f)
        {
            transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
            isMoving = false;

            if (isPluggedIn)
                onPlugIn?.Invoke();
            else
                onPlugOut?.Invoke();
        }
    }

    // Called automatically when the user clicks/taps this object's Collider
    private void OnMouseDown()
    {
        // Block movement if click-to-move is disabled or tag doesn't match
        if (!allowClickToMove)
            return;

        if (!string.IsNullOrEmpty(clickableTag) && !CompareTag(clickableTag))
            return;

        TogglePlug();
    }

    /// <summary>
    /// Call this function from external scripts/events to enable user clicks.
    /// </summary>
    public void UnlockMovement()
    {
        allowClickToMove = true;
    }

    /// <summary>
    /// Call this function from external scripts/events to disable user clicks.
    /// </summary>
    public void LockMovement()
    {
        allowClickToMove = false;
    }

    public void TogglePlug()
    {
        if (isPluggedIn)
            PlugOut();
        else
            PlugIn();
    }

    // =========================================================================
    // INSTANT SNAP FUNCTIONS (NO ANIMATION - TRANSFORMS CHANGE IMMEDIATELY)
    // =========================================================================

    /// <summary>
    /// Instantly moves and rotates the object to plugInPosition without animation.
    /// </summary>
    public void SnapPlugIn()
    {
        if (plugInPosition == null) return;

        isMoving = false;
        isPluggedIn = true;
        targetTransform = plugInPosition;

        transform.SetPositionAndRotation(plugInPosition.position, plugInPosition.rotation);
        onPlugIn?.Invoke();
    }

    /// <summary>
    /// Instantly moves and rotates the object to plugOutPosition without animation.
    /// </summary>
    public void SnapPlugOut()
    {
        if (plugOutPosition == null) return;

        isMoving = false;
        isPluggedIn = false;
        targetTransform = plugOutPosition;

        transform.SetPositionAndRotation(plugOutPosition.position, plugOutPosition.rotation);
        onPlugOut?.Invoke();
    }

    // =========================================================================
    // ANIMATED MOVEMENT FUNCTIONS
    // =========================================================================

    public void PlugIn()
    {
        if (isPluggedIn || plugInPosition == null)
            return;

        isPluggedIn = true;
        BeginMove(plugInPosition);
    }

    public void PlugOut()
    {
        if (!isPluggedIn || plugOutPosition == null)
            return;

        isPluggedIn = false;
        BeginMove(plugOutPosition);
    }

    private void BeginMove(Transform target)
    {
        targetTransform = target;
        startPos = transform.position;
        startRot = transform.rotation;
        moveTimer = 0f;
        isMoving = true;
    }

    // Query current state from other scripts
    public bool IsPluggedIn()
    {
        return isPluggedIn;
    }
}