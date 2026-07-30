using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class Question6WhyController : MonoBehaviour
{
    [Header("Question Settings")]
    [SerializeField] private int questionPageIndex = 5;

    [Header("UI References")]
    [SerializeField] private Button whyButton;
    [SerializeField] private GameObject explanationPanel;

    [Header("Placed Sprites")]
    [SerializeField] private GameObject vSprite;
    [SerializeField] private GameObject gSprite;

    [Header("Events")]
    [SerializeField] private UnityEvent onBothDragsCompleted;

    private bool drag1Completed;
    private bool drag2Completed;
    private bool eventInvoked;

    private void Awake()
    {
        if (whyButton != null)
        {
            whyButton.gameObject.SetActive(false);
            whyButton.onClick.AddListener(OpenExplanationPanel);
        }

        if (explanationPanel != null)
            explanationPanel.SetActive(false);

        if (vSprite != null)
            vSprite.SetActive(false);

        if (gSprite != null)
            gSprite.SetActive(false);
    }

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;
    }

   private void OnPageChanged(int pageIndex)
{
    if (explanationPanel != null)
        explanationPanel.SetActive(false);

    // Once enabled, keep them visible on every page.
    if (vSprite != null)
        vSprite.SetActive(drag1Completed);

    if (gSprite != null)
        gSprite.SetActive(drag2Completed);

    UpdateWhyButton();
}

    public void Drag1Completed()
    {
        if (drag1Completed)
            return;

        drag1Completed = true;

        if (vSprite != null)
            vSprite.SetActive(true);

        CheckCompleted();

        UpdateWhyButton();
    }

    public void Drag2Completed()
    {
        if (drag2Completed)
            return;

        drag2Completed = true;

        if (gSprite != null)
            gSprite.SetActive(true);

        CheckCompleted();

        UpdateWhyButton();
    }

    private void CheckCompleted()
    {
        if (eventInvoked)
            return;

        if (drag1Completed && drag2Completed)
        {
            eventInvoked = true;

            Debug.Log("Question 6 Completed");

            onBothDragsCompleted?.Invoke();
        }
    }

    private void UpdateWhyButton()
    {
        if (whyButton == null)
            return;

        whyButton.gameObject.SetActive(
            PageNavigationController.CurrentIndex == questionPageIndex &&
            drag1Completed &&
            drag2Completed);
    }

    public void OpenExplanationPanel()
    {
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
    }

    public void CloseExplanationPanel()
    {
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
    }
}