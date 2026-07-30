using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System.Globalization;

public class SingleFieldDialerController : MonoBehaviour
{
    [Header("Input Fields")]
    public TMP_InputField[] answerFields;

    [Header("Correct Answers")]
    public float[] correctAnswers;

    [Header("Icons")]
    public Image[] iconImages;

    [Header("Icon Sprites")]
    public Sprite correctSprite;
    public Sprite wrongSprite;

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

    private int currentFieldIndex = 0;
    private int wrongAttempts;
    private bool solvedAll;
    private bool isProcessingStep = false;

    private PageNavigationController slideController;

    private TMP_InputField ActiveField => (answerFields != null && currentFieldIndex >= 0 && currentFieldIndex < answerFields.Length) ? answerFields[currentFieldIndex] : null;
    private float ActiveAnswer => (correctAnswers != null && currentFieldIndex >= 0 && currentFieldIndex < correctAnswers.Length) ? correctAnswers[currentFieldIndex] : 0f;
    private Image ActiveIconImage => (iconImages != null && currentFieldIndex >= 0 && currentFieldIndex < iconImages.Length) ? iconImages[currentFieldIndex] : null;

    void Start()
    {
        slideController = FindFirstObjectByType<PageNavigationController>();

        if (answerFields == null || answerFields.Length == 0)
        {
            Debug.LogError("No Answer Fields assigned.");
            enabled = false;
            return;
        }

        if (answerFields.Length != correctAnswers.Length)
        {
            Debug.LogError("Answer Fields and Correct Answers arrays must be the same size.");
            enabled = false;
            return;
        }

        // Clean up UI button listeners to prevent multiple clicks firing in a single frame
        if (validateButton != null)
        {
            validateButton.onClick.RemoveAllListeners();
            validateButton.onClick.AddListener(OnValidatePressed);
        }

        if (autoFillButton != null)
        {
            autoFillButton.onClick.RemoveAllListeners();
            autoFillButton.onClick.AddListener(AutoFillCurrentField);
            autoFillButton.gameObject.SetActive(false);
        }

        ResetAll();
    }

    IEnumerator ShowWrongIcon()
    {
        if (ActiveIconImage != null)
        {
            ActiveIconImage.sprite = wrongSprite;
            ActiveIconImage.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(0.7f);

        if (ActiveIconImage != null)
        {
            ActiveIconImage.gameObject.SetActive(false);
        }

        if (ActiveField != null)
        {
            ActiveField.text = "";
            ActiveField.Select();
            ActiveField.ActivateInputField();
        }
    }

    public void OnDigitPressed(string digit)
    {
        if (solvedAll || isProcessingStep || ActiveField == null) return;

        ActiveField.text += digit;
    }

    public void OnDecimalPressed()
    {
        if (solvedAll || isProcessingStep || ActiveField == null) return;

        if (!ActiveField.text.Contains("."))
        {
            if (string.IsNullOrEmpty(ActiveField.text))
                ActiveField.text = "0.";
            else
                ActiveField.text += ".";
        }
    }

    public void OnBackspacePressed()
    {
        if (solvedAll || isProcessingStep || ActiveField == null) return;

        if (ActiveField.text.Length > 0)
        {
            ActiveField.text = ActiveField.text.Substring(0, ActiveField.text.Length - 1);
        }
    }

    public void OnValidatePressed()
    {
        if (solvedAll || isProcessingStep || ActiveField == null) return;

        if (!float.TryParse(ActiveField.text, NumberStyles.Any, CultureInfo.InvariantCulture, out float value))
            return;

        if (Mathf.Abs(value - ActiveAnswer) > tolerance)
        {
            StartCoroutine(ShowWrongIcon());
            wrongAttempts++;
            OnWrongAnswer?.Invoke();

            if (wrongAttempts >= maxWrongAttempts && autoFillButton != null)
                autoFillButton.gameObject.SetActive(true);

            return;
        }

        StartCoroutine(ProcessCurrentFieldSuccessRoutine());
    }

    public void AutoFillCurrentField()
    {
        if (solvedAll || isProcessingStep || ActiveField == null) return;

        // Fills ONLY the currently active index field
        ActiveField.text = ActiveAnswer.ToString("G7", CultureInfo.InvariantCulture);

        StartCoroutine(ProcessCurrentFieldSuccessRoutine());
    }

    private IEnumerator ProcessCurrentFieldSuccessRoutine()
    {
        if (isProcessingStep) yield break;
        isProcessingStep = true;

        TMP_InputField fieldToLock = ActiveField;

        // 1. Display success icon for current element
        if (ActiveIconImage != null)
        {
            ActiveIconImage.sprite = correctSprite;
            ActiveIconImage.gameObject.SetActive(true);
        }

        // 2. Lock current input field
        LockInputField(fieldToLock);

        OnCorrectAnswer?.Invoke();

        // 3. Reset wrong attempts & hide auto-fill button
        wrongAttempts = 0;
        if (autoFillButton != null)
            autoFillButton.gameObject.SetActive(false);

        // 4. Force Canvas update and wait 1 frame to prevent click propagation to the next field
        Canvas.ForceUpdateCanvases();
        yield return null;

        // 5. Increment index by EXACTLY 1 step
        currentFieldIndex++;

        if (currentFieldIndex < answerFields.Length)
        {
            // Unlock and activate ONLY the next field
            UnlockInputField(ActiveField);
        }
        else
        {
            // Finished all elements in order
            FinishPuzzle();
        }

        isProcessingStep = false;
    }

    void FinishPuzzle()
    {
        solvedAll = true;

        if (validateButton != null)
            validateButton.interactable = false;

        if (autoFillButton != null)
            autoFillButton.gameObject.SetActive(false);

        slideController?.EnableNavigationButtons();

        OnAllAnswersVerified?.Invoke();
    }

    public void ResetAll()
    {
        StopAllCoroutines();

        solvedAll = false;
        isProcessingStep = false;
        currentFieldIndex = 0;
        wrongAttempts = 0;

        if (iconImages != null)
        {
            for (int i = 0; i < iconImages.Length; i++)
            {
                if (iconImages[i] != null)
                    iconImages[i].gameObject.SetActive(false);
            }
        }

        if (validateButton != null)
            validateButton.interactable = true;

        if (autoFillButton != null)
            autoFillButton.gameObject.SetActive(false);

        // Lock all input fields on start/reset
        for (int i = 0; i < answerFields.Length; i++)
        {
            if (answerFields[i] != null)
            {
                answerFields[i].text = "";
                LockInputField(answerFields[i]);
            }
        }

        // Unlock ONLY Element 0
        if (ActiveField != null)
        {
            UnlockInputField(ActiveField);
        }
    }

    private void LockInputField(TMP_InputField field)
    {
        if (field == null) return;

        field.interactable = false;

        if (field.targetGraphic != null)
            field.targetGraphic.raycastTarget = false;
    }

    private void UnlockInputField(TMP_InputField field)
    {
        if (field == null) return;

        field.interactable = true;

        if (field.targetGraphic != null)
            field.targetGraphic.raycastTarget = true;

        field.Select();
        field.ActivateInputField();
    }
}