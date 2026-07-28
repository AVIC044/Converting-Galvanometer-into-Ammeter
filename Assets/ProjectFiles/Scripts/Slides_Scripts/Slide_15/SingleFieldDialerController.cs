using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;

public class SingleFieldDialerController : MonoBehaviour
{
    [Header("Input")]
    public TMP_InputField answerField;

    [Header("Correct Answer")]
    public float correctAnswer;

    [Header("Success Icon")]
    public GameObject successIcon;

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

    private int wrongAttempts;
    private bool solved;

    private PageNavigationController slideController;

    void Start()
    {
        slideController = FindFirstObjectByType<PageNavigationController>();

        ResetAll();

        validateButton.onClick.AddListener(OnValidatePressed);
        autoFillButton.onClick.AddListener(AutoFill);

        autoFillButton.gameObject.SetActive(false);
    }

    public void OnDigitPressed(string digit)
    {
        if (solved) return;

        answerField.text += digit;
    }

    public void OnDecimalPressed()
    {
        if (solved) return;

        if (!answerField.text.Contains("."))
        {
            if (answerField.text == "")
                answerField.text = "0.";
            else
                answerField.text += ".";
        }
    }

    public void OnBackspacePressed()
    {
        if (solved) return;

        if (answerField.text.Length > 0)
            answerField.text =
                answerField.text.Substring(0, answerField.text.Length - 1);
    }

    public void OnValidatePressed()
    {
        if (solved) return;

        if (!float.TryParse(answerField.text, out float value))
            return;

        if (Mathf.Abs(value - correctAnswer) > tolerance)
        {
            answerField.text = "";
            wrongAttempts++;

            OnWrongAnswer?.Invoke();

            if (wrongAttempts >= maxWrongAttempts)
                autoFillButton.gameObject.SetActive(true);

            return;
        }

        successIcon.SetActive(true);
        answerField.interactable = false;

        OnCorrectAnswer?.Invoke();

        FinishPuzzle();
    }

    void AutoFill()
    {
        answerField.text = correctAnswer.ToString();
        successIcon.SetActive(true);

        FinishPuzzle();
    }

    void FinishPuzzle()
    {
        solved = true;

        answerField.interactable = false;
        validateButton.interactable = false;
        autoFillButton.gameObject.SetActive(false);

        slideController?.EnableNavigationButtons();

        OnAllAnswersVerified?.Invoke();
    }

    public void ResetAll()
    {
        solved = false;
        wrongAttempts = 0;

        answerField.text = "";
        answerField.interactable = true;

        successIcon.SetActive(false);

        validateButton.interactable = true;
        autoFillButton.gameObject.SetActive(false);
    }
}
