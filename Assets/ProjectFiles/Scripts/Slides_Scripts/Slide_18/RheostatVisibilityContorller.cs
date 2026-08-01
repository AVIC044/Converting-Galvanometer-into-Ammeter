using UnityEngine;
using System.Collections;
public class RheostatVisibilityController : MonoBehaviour
{
    [Header("Special Rheostat Reference")]
    [SerializeField] private GameObject slide19Rheostat;

    [Header("Target Page Indices (0-based)")]
    [Tooltip("Page 19 = Index 18, Page 24 = Index 23")]
    [SerializeField] private int indexSlide19 = 18;
    [SerializeField] private int indexSlide24 = 23;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;

        if (PageNavigationController.Instance != null)
        {
            HandlePageChanged(PageNavigationController.CurrentIndex);
        }
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    private void HandlePageChanged(int pageIndex)
    {
        // Check if the current page is index 18 or 23
        bool isTargetIndex = (pageIndex == indexSlide19 || pageIndex == indexSlide24);

        // ONLY manage slide19Rheostat! Never call SetActive on defaultRheostat.
        if (slide19Rheostat != null)
            slide19Rheostat.SetActive(isTargetIndex);
    }
}
