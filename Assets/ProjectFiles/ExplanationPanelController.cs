using UnityEngine;
using UnityEngine.UI;

public class ExplanationPanelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button explanationButton;
    [SerializeField] private GameObject explanationPanel;

    private void Start()
    {
        if (explanationButton != null)
            explanationButton.interactable = false;

        if (explanationPanel != null)
            explanationPanel.SetActive(false);
    }

    // Call from OnAllItemsPlaced event
    public void EnableExplanationButton()
    {
        if (explanationButton != null)
            explanationButton.interactable = true;
    }

    // Assign to Explanation Button OnClick
    public void OpenExplanationPanel()
    {
        if (explanationPanel != null)
            explanationPanel.SetActive(true);
    }

    // Assign to Continue Button OnClick
    public void CloseExplanationPanel()
    {
        if (explanationPanel != null)
            explanationPanel.SetActive(false);
    }
}