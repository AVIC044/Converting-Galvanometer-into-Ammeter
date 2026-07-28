using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Question7AnswerController : MonoBehaviour
{
    [Header("Button Panel")]
    [SerializeField] private GameObject buttonPanel;

    [Header("Buttons")]
    [SerializeField] private List<Button> answerButtons = new();

    [Header("Sprites")]
    [SerializeField] private List<GameObject> answerSprites = new();

    [SerializeField] private int activePageIndex = 6; // 7th page

    private void OnEnable()
    {
        PageNavigationController.OnPageChanged += OnPageChanged;
    }

    private void OnDisable()
    {
        PageNavigationController.OnPageChanged -= OnPageChanged;
    }

    private void Start()
    {
        // Hide panel initially
        buttonPanel.SetActive(false);

        // Hide all answer sprites
        foreach (GameObject sprite in answerSprites)
            sprite.SetActive(false);

        // Register button clicks
        for (int i = 0; i < answerButtons.Count; i++)
        {
            int index = i;
            answerButtons[i].onClick.AddListener(() => OnAnswerClicked(index));
        }

        // Set correct state based on current page
        OnPageChanged(PageNavigationController.CurrentIndex);
    }

    private void OnPageChanged(int pageIndex)
    {
        buttonPanel.SetActive(pageIndex == activePageIndex);
    }

    private void OnAnswerClicked(int index)
{
    if (PageNavigationController.CurrentIndex != activePageIndex)
        return;

    // Hide the clicked button
    answerButtons[index].gameObject.SetActive(false);

    // Show corresponding sprite
    answerSprites[index].SetActive(true);

    // If the correct button (Button 1 / Index 0) is clicked
    if (index == 0)
    {
        // Disable all other buttons
        for (int i = 1; i < answerButtons.Count; i++)
        {
            answerButtons[i].interactable = false;
        }

        // Unlock next page if required
        PageNavigationController.RequestNavigationUnlock();
    }
}
}