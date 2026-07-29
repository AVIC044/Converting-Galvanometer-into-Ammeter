using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

public class SingleFieldDialerController : MonoBehaviour
{
    [Header("Input")]
    public TMP_InputField answerField;

    [Header("Correct Answer")]
    public float correctAnswer;

    [Header("Success Icon")]
    public GameObject successIcon;

    public GameObject wrongIcon;

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

    private Coroutine wrongIconCoroutine;

    private void Start()
    {
        ResetAll();

        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
            validateButton.onClick.AddListener(OnValidatePressed);
        }

        if (autoFillButton != null)
        {
            autoFillButton.onClick.RemoveAllListeners();
            autoFillButton.onClick.AddListener(AutoFill);
            autoFillButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (wrongIconCoroutine != null)
        {
            StopCoroutine(wrongIconCoroutine);
            wrongIconCoroutine = null;
        }

        if (wrongIcon != null)
            wrongIcon.SetActive(false);
    }

    private IEnumerator ShowWrongIcon()
    {
        if (wrongIcon != null)
            wrongIcon.SetActive(true);

        yield return new WaitForSeconds(0.7f);

        if (wrongIcon != null)
            wrongIcon.SetActive(false);

        if (answerField != null)
        {
            answerField.text = "";
            answerField.Select();
            answerField.ActivateInputField();
        }

        wrongIconCoroutine = null;
    }

    public void OnDigitPressed(string digit)
    {
        if (solved || answerField == null) return;

        answerField.text += digit;
    }

    public void OnDecimalPressed()
    {
        if (solved || answerField == null) return;

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
        if (solved || answerField == null) return;

        if (answerField.text.Length > 0)
            answerField.text = answerField.text.Substring(0, answerField.text.Length - 1);
    }

    public void OnValidatePressed()
    {
        if (solved || answerField == null) return;

        if (!float.TryParse(answerField.text, out float value))
            return;

        if (Mathf.Abs(value - correctAnswer) > tolerance)
        {
            if (wrongIconCoroutine != null)
                StopCoroutine(wrongIconCoroutine);

            wrongIconCoroutine = StartCoroutine(ShowWrongIcon());
            wrongAttempts++;
            OnWrongAnswer?.Invoke();

            if (wrongAttempts >= maxWrongAttempts && autoFillButton != null)
                autoFillButton.gameObject.SetActive(true);

            return;
        }

        if (successIcon != null)
            successIcon.SetActive(true);

        answerField.interactable = false;

        OnCorrectAnswer?.Invoke();

        FinishPuzzle();
    }

    public void AutoFill()
    {
        if (answerField != null)
            answerField.text = correctAnswer.ToString();

        if (successIcon != null)
            successIcon.SetActive(true);

        FinishPuzzle();
    }

    private void FinishPuzzle()
    {
        solved = true;

        if (answerField != null)
            answerField.interactable = false;

        if (validateButton != null)
            validateButton.interactable = false;

        if (autoFillButton != null)
            autoFillButton.gameObject.SetActive(false);

        // Access the singleton instance directly so multiple components don't interfere
        if (PageNavigationController.Instance != null)
        {
            PageNavigationController.Instance.EnableNavigationButtons();
        }
        else
        {
            PageNavigationController.RequestNavigationUnlock();
        }

        OnAllAnswersVerified?.Invoke();
    }

    public void ResetAll()
    {
        solved = false;
        wrongAttempts = 0;

        if (answerField != null)
        {
            answerField.text = "";
            answerField.interactable = true;
            answerField.Select();
            answerField.ActivateInputField();
        }

        if (successIcon != null)
            successIcon.SetActive(false);

        if (wrongIcon != null)
            wrongIcon.SetActive(false);

        if (validateButton != null)
            validateButton.interactable = true;

        if (autoFillButton != null)
            autoFillButton.gameObject.SetActive(false);
    }
}