using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableImageInitiator :
    MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("References")]
    public ObjectPlacementDragHandler dragManager;
    public Canvas parentCanvas;

    [Header("Return Animation")]
    [Tooltip("Duration for the image to return to its original position after an invalid drop.")]
    public float returnDuration = 0.25f;

    [Header("Scale Animation")]
    [Tooltip("Scale applied while dragging.")]
    public float dragScale = 1.2f;

    [Tooltip("Duration of the scale animation.")]
    public float scaleDuration = 0.15f;

    [Header("Drag Events")]
    [Tooltip("Invoked when dragging starts.")]
    public UnityEvent OnDragStart;

    [Tooltip("Invoked continuously while dragging.")]
    public UnityEvent OnDragging;

    [Tooltip("Invoked when dragging ends.")]
    public UnityEvent OnDragEnd;

    [Tooltip("Invoked only when the image is successfully placed.")]
    public UnityEvent OnPlacedSuccessfully;

    private RectTransform uiRectTransform;
    private CanvasGroup uiCanvasGroup;

    private Vector2 originalAnchoredPosition;
    private Vector2 pointerOffset;

    private Vector3 originalScale;

    private Coroutine activeScaleCoroutine;

    private void Awake()
    {
        uiRectTransform = GetComponent<RectTransform>();
        uiCanvasGroup = GetComponent<CanvasGroup>();

        originalScale = uiRectTransform.localScale;

        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();
    }

    // =========================================================================
    // DRAG OPERATIONS
    // =========================================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalAnchoredPosition = uiRectTransform.anchoredPosition;

        uiCanvasGroup.blocksRaycasts = false;

        BeginScaleAnimation(Vector3.one * dragScale);

        RectTransform parentRect = uiRectTransform.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition);

        pointerOffset = localPointerPosition - uiRectTransform.anchoredPosition;

        if (dragManager != null)
            dragManager.OnStartDragging(uiRectTransform);

        OnDragStart?.Invoke();
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransform parentRect = uiRectTransform.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition);

        uiRectTransform.anchoredPosition = localPointerPosition - pointerOffset;

        OnDragging?.Invoke();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        uiCanvasGroup.blocksRaycasts = true;

        bool placementSucceeded = false;

        if (dragManager != null)
            placementSucceeded = dragManager.OnAttemptDrop(eventData.position);

       if (placementSucceeded)
{
    Debug.Log($"[SUCCESS] '{gameObject.name}' was dropped on the correct target and hidden.", this);

    // Restore original scale
    uiRectTransform.localScale = originalScale;

    // Fire success event
    OnPlacedSuccessfully?.Invoke();

    // Hide dragged image
    gameObject.SetActive(false);
}
        else
        {
            StartCoroutine(AnimateReturnToOrigin());
            BeginScaleAnimation(originalScale);
        }

        OnDragEnd?.Invoke();
    }

    // =========================================================================
    // RETURN POSITION ANIMATION
    // =========================================================================

    private IEnumerator AnimateReturnToOrigin()
    {
        float elapsedTime = 0f;

        Vector2 startingPosition = uiRectTransform.anchoredPosition;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime / returnDuration;

            uiRectTransform.anchoredPosition = Vector2.Lerp(
                startingPosition,
                originalAnchoredPosition,
                elapsedTime);

            yield return null;
        }

        uiRectTransform.anchoredPosition = originalAnchoredPosition;
    }

    // =========================================================================
    // SCALE ANIMATION
    // =========================================================================

    private void BeginScaleAnimation(Vector3 destinationScale)
    {
        if (activeScaleCoroutine != null)
            StopCoroutine(activeScaleCoroutine);

        activeScaleCoroutine = StartCoroutine(
            AnimateScaleTransition(destinationScale));
    }

    private IEnumerator AnimateScaleTransition(Vector3 destinationScale)
    {
        float elapsedTime = 0f;

        Vector3 initialScale = uiRectTransform.localScale;

        while (elapsedTime < 1f)
        {
            elapsedTime += Time.deltaTime / scaleDuration;

            uiRectTransform.localScale = Vector3.Lerp(
                initialScale,
                destinationScale,
                elapsedTime);

            yield return null;
        }

        uiRectTransform.localScale = destinationScale;
    }
}