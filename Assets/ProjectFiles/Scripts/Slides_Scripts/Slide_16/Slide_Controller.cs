using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class Slide_Controller : MonoBehaviour
{
    [Header("Slide 16 References WorldSpace")]
    [SerializeField] private GameObject plugSetLeft;
    [SerializeField] private GameObject plugSetRight;
    [SerializeField] private Material highlightShaderMaterial;
    [SerializeField] private Material defaultPlugMaterial;

    [Header("Slide 16 References UI and Validation")]
    [SerializeField] private bool isSlide16Completed = false;
    [SerializeField] private GameObject[] valueSelectionButtons;
    [SerializeField] private ResistanceBoxController _resistanceBox;
    [SerializeField] private bool validated = false;
    [SerializeField] private int requiredResistance;

    [SerializeField] private Button setResistanceButton;
    [SerializeField] private Button retryButton;

    [SerializeField] private GameObject correctIcon;
    [SerializeField] private GameObject wrongIcon;

    [SerializeField] private GameObject wrongHintPanel;

    [Header("Value Button Highlight")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.white;

    private Image[] _valueButtonImages;

    private void Start()
    {
        StartCoroutine(OnSlideStartHighlight());
        setResistanceButton.interactable = false;

        retryButton.gameObject.SetActive(false);

        correctIcon.SetActive(false);
        wrongIcon.SetActive(false);

        wrongHintPanel.SetActive(false);

        if (_resistanceBox != null && valueSelectionButtons.Length != _resistanceBox.OptionCount)
        {
            Debug.LogWarning($"[{nameof(Slide_Controller)}] valueSelectionButtons ({valueSelectionButtons.Length}) " +
                              $"and options in ResistanceBoxController ({_resistanceBox.OptionCount}) are different lengths. " +
                              "Every button index must line up with the same slot index in the box controller.");
        }

        _valueButtonImages = new Image[valueSelectionButtons.Length];

        for (int i = 0; i < valueSelectionButtons.Length; i++)
        {
            int capturedIndex = i; // avoid closure bug — don't inline i directly

            Button btn = valueSelectionButtons[i].GetComponent<Button>();
            btn.onClick.AddListener(() => OnValueButtonClicked(capturedIndex));

            Image img = valueSelectionButtons[i].GetComponent<Image>();
            _valueButtonImages[i] = img;

            if (img != null)
                img.color = defaultColor;
        }
    }



    public void OnValueButtonClicked(int slotIndex)
    {
        _resistanceBox.SelectResistance(slotIndex);
    }

    private IEnumerator OnSlideStartHighlight()
    {
        yield return new WaitForSeconds(1f);

        MeshRenderer[] plugSetLeftRenderers = plugSetLeft.GetComponentsInChildren<MeshRenderer>();
        MeshRenderer[] plugSetRightRenderers = plugSetRight.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in plugSetLeftRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 0)
            {
                mats[1] = highlightShaderMaterial;
                renderer.materials = mats;
            }
        }

        foreach (MeshRenderer renderer in plugSetRightRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 0)
            {
                mats[1] = highlightShaderMaterial;
                renderer.materials = mats;
            }
        }

        yield return new WaitForSeconds(2f);

        foreach (MeshRenderer renderer in plugSetLeftRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 0)
            {
                mats[1] = defaultPlugMaterial;
                renderer.materials = mats;
            }
        }

        foreach (MeshRenderer renderer in plugSetRightRenderers)
        {
            Material[] mats = renderer.materials;
            if (mats != null && mats.Length > 0)
            {
                mats[1] = defaultPlugMaterial;
                renderer.materials = mats;
            }
        }
    }

    private void OnEnable()
    {
        if (_resistanceBox != null)
        {
            _resistanceBox.OnResistanceChanged -= HandleResistanceChanged;
            _resistanceBox.OnResistanceChanged += HandleResistanceChanged;
            _resistanceBox.OnPlugToggled += HandlePlugToggled;
            SyncUiWithBoxState();
        }
    }

    private void OnDisable()
    {
        if (_resistanceBox != null)
        {
            _resistanceBox.OnResistanceChanged -= HandleResistanceChanged;
            _resistanceBox.OnPlugToggled -= HandlePlugToggled;
        }
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
                Color c = isRemoved ? selectedColor : defaultColor;
                c.a = 0.5f; // matches HandlePlugToggled's visual convention
                _valueButtonImages[i].color = c;
            }
        }

        setResistanceButton.interactable = _resistanceBox.CurrentResistance > 0;
    }

    // Use reflection to safely attempt to call IsPlugRemoved on the ResistanceBoxController
    // If the method doesn't exist, default to false (not removed).
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
        catch
        {
            // ignore invocation errors and fall through to default
        }

        return false;
    }

    private void HandleResistanceChanged(int index)
    {
        int currentResistance = (_resistanceBox != null) ? _resistanceBox.CurrentResistance : 0;
        setResistanceButton.interactable = currentResistance > 0;
    }

    private void HandlePlugToggled(int slotIndex, bool isRemoved)
    {
        if (_valueButtonImages == null || slotIndex < 0 || slotIndex >= _valueButtonImages.Length)
            return;

        if (_valueButtonImages[slotIndex] == null)
            return;

        Color buttonColor = isRemoved ? selectedColor : defaultColor;
        buttonColor.a = 0.5f;
        _valueButtonImages[slotIndex].color = buttonColor;
    }


    public void ResetValidation()
    {
        validated = false;
    }

    public void OnSetResistancePressed()
    {
        if (validated)
            return;

        if (_resistanceBox.CurrentResistance == requiredResistance)
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

        correctIcon.SetActive(true);
        wrongIcon.SetActive(false);

        retryButton.gameObject.SetActive(false);

        PageNavigationController.RequestNavigationUnlock();
    }

    private void HandleWrongAnswer()
    {
        _resistanceBox.RestoreAllPlugs();

        correctIcon.SetActive(false);
        wrongIcon.SetActive(true);

        setResistanceButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);

        wrongHintPanel.SetActive(true);
    }

    public void Retry()
    {
        wrongIcon.SetActive(false);

        wrongHintPanel.SetActive(false);

        retryButton.gameObject.SetActive(false);

        setResistanceButton.gameObject.SetActive(true);
    }
}
