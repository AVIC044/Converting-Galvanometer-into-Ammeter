using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Slide_Controller : MonoBehaviour
{
    [Header("Page Index Configuration (0-based)")]
    [Tooltip("SET 1: The page index where setting the first resistance is validated.")]
    [SerializeField] private int setResistancePageIndex1 = 15;

    [Tooltip("SET 2: The page index where setting the second resistance is validated.")]
    [SerializeField] private int setResistancePageIndex2 = 17;

    [Tooltip("The page index where plugging in ALL plugs triggers completion.")]
    [SerializeField] private int allPlugsPageIndex = 18;

    [Header("Slide References WorldSpace")]
    [SerializeField] private GameObject plugSetLeft;
    [SerializeField] private GameObject plugSetRight;
    [SerializeField] private Material highlightShaderMaterial;
    [SerializeField] private Material defaultPlugMaterial;

    [Header("Slide References UI and Validation")]
    [SerializeField] private GameObject[] valueSelectionButtons;
    [SerializeField] private ResistanceBoxController _resistanceBox;

    [Tooltip("SET 1: Required resistance value.")]
    [SerializeField] private int requiredResistance1 = 9900;

    [Tooltip("SET 2: Required resistance value.")]
    [SerializeField] private int requiredResistance2 = 5000;

    [SerializeField] private Button setResistanceButton;
    [SerializeField] private Button retryButton;

    [SerializeField] private GameObject correctIcon;
    [SerializeField] private GameObject wrongIcon;
    [SerializeField] private GameObject wrongHintPanel;

    [Header("Value Button Highlight")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Events")]
    public UnityEvent OnAllPlugsInserted;
    public UnityEvent OnResistanceValidated;

    private Image[] _valueButtonImages;
    private Button[] _valueButtons;

    // Direct local tracking of toggled plug indices purely for UI highlight rendering
    private readonly HashSet<int> _removedPlugIndices = new HashSet<int>();

    private int activePageIndex = -1;

    private void Awake()
    {
        _valueButtonImages = new Image[valueSelectionButtons.Length];
        _valueButtons = new Button[valueSelectionButtons.Length];

        for (int i = 0; i < valueSelectionButtons.Length; i++)
        {
            if (valueSelectionButtons[i] == null) continue;

            int capturedIndex = i;

            Button btn = valueSelectionButtons[i].GetComponent<Button>();
            _valueButtons[i] = btn;
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnValueButtonClicked(capturedIndex));
            }

            Image img = valueSelectionButtons[i].GetComponentInChildren<Image>();
            _valueButtonImages[i] = img;

            if (img != null)
                img.color = defaultColor;
        }
    }

    private void Start()
    {
        if (plugSetLeft != null && plugSetRight != null)
            StartCoroutine(OnSlideStartHighlight());

        ResetValidationUI();
        RefreshActivePage();
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;

        if (_resistanceBox != null)
        {
            _resistanceBox.OnResistanceChanged -= HandleResistanceChanged;
            _resistanceBox.OnResistanceChanged += HandleResistanceChanged;
            _resistanceBox.OnPlugToggled -= HandlePlugToggled;
            _resistanceBox.OnPlugToggled += HandlePlugToggled;
        }

        RefreshActivePage();
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;

        if (_resistanceBox != null)
        {
            _resistanceBox.OnResistanceChanged -= HandleResistanceChanged;
            _resistanceBox.OnPlugToggled -= HandlePlugToggled;
        }
    }

    private void RefreshActivePage()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    private void HandlePageChanged(int newPageIndex)
    {
        int previousPageIndex = activePageIndex;
        activePageIndex = newPageIndex;

        // Reset plugs ONLY when transitioning INTO the second value slide (Page 17)
        if (activePageIndex == setResistancePageIndex2 && previousPageIndex != setResistancePageIndex2)
        {
            _removedPlugIndices.Clear();
            if (_resistanceBox != null)
            {
                _resistanceBox.RestoreAllPlugs();
            }
        }

        // Reset validation UI icons/messages
        ResetValidationUI();

        bool isSetPage = (activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2);
        bool isPageCompleted = PageNavigationController.Instance != null &&
                               PageNavigationController.Instance.IsPageCompleted(activePageIndex);

        // Make buttons interactable immediately if the page isn't marked complete
        SetButtonsInteractable(!isPageCompleted);

        // Show/hide Set Resistance button
        if (setResistanceButton != null)
        {
            setResistanceButton.gameObject.SetActive(isSetPage && !isPageCompleted);
        }

        // Keep UI button colors synchronized with box state
        SyncUiWithBoxState();
    }

    private void ResetValidationUI()
    {
        if (correctIcon != null) correctIcon.SetActive(false);
        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (wrongHintPanel != null) wrongHintPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
    }

    public void OnValueButtonClicked(int slotIndex)
    {
        bool isPageCompleted = PageNavigationController.Instance != null &&
                               PageNavigationController.Instance.IsPageCompleted(activePageIndex);

        if (isPageCompleted) return;

        _resistanceBox?.SelectResistance(slotIndex);
    }

    private void HandlePlugToggled(int slotIndex, bool isRemoved)
    {
        if (isRemoved)
            _removedPlugIndices.Add(slotIndex);
        else
            _removedPlugIndices.Remove(slotIndex);

        if (_valueButtonImages != null && slotIndex >= 0 && slotIndex < _valueButtonImages.Length)
        {
            if (_valueButtonImages[slotIndex] != null)
            {
                Color targetColor = isRemoved ? selectedColor : defaultColor;
                _valueButtonImages[slotIndex].color = targetColor;
            }
        }

        if (activePageIndex == allPlugsPageIndex)
        {
            CheckIfAllPlugsInserted();
        }
    }

    private void CheckIfAllPlugsInserted()
    {
        if (_resistanceBox == null) return;

        bool isPageCompleted = PageNavigationController.Instance != null &&
                               PageNavigationController.Instance.IsPageCompleted(activePageIndex);

        if (isPageCompleted) return;

        if (_resistanceBox.CurrentResistance == 0)
        {
            if (correctIcon != null) correctIcon.SetActive(true);

            SetButtonsInteractable(false);

            OnAllPlugsInserted?.Invoke();
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    public void OnSetResistancePressed()
    {
        bool isPageCompleted = PageNavigationController.Instance != null &&
                               PageNavigationController.Instance.IsPageCompleted(activePageIndex);

        if (isPageCompleted) return;

        int targetRequiredResistance = 0;

        if (activePageIndex == setResistancePageIndex1)
        {
            targetRequiredResistance = requiredResistance1;
        }
        else if (activePageIndex == setResistancePageIndex2)
        {
            targetRequiredResistance = requiredResistance2;
        }
        else
        {
            return;
        }

        if (_resistanceBox != null && _resistanceBox.CurrentResistance == targetRequiredResistance)
        {
            HandleCorrectAnswer();
        }
        else
        {
            HandleWrongAnswer();
        }
    }

    private void HandleCorrectAnswer()
    {
        if (correctIcon != null) correctIcon.SetActive(true);
        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(false);

        SetButtonsInteractable(false);

        OnResistanceValidated?.Invoke();
        PageNavigationController.RequestNavigationUnlock();
    }

    private void HandleWrongAnswer()
    {
        _removedPlugIndices.Clear();
        if (_resistanceBox != null)
            _resistanceBox.RestoreAllPlugs();

        if (correctIcon != null) correctIcon.SetActive(false);
        if (wrongIcon != null) wrongIcon.SetActive(true);

        if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (wrongHintPanel != null) wrongHintPanel.SetActive(true);
    }

    public void Retry()
    {
        ResetValidationUI();

        if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(true);

        SetButtonsInteractable(true);
    }

    private void HandleResistanceChanged(int index)
    {
        UpdateSetResistanceButtonInteractable();
    }

    private void SetButtonsInteractable(bool state)
    {
        if (_valueButtons != null)
        {
            for (int i = 0; i < _valueButtons.Length; i++)
            {
                if (_valueButtons[i] != null)
                    _valueButtons[i].interactable = state;
            }
        }

        UpdateSetResistanceButtonInteractable();
    }

    private void UpdateSetResistanceButtonInteractable()
    {
        if (setResistanceButton == null) return;

        bool isSetPage = (activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2);
        bool isPageCompleted = PageNavigationController.Instance != null &&
                               PageNavigationController.Instance.IsPageCompleted(activePageIndex);

        int currentRes = (_resistanceBox != null) ? _resistanceBox.CurrentResistance : 0;

        setResistanceButton.interactable = !isPageCompleted && isSetPage && (currentRes > 0);
    }

    private void SyncUiWithBoxState()
    {
        if (_valueButtonImages == null) return;

        for (int i = 0; i < _valueButtonImages.Length; i++)
        {
            if (_valueButtonImages[i] == null) continue;

            bool isRemoved = _removedPlugIndices.Contains(i) || IsPlugRemovedSafe(i);
            Color targetColor = isRemoved ? selectedColor : defaultColor;

            _valueButtonImages[i].color = targetColor;
        }

        UpdateSetResistanceButtonInteractable();
    }

    private bool IsPlugRemovedSafe(int index)
    {
        if (_resistanceBox == null) return false;

        var method = _resistanceBox.GetType().GetMethod("IsPlugRemoved");
        if (method == null) return false;

        try
        {
            object result = method.Invoke(_resistanceBox, new object[] { index });
            if (result is bool b) return b;
        }
        catch { }

        return false;
    }

    private IEnumerator OnSlideStartHighlight()
    {
        yield return new WaitForSeconds(1f);

        MeshRenderer[] plugSetLeftRenderers = plugSetLeft ? plugSetLeft.GetComponentsInChildren<MeshRenderer>() : new MeshRenderer[0];
        MeshRenderer[] plugSetRightRenderers = plugSetRight ? plugSetRight.GetComponentsInChildren<MeshRenderer>() : new MeshRenderer[0];

        foreach (MeshRenderer renderer in plugSetLeftRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 1)
            {
                mats[1] = highlightShaderMaterial;
                renderer.materials = mats;
            }
        }

        foreach (MeshRenderer renderer in plugSetRightRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 1)
            {
                mats[1] = highlightShaderMaterial;
                renderer.materials = mats;
            }
        }

        yield return new WaitForSeconds(2f);

        foreach (MeshRenderer renderer in plugSetLeftRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 1)
            {
                mats[1] = defaultPlugMaterial;
                renderer.materials = mats;
            }
        }

        foreach (MeshRenderer renderer in plugSetRightRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 1)
            {
                mats[1] = defaultPlugMaterial;
                renderer.materials = mats;
            }
        }
    }
}