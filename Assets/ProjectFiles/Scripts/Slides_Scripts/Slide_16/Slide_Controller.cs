using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
    [SerializeField] private bool validated = false;

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

            Image img = valueSelectionButtons[i].GetComponent<Image>();
            _valueButtonImages[i] = img;

            if (img != null)
                img.color = defaultColor;
        }
    }

    private void Start()
    {
        if (plugSetLeft != null && plugSetRight != null)
            StartCoroutine(OnSlideStartHighlight());

        if (retryButton != null)
            retryButton.gameObject.SetActive(false);

        if (correctIcon != null) correctIcon.SetActive(false);
        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (wrongHintPanel != null) wrongHintPanel.SetActive(false);

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
        activePageIndex = newPageIndex;

        // Reset state when entering the ALL PLUGS page (Index 18)
        if (activePageIndex == allPlugsPageIndex)
        {
            validated = false; // Reset validation so button clicks work again
            SetButtonsInteractable(true);

            if (correctIcon != null) correctIcon.SetActive(false);
            if (wrongIcon != null) wrongIcon.SetActive(false);
            if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(false);
        }
        else if ((activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2) && !validated)
        {
            SetButtonsInteractable(true);
            if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(true);
        }

        // Always synchronize visual plug states and carry forward highlighted colors to any active slide index
        SyncUiWithBoxState();
    }

    public void OnValueButtonClicked(int slotIndex)
    {
        // Allow clicks on either the Set Resistance page (if not completed) or the All Plugs page
        if (validated) return;

        _resistanceBox?.SelectResistance(slotIndex);
    }

    private void HandlePlugToggled(int slotIndex, bool isRemoved)
    {
        if (_valueButtonImages != null && slotIndex >= 0 && slotIndex < _valueButtonImages.Length)
        {
            if (_valueButtonImages[slotIndex] != null)
            {
                Color targetColor = isRemoved ? selectedColor : defaultColor;
                _valueButtonImages[slotIndex].color = targetColor;
            }
        }

        // Check completion ONLY when on page 18
        if (activePageIndex == allPlugsPageIndex)
        {
            CheckIfAllPlugsInserted();
        }
    }

    private void CheckIfAllPlugsInserted()
    {
        if (_resistanceBox == null || validated) return;

        // All plugs inserted = Resistance is 0
        if (_resistanceBox.CurrentResistance == 0)
        {
            validated = true;
            if (correctIcon != null) correctIcon.SetActive(true);

            SetButtonsInteractable(false);

            OnAllPlugsInserted?.Invoke();
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    public void OnSetResistancePressed()
    {
        if (validated) return;

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
            return; // Not on a set resistance page
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
        validated = true;

        if (correctIcon != null) correctIcon.SetActive(true);
        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        SetButtonsInteractable(false);

        OnResistanceValidated?.Invoke();
        PageNavigationController.RequestNavigationUnlock();
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

        if (setResistanceButton != null)
        {
            int currentRes = (_resistanceBox != null) ? _resistanceBox.CurrentResistance : 0;
            bool isSetPage = (activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2);
            setResistanceButton.interactable = state && (currentRes > 0) && isSetPage;
        }

        // Force re-sync green/default highlights after changing interactable state
        SyncUiWithBoxState();
    }

    private void HandleWrongAnswer()
    {
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
        if (wrongIcon != null) wrongIcon.SetActive(false);
        if (wrongHintPanel != null) wrongHintPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);

        if (setResistanceButton != null) setResistanceButton.gameObject.SetActive(true);

        SetButtonsInteractable(true);
    }

    private void HandleResistanceChanged(int index)
    {
        bool isSetPage = (activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2);
        if (validated && isSetPage) return;

        int currentResistance = (_resistanceBox != null) ? _resistanceBox.CurrentResistance : 0;
        if (setResistanceButton != null)
            setResistanceButton.interactable = (currentResistance > 0) && isSetPage;
    }

    private void SyncUiWithBoxState()
    {
        if (_resistanceBox == null) return;

        if (_valueButtonImages != null)
        {
            for (int i = 0; i < _valueButtonImages.Length; i++)
            {
                if (_valueButtonImages[i] == null) continue;

                bool isRemoved = IsPlugRemovedSafe(i);
                Color targetColor = isRemoved ? selectedColor : defaultColor;

                // Always apply target highlight color so it carries forward to all slide indices
                _valueButtonImages[i].color = targetColor;
            }
        }

        if (setResistanceButton != null)
        {
            bool isSetPage = (activePageIndex == setResistancePageIndex1 || activePageIndex == setResistancePageIndex2);
            setResistanceButton.interactable = (_resistanceBox.CurrentResistance > 0) && isSetPage;
        }
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