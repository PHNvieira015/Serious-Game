using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ArticleViewer : MonoBehaviour
{
    [Header("Article Data")]
    public ArticleData currentArticle;
    public int currentBlockIndex = 0;

    [Header("UI Elements")]
    public Text titleText;
    public Text subtitleText;
    public Text authorNameText;
    public Text authorDescText;
    public Image authorPhotoImage;

    public Text blockContentText;
    public Image blockImage;
    public GameObject imageContainer;
    public GameObject textContainer;

    [Header("Block Navigation")]
    public Text blockCounterText;
    public Button nextButton;
    public Button previousButton;

    [Header("Check System")]
    public GameObject checkPanel;
    public Text hintText;
    public Button labelCheckButton;
    public Button sourceCheckButton;
    public Button aiCheckButton;
    public Button specialistCheckButton;
    public Button falacyCheckButton;

    [Header("Feedback")]
    public GameObject feedbackPanel;
    public Text feedbackText;
    public Button continueButton;

    private Dictionary<CheckType, Button> checkButtons;

    void Start()
    {
        SetupButtons();
        LoadArticle(currentArticle);
    }

    void SetupButtons()
    {
        checkButtons = new Dictionary<CheckType, Button>
        {
            { CheckType.LabelCheck, labelCheckButton },
            { CheckType.SourceCheck, sourceCheckButton },
            { CheckType.AICheck, aiCheckButton },
            { CheckType.SpecialistCheck, specialistCheckButton },
            { CheckType.FalacyCheck, falacyCheckButton }
        };

        foreach (var btn in checkButtons)
        {
            CheckType type = btn.Key;
            btn.Value.onClick.AddListener(() => OnCheckSelected(type));
        }

        nextButton.onClick.AddListener(NextBlock);
        previousButton.onClick.AddListener(PreviousBlock);
        continueButton.onClick.AddListener(CloseFeedback);
    }

    public void LoadArticle(ArticleData article)
    {
        currentArticle = article;
        currentBlockIndex = 0;
        DisplayArticleInfo();
        DisplayCurrentBlock();
    }

    void DisplayArticleInfo()
    {
        titleText.text = currentArticle.Titulo;
        subtitleText.text = currentArticle.Subtitulo;
        authorNameText.text = currentArticle.Autor.NomeAutor;
        authorDescText.text = currentArticle.Autor.DescriçãoAutor;
        authorPhotoImage.sprite = currentArticle.Autor.FotoAutor;
    }

    void DisplayCurrentBlock()
    {
        if (currentBlockIndex >= currentArticle.Bloco.Count)
            return;

        ArticleBlock block = currentArticle.Bloco[currentBlockIndex];

        // Display block content
        if (block.Type == BlockType.Image && block.Image != null)
        {
            textContainer.SetActive(false);
            imageContainer.SetActive(true);
            blockImage.sprite = block.Image;
        }
        else
        {
            textContainer.SetActive(true);
            imageContainer.SetActive(false);
            blockContentText.text = block.Text;
        }

        // Update counter
        blockCounterText.text = $"{currentBlockIndex + 1} / {currentArticle.Bloco.Count}";

        // Update check panel
        UpdateCheckPanel(block);

        // Update navigation buttons
        previousButton.interactable = currentBlockIndex > 0;
        nextButton.interactable = currentBlockIndex < currentArticle.Bloco.Count - 1;

        // Close feedback panel when changing blocks
        feedbackPanel.SetActive(false);
        checkPanel.SetActive(true);
    }

    void UpdateCheckPanel(ArticleBlock block)
    {
        if (block.IsFalseInformation && !string.IsNullOrEmpty(block.Hint))
        {
            hintText.text = block.Hint;
            hintText.gameObject.SetActive(true);
        }
        else
        {
            hintText.gameObject.SetActive(false);
        }

        // Enable all check buttons
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = true;
        }
    }

    void OnCheckSelected(CheckType selectedCheck)
    {
        ArticleBlock currentBlock = currentArticle.Bloco[currentBlockIndex];

        if (!currentBlock.IsFalseInformation)
        {
            // Block is actually true
            ShowFeedback(
                "This information appears to be correct.\nNo issues found.",
                false
            );
            return;
        }

        // Get the explanation for what the player selected
        string explanation = currentBlock.GetExplanationForCheck(selectedCheck);

        if (selectedCheck == currentBlock.CorrectCheckType)
        {
            // Player was correct
            ShowFeedback(
                $"CORRECT! {explanation}",
                true
            );
        }
        else
        {
            // Player was wrong
            string correctExplanation = currentBlock.GetExplanationForCheck(currentBlock.CorrectCheckType);
            string correctName = GetCheckTypeName(currentBlock.CorrectCheckType);

            ShowFeedback(
                $"WRONG. {explanation}\n\n" +
                $"The correct check is {correctName}: {correctExplanation}",
                false
            );
        }
    }

    void ShowFeedback(string message, bool isCorrect)
    {
        feedbackText.text = message;
        feedbackPanel.SetActive(true);
        checkPanel.SetActive(false);

        // Disable check buttons while feedback is showing
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = false;
        }

        // You can add score/penalty logic here
        if (isCorrect)
        {
            // Award points
            Debug.Log("+10 points");
        }
        else
        {
            // Apply penalty
            Debug.Log("-5 points");
        }
    }

    void CloseFeedback()
    {
        feedbackPanel.SetActive(false);
        checkPanel.SetActive(true);

        // Re-enable check buttons for next block
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = true;
        }
    }

    void NextBlock()
    {
        if (currentBlockIndex < currentArticle.Bloco.Count - 1)
        {
            currentBlockIndex++;
            DisplayCurrentBlock();
        }
        else
        {
            // Article complete
            OnArticleComplete();
        }
    }

    void PreviousBlock()
    {
        if (currentBlockIndex > 0)
        {
            currentBlockIndex--;
            DisplayCurrentBlock();
        }
    }

    void OnArticleComplete()
    {
        ShowFeedback("Article review complete!\nProceed to next article.", true);
        continueButton.onClick.RemoveListener(CloseFeedback);
        continueButton.onClick.AddListener(LoadNextArticle);
    }

    void LoadNextArticle()
    {
        // Load your next article here
        Debug.Log("Loading next article...");
    }

    string GetCheckTypeName(CheckType checkType)
    {
        switch (checkType)
        {
            case CheckType.LabelCheck: return "Label Check";
            case CheckType.SourceCheck: return "Source Check";
            case CheckType.AICheck: return "AI Check";
            case CheckType.SpecialistCheck: return "Specialist Check";
            case CheckType.FalacyCheck: return "Falacy Check";
            default: return "None";
        }
    }
}