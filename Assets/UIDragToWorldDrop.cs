using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIDragToWorldDrop : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Canvas")]
    public Canvas canvas;
    public Camera worldCamera;

    [Header("Correct Drop Target")]
    public Collider targetCollider;

    [Header("Return Animation")]
    public float returnSpeed = 10f;

    [Header("After Correct Drop")]
    public bool hideAfterSuccess = true;

    [Header("Preview Object (Optional)")]
    public GameObject objectWhileDragging;

    [Header("Preview Blink")]
    public bool blinkPreview = true;
    public float blinkSpeed = 5f;

    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("Snap Sound")]
    public AudioSource audioSource;
    public AudioClip snapAudioClip;

    [Header("Events")]
    public UnityEvent onCorrectDrop;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image previewImage;

    private Vector2 originalAnchoredPosition;

    private bool droppedCorrectly = false;
    private bool isDragging = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (objectWhileDragging != null)
        {
            previewImage = objectWhileDragging.GetComponent<Image>();

            if (previewImage == null)
                previewImage = objectWhileDragging.GetComponentInChildren<Image>();

            objectWhileDragging.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDragging &&
            blinkPreview &&
            objectWhileDragging != null &&
            objectWhileDragging.activeSelf &&
            previewImage != null)
        {
            Color c = previewImage.color;
            c.a = Mathf.Lerp(
                minAlpha,
                maxAlpha,
                Mathf.PingPong(Time.time * blinkSpeed, 1f));

            previewImage.color = c;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (droppedCorrectly)
            return;

        isDragging = true;

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.8f;

        if (objectWhileDragging != null)
            objectWhileDragging.SetActive(true);

        Debug.Log("Drag Started");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (droppedCorrectly)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (droppedCorrectly)
            return;

        isDragging = false;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        if (objectWhileDragging != null)
            objectWhileDragging.SetActive(false);

        if (previewImage != null)
        {
            Color c = previewImage.color;
            c.a = 1f;
            previewImage.color = c;
        }

        Ray ray = worldCamera.ScreenPointToRay(eventData.position);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Dropped On : " + hit.collider.name);

            if (hit.collider == targetCollider)
            {
                droppedCorrectly = true;

                Debug.Log("Correct Drop!");

                // Play Snap Sound
                if (audioSource != null && snapAudioClip != null)
                {
                    audioSource.PlayOneShot(snapAudioClip);
                }

                onCorrectDrop?.Invoke();

                if (hideAfterSuccess)
                    gameObject.SetActive(false);

                return;
            }
        }

        Debug.Log("Wrong Drop! Returning To Original Position.");

        StopAllCoroutines();
        StartCoroutine(ReturnToOrigin());
    }

    private IEnumerator ReturnToOrigin()
    {
        while (Vector2.Distance(rectTransform.anchoredPosition, originalAnchoredPosition) > 0.1f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                originalAnchoredPosition,
                Time.deltaTime * returnSpeed);

            yield return null;
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;
    }
}