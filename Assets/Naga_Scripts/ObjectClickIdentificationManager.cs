using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ObjectClickIdentificationManager : MonoBehaviour
{
    [System.Serializable]
    public class IdentificationTarget
    {
        [Header("Target Identification Item")]
        public string itemName = "Rheostat";

        [Tooltip("The 3D clickable object in the scene (Must have a Collider).")]
        public Collider clickableCollider;

        [Header("UI Elements to Enable")]
        [Tooltip("UI Image component in Screen/Overlay Canvas (e.g., Checkmark UI).")]
        public Image overlayCheckImage;

        [Tooltip("Text UI component (TMP_Text) to reveal upon correct click.")]
        public TMP_Text labelText;
    }

    [Header("Camera Configuration")]
    [SerializeField] private Camera mainCamera;

    [Header("Correct Clickable Items Setup")]
    [SerializeField] private List<IdentificationTarget> correctTargets = new List<IdentificationTarget>();

    [Header("Wrong Clickable Items Setup")]
    [Tooltip("Distractor objects in the scene. Clicking these triggers OnWrongObjectClicked, but they are NOT required to complete the page.")]
    [SerializeField] private List<Collider> wrongColliders = new List<Collider>();

    [Header("World Space Canvas & Prefabs (Pop-Up Effect)")]
    [Tooltip("Parent Canvas for spawned World-Space Checkmarks (Assign your World-Space Canvas here).")]
    [SerializeField] private Transform worldSpaceCanvasParent;

    [Tooltip("Prefab to spawn over correct object when clicked.")]
    [SerializeField] private GameObject correctWorldSpacePrefab;

    [Tooltip("Prefab to spawn over wrong object when clicked.")]
    [SerializeField] private GameObject wrongWorldSpacePrefab;

    [Tooltip("Vertical offset distance above the object's top Y bounds to spawn the prefab.")]
    [SerializeField] private float yOffsetDistance = 0.05f;

    [Tooltip("Adjust this to scale down oversized prefabs in 3D space (e.g., 0.005 or 0.01).")]
    [SerializeField] private Vector3 spawnScaleMultiplier = new Vector3(0.005f, 0.005f, 0.005f);

    [Header("Completion Settings")]
    [Tooltip("Canvas reference (kept for configuration context).")]
    [SerializeField] private Canvas targetCanvas;

    [Header("Events")]
    [Tooltip("Triggered when a CORRECT object is clicked.")]
    public UnityEvent OnCorrectObjectClicked;

    [Tooltip("Triggered when a WRONG object is clicked.")]
    public UnityEvent OnWrongObjectClicked;

    [Tooltip("Triggered when ALL required correct objects have been successfully identified.")]
    public UnityEvent OnAllObjectsCompleted;

    private readonly HashSet<int> completedIndices = new HashSet<int>();
    private readonly HashSet<Collider> spawnedWrongColliders = new HashSet<Collider>();

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        InitializeUIState();
    }

    private void Update()
    {
        // Detect click or touch input
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick(Input.mousePosition);
        }
    }

    /// <summary>
    /// Hides all overlay checkmark images and text elements on scene start.
    /// </summary>
    private void InitializeUIState()
    {
        foreach (var target in correctTargets)
        {
            if (target.overlayCheckImage != null)
                target.overlayCheckImage.enabled = false;

            if (target.labelText != null)
                target.labelText.enabled = false;
        }
    }

    private void HandleClick(Vector3 screenPoint)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPoint);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int matchedIndex = GetMatchingCorrectTargetIndex(hit.collider);

            if (matchedIndex != -1)
            {
                // Already identified correct item clicked again
                if (completedIndices.Contains(matchedIndex))
                    return;

                // --- CORRECT CLICK ---
                ProcessCorrectClick(matchedIndex, hit.collider);
            }
            else
            {
                // --- WRONG CLICK (Hit wrong object or non-target collider) ---
                ProcessWrongClick(hit.collider);
            }
        }
        else
        {
            // --- CLICKED EMPTY SPACE ---
            OnWrongObjectClicked?.Invoke();
        }
    }

    private int GetMatchingCorrectTargetIndex(Collider hitCollider)
    {
        for (int i = 0; i < correctTargets.Count; i++)
        {
            if (correctTargets[i].clickableCollider == null) continue;

            // Matches collider directly or as a child in hierarchy
            if (correctTargets[i].clickableCollider == hitCollider || hitCollider.transform.IsChildOf(correctTargets[i].clickableCollider.transform))
            {
                return i;
            }
        }

        return -1;
    }

    private void ProcessCorrectClick(int index, Collider hitCollider)
    {
        IdentificationTarget target = correctTargets[index];
        completedIndices.Add(index);

        // Spawn world-space correct prefab inside canvas above object Y bounds
        if (correctWorldSpacePrefab != null && hitCollider != null)
        {
            Vector3 spawnPos = CalculateTopPosition(hitCollider);
            SpawnPopUpEffect(correctWorldSpacePrefab, spawnPos);
        }

        // Enable ONLY Overlay Image component (keeps GameObject active state unchanged)
        if (target.overlayCheckImage != null)
            target.overlayCheckImage.enabled = true;

        // Enable label text
        if (target.labelText != null)
            target.labelText.enabled = true;

        Debug.Log($"[Identification] Correct Object Clicked: {target.itemName}");
        OnCorrectObjectClicked?.Invoke();

        // Check if all CORRECT objects are completed (Ignores wrong objects completely)
        if (completedIndices.Count >= correctTargets.Count)
        {
            Debug.Log("[Identification] All correct items identified successfully!");
            OnAllObjectsCompleted?.Invoke();
        }
    }

    private void ProcessWrongClick(Collider hitCollider)
    {
        if (hitCollider != null && wrongWorldSpacePrefab != null)
        {
            // Check if the hit collider is actually in the wrongColliders list
            if (wrongColliders.Contains(hitCollider))
            {
                if (!spawnedWrongColliders.Contains(hitCollider))
                {
                    spawnedWrongColliders.Add(hitCollider);
                    Vector3 spawnPos = CalculateTopPosition(hitCollider);
                    SpawnPopUpEffect(wrongWorldSpacePrefab, spawnPos);
                }
            }
        }

        OnWrongObjectClicked?.Invoke();
    }

    // =========================================================================
    // Y-AXIS POSITION & POP-UP EFFECT HELPERS
    // =========================================================================

    private Vector3 CalculateTopPosition(Collider col)
    {
        Bounds bounds = col.bounds;
        // Top Y position of the collider bounds + offset
        Vector3 topPos = new Vector3(bounds.center.x, bounds.max.y + yOffsetDistance, bounds.center.z);
        return topPos;
    }

    private void SpawnPopUpEffect(GameObject prefab, Vector3 worldPosition)
    {
        // Instantiate under worldSpaceCanvasParent (or as root if not set)
        GameObject instance = (worldSpaceCanvasParent != null)
            ? Instantiate(prefab, worldSpaceCanvasParent)
            : Instantiate(prefab);

        // Place directly in World Space coordinates
        instance.transform.position = worldPosition;

        // Billboard toward Main Camera
        if (mainCamera != null)
        {
            instance.transform.rotation = Quaternion.LookRotation(instance.transform.position - mainCamera.transform.position);
        }

        // Trigger scale animation cleanly within Canvas bounds
        StartCoroutine(PopUpAnimation(instance.transform));
    }

    private IEnumerator PopUpAnimation(Transform targetTransform)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        Vector3 targetScale = Vector3.Scale(targetTransform.localScale, spawnScaleMultiplier);
        targetTransform.localScale = Vector3.zero;

        while (elapsed < duration)
        {
            if (targetTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth pop-up scale lerp
            targetTransform.localScale = Vector3.Lerp(Vector3.zero, targetScale, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        if (targetTransform != null)
            targetTransform.localScale = targetScale;
    }
}