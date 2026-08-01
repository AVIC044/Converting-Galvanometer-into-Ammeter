using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class RheostatSlideController : MonoBehaviour
{
    [System.Serializable]
    public class RheostatPoint
    {
        [Tooltip("The slide/page index in PageNavigationController that uses this point")]
        public int slidePageIndex;

        [Tooltip("The 3D transform point on the track representing the correct answer (e.g., Answer_pont_1)")]
        public Transform targetPosition;

        [Header("Correct Reading At targetPosition (overrides the live sweep when handle is here)")]
        public float voltmeterValue = 3f;
        public string voltmeterUnit = "V";

        public float galvanometerValue = 30f;
        public string galvanometerUnit = "";
    }

    [Header("Rheostat Reference")]
    [SerializeField] private RheostatController rheostat;

    [Header("Target Points Configuration")]
    [SerializeField] private RheostatPoint[] points;

    // Driven entirely by OnPageChanged - not exposed for manual editing.
    private int activePoint = 0;

    [Header("Global Meter Ranges (full slider sweep, min position -> max position)")]
    [SerializeField] private float voltmeterRangeMin = 0f;
    [SerializeField] private float voltmeterRangeMax = 5f;
    [SerializeField] private float galvanometerRangeMin = -30f;
    [SerializeField] private float galvanometerRangeMax = 30f;

    [Header("Answer Point Detection")]
    [Tooltip("Allowed slider-position error (normalized 0-1) to count as 'at' the answer point")]
    [SerializeField] private float positionTolerance = 0.02f;

    [Header("Digital Displays")]
    [SerializeField] private TMP_Text voltmeterText;
    [SerializeField] private TMP_Text galvanometerText;

    [Header("Digital Display Parents")]
    [Tooltip("Display parent for the first target slide (e.g., Slide 19 / Index 18)")]
    [SerializeField] private GameObject meterDisplayParent1;

    [Tooltip("Display parent for the second target slide (e.g., Slide 24 / Index 23)")]
    [SerializeField] private GameObject meterDisplayParent2;

    [Header("UI Controls")]
    [SerializeField] private Button validateButton;
    [SerializeField] private Button autoFillButton;
    [SerializeField] private GameObject correctIcon;
    [SerializeField] private GameObject wrongIcon;

    [Header("Settings")]
    [SerializeField] private int maxWrongAttempts = 3;

    [Header("Events")]
    public UnityEvent OnCorrectAnswer;
    public UnityEvent OnWrongAnswer;
    public UnityEvent OnSlideReset;

    private bool completed = false;
    private bool isAtAnswerPoint = false;
    private int wrongAttempts = 0;
    private int currentVoltmeterDisplay = 0;
    private int currentGalvanometerDisplay = 0;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;

        if (rheostat != null)
            rheostat.OnValueChanged += OnRheostatValueChanged;

        // Sync immediately with the active page index whenever enabled
        if (PageNavigationController.Instance != null)
        {
            OnPageChanged(PageNavigationController.CurrentIndex);
        }
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;

        if (rheostat != null)
            rheostat.OnValueChanged -= OnRheostatValueChanged;
    }

    private void Start()
    {
        if (validateButton != null)
            validateButton.onClick.AddListener(ValidateAnswer);

        if (autoFillButton != null)
        {
            autoFillButton.onClick.AddListener(AutoFill);
            autoFillButton.gameObject.SetActive(false);
        }

        // Explicitly check the active page on Start
        OnPageChanged(PageNavigationController.CurrentIndex);
    }

    private void OnPageChanged(int pageIndex)
    {

        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].slidePageIndex == pageIndex)
            {
                SetActivePoint(i);

                // 1. Enable the correct meter display parent for this specific slide
                UpdateMeterDisplayVisibility(pageIndex);

                // 2. Reset slide UI & enable handle interaction
                ResetSlideUI();

                return;
            }
        }

        // --- Not an answer-tracked slide ---

        // Hide both meter text parents on all non-tracked slides
        SetAllMeterDisplaysActive(false);

        if (correctIcon) correctIcon.SetActive(false);
        if (wrongIcon) wrongIcon.SetActive(false);
        if (autoFillButton) autoFillButton.gameObject.SetActive(false);

        if (rheostat != null)
            rheostat.SetInteraction(false);
    }

    private void OnRheostatValueChanged(float normalized)
    {
        if (completed || points == null || points.Length == 0)
            return;

        UpdateLiveDisplay(normalized);
    }

    private void UpdateLiveDisplay(float normalized)
    {
        if (points == null || points.Length == 0 || activePoint >= points.Length || rheostat == null)
            return;

        RheostatPoint p = points[activePoint];

        isAtAnswerPoint = false;
        if (p.targetPosition != null)
        {
            float targetNorm = rheostat.GetNormalizedValueForTransform(p.targetPosition);
            isAtAnswerPoint = Mathf.Abs(normalized - targetNorm) <= positionTolerance;
        }

        float voltmeterLive;
        float galvanometerLive;

        if (isAtAnswerPoint)
        {
            voltmeterLive = p.voltmeterValue;
            galvanometerLive = p.galvanometerValue;
        }
        else
        {
            voltmeterLive = Mathf.Lerp(voltmeterRangeMin, voltmeterRangeMax, normalized);
            galvanometerLive = Mathf.Lerp(galvanometerRangeMin, galvanometerRangeMax, normalized);
        }

        currentVoltmeterDisplay = Mathf.RoundToInt(voltmeterLive);
        currentGalvanometerDisplay = Mathf.RoundToInt(galvanometerLive);

        if (voltmeterText)
            voltmeterText.text = $"{currentVoltmeterDisplay}{p.voltmeterUnit}";

        if (galvanometerText)
            galvanometerText.text = $"{currentGalvanometerDisplay}A";
    }

    public void ValidateAnswer()
    {
        if (completed || points == null || points.Length == 0 || rheostat == null)
            return;

        if (wrongIcon) wrongIcon.SetActive(false);
        if (correctIcon) correctIcon.SetActive(false);

        RheostatPoint currentPoint = points[activePoint];

        if (isAtAnswerPoint)
        {
            StartCoroutine(ShowReadingsRoutine(currentPoint));
        }
        else
        {
            wrongAttempts++;
            if (wrongIcon) wrongIcon.SetActive(true);

            OnWrongAnswer?.Invoke();

            if (autoFillButton != null && wrongAttempts >= maxWrongAttempts)
            {
                autoFillButton.gameObject.SetActive(true);
            }
        }
    }

    public void AutoFill()
    {
        if (completed || points == null || points.Length == 0 || rheostat == null)
            return;

        RheostatPoint currentPoint = points[activePoint];

        if (currentPoint.targetPosition != null)
        {
            float targetNorm = rheostat.GetNormalizedValueForTransform(currentPoint.targetPosition);
            rheostat.SetNormalizedValue(targetNorm); // Snaps rheostat to target position
        }

        isAtAnswerPoint = true;
        UpdateLiveDisplay(rheostat.NormalizedValue);

        StartCoroutine(ShowReadingsRoutine(currentPoint));
    }

    private IEnumerator ShowReadingsRoutine(RheostatPoint point)
    {
        completed = true;

        if (correctIcon) correctIcon.SetActive(true);
        if (wrongIcon) wrongIcon.SetActive(false);
        if (validateButton) validateButton.interactable = false;
        if (autoFillButton) autoFillButton.gameObject.SetActive(false);

        rheostat.SetInteraction(false);

        OnCorrectAnswer?.Invoke();

        // Unlock page navigation in PageNavigationController
        PageNavigationController.RequestNavigationUnlock();

        yield return null;
    }

    public void SetActivePoint(int index)
    {
        if (index >= 0 && index < points.Length)
            activePoint = index;
    }

    private void UpdateMeterDisplayVisibility(int currentPageIndex)
    {
        // Hide both by default
        SetAllMeterDisplaysActive(false);

        // Turn ON only the one corresponding to the active index
        if (points.Length > 0 && points[0].slidePageIndex == currentPageIndex)
        {
            if (meterDisplayParent1 != null) meterDisplayParent1.SetActive(true);
        }
        else if (points.Length > 1 && points[1].slidePageIndex == currentPageIndex)
        {
            if (meterDisplayParent2 != null) meterDisplayParent2.SetActive(true);
        }
    }

    public void ResetSlideUI()
    {
        completed = false;
        wrongAttempts = 0;

        if (rheostat != null)
            UpdateLiveDisplay(rheostat.NormalizedValue);

        if (correctIcon) correctIcon.SetActive(false);
        if (wrongIcon) wrongIcon.SetActive(false);
        if (validateButton) validateButton.interactable = true;
        if (autoFillButton) autoFillButton.gameObject.SetActive(false);

        if (rheostat != null)
        {
            rheostat.SetInteraction(true);
            Debug.Log($"[RheostatSlideController] Interaction ENABLED for slide index: {PageNavigationController.CurrentIndex}");
        }

        OnSlideReset?.Invoke();
    }

    private void SetAllMeterDisplaysActive(bool isActive)
    {
        if (meterDisplayParent1 != null) meterDisplayParent1.SetActive(isActive);
        if (meterDisplayParent2 != null) meterDisplayParent2.SetActive(isActive);
    }
}