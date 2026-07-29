using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;

public class ObjectPlacementDragHandler : MonoBehaviour
{
    public Camera sceneCamera;

    [Header("UI Images (Order Matters)")]
    public List<RectTransform> draggableUiElements;

    [Header("Target Objects (Hierarchy with Multiple Mesh Renderers)")]
    [Tooltip("The 3D target objects. Can contain multiple child MeshRenderers.")]
    public List<GameObject> placementTargets;

    [Header("Additive Highlight Material")]
    [Tooltip("Material appended to all MeshRenderers on the active target object while dragging.")]
    public Material overlayHighlightMaterial;

    [Header("UI Parent To Disable When Snapping Is Complete")]
    [Tooltip("Specific UI parent GameObject (e.g. Panel/Group) to turn off when snapping completes (Not the whole Canvas).")]
    public GameObject uiContainerToHide;

    [Header("Placement Events")]
    [Tooltip("Triggered every time a single object is successfully placed.")]
    public UnityEvent OnItemPlaced;

    [Tooltip("Triggered when ALL target objects have been successfully placed.")]
    public UnityEvent OnAllItemsPlaced;

    private int activeIndex = -1;

    // Dictionary tracking all MeshRenderers on the active target and their original materials
    private readonly Dictionary<Renderer, Material[]> cachedOriginalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly HashSet<int> completedTargetIndices = new HashSet<int>();

    private void Start()
    {
        if (sceneCamera == null)
            sceneCamera = Camera.main;

        // Hide all target objects initially
        foreach (var target in placementTargets)
        {
            if (target != null)
                target.SetActive(false);
        }
    }

    public void OnStartDragging(RectTransform sourceUiElement)
    {
        activeIndex = draggableUiElements.IndexOf(sourceUiElement);

        if (activeIndex < 0)
        {
            Debug.LogError("[ObjectPlacementDragHandler] UI element not found in list.", this);
            return;
        }

        // Clean up any existing active highlights
        ClearActiveHighlights();

        for (int i = 0; i < placementTargets.Count; i++)
        {
            if (placementTargets[i] == null) continue;

            // Do not modify already placed objects
            if (completedTargetIndices.Contains(i)) continue;

            bool isCurrentTarget = (i == activeIndex);

            if (isCurrentTarget)
            {
                // 1. Unhide the target object while dragging
                placementTargets[i].SetActive(true);

                // 2. Find ALL MeshRenderers in child hierarchy and apply additive highlight
                MeshRenderer[] childMeshRenderers = placementTargets[i].GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer meshRend in childMeshRenderers)
                {
                    AttachHighlightMaterial(meshRend);
                }

                // Also check for SkinnedMeshRenderers in case of rigged models
                SkinnedMeshRenderer[] childSkinnedRenderers = placementTargets[i].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach (SkinnedMeshRenderer skinnedRend in childSkinnedRenderers)
                {
                    AttachHighlightMaterial(skinnedRend);
                }
            }
            else
            {
                // Keep other unplaced target objects hidden
                placementTargets[i].SetActive(false);
            }
        }
    }

    public bool OnAttemptDrop(Vector2 pointerScreenPosition)
    {
        Ray ray = sceneCamera.ScreenPointToRay(pointerScreenPosition);

        if (Physics.Raycast(ray, out RaycastHit raycastHit))
        {
            int hitTargetIndex = ResolveTargetIndexFromCollider(raycastHit.collider);

            if (hitTargetIndex == activeIndex && hitTargetIndex != -1)
            {
                // 1. Strip highlight materials from all MeshRenderers
                ClearActiveHighlights();

                // 2. Mark as successfully placed (Stays unhidden forever)
                completedTargetIndices.Add(hitTargetIndex);
                placementTargets[hitTargetIndex].SetActive(true);

                Debug.Log("Target Object Placed Permanently: " + placementTargets[hitTargetIndex].name, placementTargets[hitTargetIndex]);
                OnItemPlaced?.Invoke();

                activeIndex = -1;

                // 3. Check if all items are completed
                EvaluateAllTargetsPlaced();

                return true;
            }
        }

        // Failed drop - remove highlight materials from all child MeshRenderers and hide the object
        ClearActiveHighlights();

        if (activeIndex >= 0 && activeIndex < placementTargets.Count && placementTargets[activeIndex] != null)
        {
            if (!completedTargetIndices.Contains(activeIndex))
            {
                placementTargets[activeIndex].SetActive(false);
            }
        }

        activeIndex = -1;
        return false;
    }

    private int ResolveTargetIndexFromCollider(Collider targetCollider)
    {
        if (targetCollider == null) return -1;

        for (int i = 0; i < placementTargets.Count; i++)
        {
            if (placementTargets[i] == null) continue;

            // Checks if hit collider belongs to target object or any child mesh
            if (placementTargets[i] == targetCollider.gameObject || targetCollider.transform.IsChildOf(placementTargets[i].transform))
            {
                return i;
            }
        }

        return -1;
    }

    private void EvaluateAllTargetsPlaced()
    {
        if (completedTargetIndices.Count >= placementTargets.Count)
        {
            // ✅ All objects placed
            OnAllItemsPlaced?.Invoke();

            // Disable only the specific parent UI object (Panel/Group) instead of the whole Canvas
            if (uiContainerToHide != null)
                uiContainerToHide.SetActive(false);
        }
    }

    // =========================================================================
    // MULTI MESH RENDERER ADDITIVE HIGHLIGHT LOGIC
    // =========================================================================

    private void AttachHighlightMaterial(Renderer targetRenderer)
    {
        if (targetRenderer == null || overlayHighlightMaterial == null) return;

        Material[] currentMaterials = targetRenderer.sharedMaterials;

        // Save original materials for clean removal later
        if (!cachedOriginalMaterials.ContainsKey(targetRenderer))
        {
            cachedOriginalMaterials.Add(targetRenderer, currentMaterials);
        }

        // Avoid adding duplicate highlight slots
        foreach (Material mat in currentMaterials)
        {
            if (mat == overlayHighlightMaterial) return;
        }

        // Build new array containing base sub-mesh materials + 1 highlight slot
        Material[] expandedMaterials = new Material[currentMaterials.Length + 1];
        for (int i = 0; i < currentMaterials.Length; i++)
        {
            expandedMaterials[i] = currentMaterials[i];
        }
        expandedMaterials[expandedMaterials.Length - 1] = overlayHighlightMaterial;

        targetRenderer.sharedMaterials = expandedMaterials;
    }

    private void ClearActiveHighlights()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry in cachedOriginalMaterials)
        {
            if (entry.Key != null)
            {
                // Restore exact original material array for each child mesh
                entry.Key.sharedMaterials = entry.Value;
            }
        }

        cachedOriginalMaterials.Clear();
    }
}
