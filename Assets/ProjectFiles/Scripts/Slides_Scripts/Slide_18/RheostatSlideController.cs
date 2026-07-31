using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("UI Controls")]
    [SerializeField] private Button validateButton;
    [SerializeField] private GameObject correctIcon;
    [SerializeField] private GameObject wrongIcon;

    private bool completed = false;
    private bool isAtAnswerPoint = false;
    private int currentVoltmeterDisplay = 0;
    private int currentGalvanometerDisplay = 0;

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;

        if (rheostat != null)
            rheostat.OnValueChanged += OnRheostatValueChanged;
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

        ResetSlideUI();
    }

    private void OnPageChanged(int pageIndex)
    {
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i].slidePageIndex == pageIndex)
            {
                SetActivePoint(i);
                ResetSlideUI();
                return;
            }
        }

        // pageIndex isn't one of our answer-tracked slides (e.g. not 18 or 23) -
        // make sure nothing is left interactable or displayed from a previous slide.
        if (correctIcon) correctIcon.SetActive(false);
        if (wrongIcon) wrongIcon.SetActive(false);
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

        // Correct only while the handle is actually parked on this page's
        // answer point (the override display is active) - not just whenever
        // the continuous sweep happens to pass through the right number.
        if (isAtAnswerPoint)
        {
            StartCoroutine(ShowReadingsRoutine(currentPoint));
        }
        else
        {
            if (wrongIcon) wrongIcon.SetActive(true);
        }
    }

    private IEnumerator ShowReadingsRoutine(RheostatPoint point)
    {
        completed = true;

        // Meters are already showing the exact correct values via the
        // answer-point override; lock them in place and unlock navigation.
        if (correctIcon) correctIcon.SetActive(true);
        if (validateButton) validateButton.interactable = false;

        rheostat.SetInteraction(false);

        // Unlock next page in PageNavigationController
        PageNavigationController.RequestNavigationUnlock();

        yield return null;
    }

    public void SetActivePoint(int index)
    {
        if (index >= 0 && index < points.Length)
            activePoint = index;
    }

    public void ResetSlideUI()
    {
        completed = false;

        if (rheostat != null)
            UpdateLiveDisplay(rheostat.NormalizedValue);

        if (correctIcon) correctIcon.SetActive(false);
        if (wrongIcon) wrongIcon.SetActive(false);
        if (validateButton) validateButton.interactable = true;

        rheostat.SetInteraction(true);
    }
}
