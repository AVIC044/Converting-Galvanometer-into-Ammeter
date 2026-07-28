using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class MultiStepDialerController : MonoBehaviour
{
    [Header("Input Fields (Order Matters)")]
    public TMP_InputField numeratorField;
    public TMP_InputField denominatorField;
    public TMP_InputField gField;
    public TMP_InputField finalAnswerField;

    [Header("Correct Answers")]
    public float numeratorAnswer;
    public float denominatorAnswer;
    public float gAnswer;
    public float finalAnswer;

    [Header("Success Icons (Same Order)")]
    public GameObject numeratorIcon;
    public GameObject denominatorIcon;
    public GameObject gIcon;
    public GameObject finalIcon;

    [Header("Buttons")]
    public Button validateButton;
    public Button autoFillButton;

    [Header("Settings")]
    public int maxWrongAttempts = 3;
    public float tolerance = 0.001f;

    [Header("Events")]
    public UnityEvent OnCorrectAnswer;
    public UnityEvent OnWrongAnswer;
    public UnityEvent OnAllAnswersVerified;

    private TMP_InputField[] fields;
    private float[] answers;
    private GameObject[] icons;

    private int activeIndex;
    private int wrongAttempts;
    private bool solved;

    private PageNavigationController slideController;

    void Start()
    {
        slideController = FindFirstObjectByType<PageNavigationController>();

        fields = new TMP_InputField[]
        {
            numeratorField,
            denominatorField,
            gField,
            finalAnswerField
        };

        answers = new float[]
        {
            numeratorAnswer,
            denominatorAnswer,
            gAnswer,
            finalAnswer
        };

        icons = new GameObject[]
        {
            numeratorIcon,
            denominatorIcon,
            gIcon,
            finalIcon
        };

        ResetAll();

        if (validateButton)
            validateButton.onClick.AddListener(OnValidatePressed);

        if (autoFillButton)
        {
            autoFillButton.onClick.AddListener(AutoFillAll);
            autoFillButton.gameObject.SetActive(false);
        }
    }

    public void OnDigitPressed(string digit)
    {
        if (solved) return;
        if (!fields[activeIndex].interactable) return;

        fields[activeIndex].text += digit;
    }

    public void OnDecimalPressed()
    {
        if (solved) return;

        TMP_InputField f = fields[activeIndex];

        if (!f.interactable) return;

        if (!f.text.Contains("."))
        {
            if (string.IsNullOrEmpty(f.text))
                f.text = "0.";
            else
                f.text += ".";
        }
    }

    public void OnBackspacePressed()
    {
        if (solved) return;

        TMP_InputField f = fields[activeIndex];

        if (!f.interactable) return;

        if (f.text.Length > 0)
            f.text = f.text.Substring(0, f.text.Length - 1);
    }

    public void OnValidatePressed()
    {
        if (solved) return;

        TMP_InputField current = fields[activeIndex];

        if (!float.TryParse(current.text, out float value))
            return;

        if (Mathf.Abs(value - answers[activeIndex]) > tolerance)
        {
            current.text = "";
            wrongAttempts++;

            OnWrongAnswer?.Invoke();

            if (wrongAttempts >= maxWrongAttempts && autoFillButton)
                autoFillButton.gameObject.SetActive(true);

            return;
        }

        current.interactable = false;
        icons[activeIndex]?.SetActive(true);
        OnCorrectAnswer?.Invoke();

        activeIndex++;

        if (activeIndex < fields.Length)
        {
            fields[activeIndex].interactable = true;
        }
        else
        {
            FinishPuzzle();
        }
    }

    public void AutoFillAll()
    {
        if (solved) return;

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].text = answers[i].ToString();
            fields[i].interactable = false;
            icons[i]?.SetActive(true);
        }

        FinishPuzzle();
    }

    void FinishPuzzle()
    {
        solved = true;
        activeIndex = fields.Length;

        if (validateButton)
            validateButton.interactable = false;

        if (autoFillButton)
            autoFillButton.gameObject.SetActive(false);

        foreach (TMP_InputField f in fields)
            f.interactable = false;

        slideController?.EnableNavigationButtons();

        OnAllAnswersVerified?.Invoke();
    }

    public void ResetAll()
    {
        solved = false;
        wrongAttempts = 0;
        activeIndex = 0;

        if (validateButton)
            validateButton.interactable = true;

        if (autoFillButton)
            autoFillButton.gameObject.SetActive(false);

        for (int i = 0; i < fields.Length; i++)
        {
            fields[i].text = "";
            fields[i].interactable = (i == 0);

            if (icons[i] != null)
                icons[i].SetActive(false);
        }
    }
}
