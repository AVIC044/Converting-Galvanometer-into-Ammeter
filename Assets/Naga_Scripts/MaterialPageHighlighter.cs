using System.Collections.Generic;
using UnityEngine;

public class PageMeshHighlightManager : MonoBehaviour
{
    [System.Serializable]
    public class MeshHighlightEntry
    {
        [Tooltip("The target GameObject or Renderer (MeshRenderer or SkinnedMeshRenderer) to highlight.")]
        [SerializeField] private GameObject targetObject;

        [Tooltip("Direct Renderer reference (optional, fallback if targetObject is null).")]
        [SerializeField] private Renderer meshRenderer;

        [Tooltip("Automatically highlight when this page opens.")]
        [SerializeField] private bool autoHighlightOnPageEnter = false;

        public GameObject TargetObject => targetObject;
        public Renderer MeshRenderer => meshRenderer;
        public bool AutoHighlightOnPageEnter => autoHighlightOnPageEnter;
    }

    [System.Serializable]
    public class PageHighlightConfig
    {
        [Header("Page Index")]
        public int pageIndex = 0;

        [Header("Mesh Renderers")]
        public List<MeshHighlightEntry> meshEntries = new List<MeshHighlightEntry>();
    }

    [Header("Highlight Material")]
    [SerializeField] private Material highlightMaterial;

    [Header("Page Configurations")]
    [SerializeField] private List<PageHighlightConfig> pageConfigs = new List<PageHighlightConfig>();

    private readonly HashSet<Renderer> highlightedRenderers = new HashSet<Renderer>();

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int currentPageIndex)
    {
        ClearAllHighlightsGlobal();

        PageHighlightConfig config = GetConfigByPageIndex(currentPageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            if (entry.AutoHighlightOnPageEnter)
            {
                ApplyHighlightToEntry(entry);
            }
        }
    }

    //==========================================================
    // UNITY EVENT HELPERS (CALL MULTIPLE TIMES FREELY)
    //==========================================================

    public void EnableElement0ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 0);
    public void DisableElement0ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 0);

    public void EnableElement1ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 1);
    public void DisableElement1ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 1);

    public void EnableElement2ByPageIndex(int pageIndex) => EnableElementHighlight(pageIndex, 2);
    public void DisableElement2ByPageIndex(int pageIndex) => DisableElementHighlight(pageIndex, 2);

    //==========================================================
    // PUBLIC FUNCTIONS
    //==========================================================

    public void EnableElementHighlight(int pageIndex, int elementIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        if (elementIndex < 0 || elementIndex >= config.meshEntries.Count)
            return;

        ApplyHighlightToEntry(config.meshEntries[elementIndex]);
    }

    public void DisableElementHighlight(int pageIndex, int elementIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        if (elementIndex < 0 || elementIndex >= config.meshEntries.Count)
            return;

        RemoveHighlightFromEntry(config.meshEntries[elementIndex]);
    }

    public void EnableAllHighlightsForPageIndex(int pageIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            ApplyHighlightToEntry(entry);
        }
    }

    public void DisableAllHighlightsForPageIndex(int pageIndex)
    {
        PageHighlightConfig config = GetConfigByPageIndex(pageIndex);

        if (config == null)
            return;

        foreach (MeshHighlightEntry entry in config.meshEntries)
        {
            RemoveHighlightFromEntry(entry);
        }
    }

    //==========================================================
    // HIGHLIGHT FUNCTIONS
    //==========================================================

    private void ApplyHighlightToEntry(MeshHighlightEntry entry)
    {
        if (entry == null) return;

        // If TargetObject is assigned, search all child renderers in hierarchy
        if (entry.TargetObject != null)
        {
            Renderer[] renderers = entry.TargetObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                ApplyHighlightMaterial(rend);
            }
        }
        else if (entry.MeshRenderer != null)
        {
            ApplyHighlightMaterial(entry.MeshRenderer);
        }
    }

    private void RemoveHighlightFromEntry(MeshHighlightEntry entry)
    {
        if (entry == null) return;

        if (entry.TargetObject != null)
        {
            Renderer[] renderers = entry.TargetObject.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                RemoveHighlightMaterial(rend);
            }
        }
        else if (entry.MeshRenderer != null)
        {
            RemoveHighlightMaterial(entry.MeshRenderer);
        }
    }

    private void ApplyHighlightMaterial(Renderer renderer)
    {
        if (renderer == null || highlightMaterial == null)
            return;

        Material[] mats = renderer.sharedMaterials;

        // Already highlighted check
        foreach (Material mat in mats)
        {
            if (mat == highlightMaterial)
            {
                highlightedRenderers.Add(renderer);
                return;
            }
        }

        List<Material> newMats = new List<Material>(mats);
        newMats.Add(highlightMaterial);

        renderer.sharedMaterials = newMats.ToArray();
        highlightedRenderers.Add(renderer);
    }

    private void RemoveHighlightMaterial(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] mats = renderer.sharedMaterials;
        List<Material> newMats = new List<Material>();

        foreach (Material mat in mats)
        {
            if (mat != highlightMaterial)
                newMats.Add(mat);
        }

        renderer.sharedMaterials = newMats.ToArray();
        highlightedRenderers.Remove(renderer);
    }

    private void ClearAllHighlightsGlobal()
    {
        // Clear currently tracked active highlights
        foreach (Renderer renderer in highlightedRenderers)
        {
            if (renderer == null)
                continue;

            RemoveHighlightMaterialDirect(renderer);
        }
        highlightedRenderers.Clear();

        // Fallback check on all configured renderers
        foreach (PageHighlightConfig config in pageConfigs)
        {
            foreach (MeshHighlightEntry entry in config.meshEntries)
            {
                RemoveHighlightFromEntry(entry);
            }
        }
    }

    private void RemoveHighlightMaterialDirect(Renderer renderer)
    {
        if (renderer == null)
            return;

        Material[] mats = renderer.sharedMaterials;
        List<Material> newMats = new List<Material>();

        bool modified = false;
        foreach (Material mat in mats)
        {
            if (mat != highlightMaterial)
            {
                newMats.Add(mat);
            }
            else
            {
                modified = true;
            }
        }

        if (modified)
        {
            renderer.sharedMaterials = newMats.ToArray();
        }
    }

    //==========================================================
    // HELPERS
    //==========================================================

    private PageHighlightConfig GetConfigByPageIndex(int pageIndex)
    {
        foreach (PageHighlightConfig config in pageConfigs)
        {
            if (config.pageIndex == pageIndex)
                return config;
        }

        return null;
    }
}