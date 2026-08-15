using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ArticleViewer : MonoBehaviour
{
    [Header("Article Data")]
    public ArticleData currentArticle;
    public int currentBlockIndex = 0;

    [Header("UI Elements")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text authorNameText;
    public TMP_Text authorDescText;
    public Image authorPhotoImage;

    [Header("Block Content")]
    public TMP_Text blockContentText;
    public Image blockImage;
    public GameObject imageContainer;
    public GameObject textContainer;

    [Header("Dynamic Image Layout")]
    public LayoutElement imageLayoutElement;
    public RectTransform articleContent;

    [Header("Block Navigation")]
    public TMP_Text blockCounterText;
    public Button nextButton;
    public Button previousButton;

    [Header("Check System")]
    public GameObject checkPanel;
    public TMP_Text hintText;
    public Button labelCheckButton;
    public Button sourceCheckButton;
    public Button aiCheckButton;
    public Button specialistCheckButton;
    public Button falacyCheckButton;

    [Header("Feedback")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public Button continueButton;

    private Dictionary<CheckType, Button> checkButtons;

    void Start()
    {
        SetupButtons();
        LoadArticle(currentArticle);
    }

    // ============================================================
    // BUTTON SETUP
    // ============================================================

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

    // ============================================================
    // ARTICLE
    // ============================================================

    public void LoadArticle(ArticleData article)
    {
        if (article == null)
        {
            Debug.LogError("ArticleViewer: Article is null.");
            return;
        }

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

        // Make sure TMP recalculates its preferred size.
        titleText.ForceMeshUpdate();
        subtitleText.ForceMeshUpdate();
        authorNameText.ForceMeshUpdate();
        authorDescText.ForceMeshUpdate();

        ForceLayoutUpdate();
    }

    // ============================================================
    // BLOCK DISPLAY
    // ============================================================

    void DisplayCurrentBlock()
    {
        if (currentArticle == null)
            return;

        if (currentArticle.Bloco == null || currentArticle.Bloco.Count == 0)
        {
            Debug.LogWarning("ArticleViewer: Article has no blocks.");
            return;
        }

        if (currentBlockIndex >= currentArticle.Bloco.Count)
            return;

        ArticleBlock block = currentArticle.Bloco[currentBlockIndex];

        // --------------------------------------------------------
        // IMAGE BLOCK
        // --------------------------------------------------------

        if (block.Type == BlockType.Image && block.Image != null)
        {
            textContainer.SetActive(false);
            imageContainer.SetActive(true);

            blockImage.sprite = block.Image;

            // Preserve the image's aspect ratio.
            StartCoroutine(UpdateImageLayout(block.Image));
        }

        // --------------------------------------------------------
        // TEXT BLOCK
        // --------------------------------------------------------

        else
        {
            imageContainer.SetActive(false);
            textContainer.SetActive(true);

            blockContentText.text = block.Text;

            // Tell TMP to update its preferred size.
            blockContentText.ForceMeshUpdate();

            // Rebuild after changing the text.
            StartCoroutine(RefreshLayoutNextFrame());
        }

        // --------------------------------------------------------
        // COUNTER
        // --------------------------------------------------------

        blockCounterText.text =
            $"{currentBlockIndex + 1} / {currentArticle.Bloco.Count}";

        // --------------------------------------------------------
        // CHECK PANEL
        // --------------------------------------------------------

        UpdateCheckPanel(block);

        // --------------------------------------------------------
        // NAVIGATION
        // --------------------------------------------------------

        previousButton.interactable = currentBlockIndex > 0;

        nextButton.interactable =
            currentBlockIndex < currentArticle.Bloco.Count - 1;

        // --------------------------------------------------------
        // FEEDBACK
        // --------------------------------------------------------

        feedbackPanel.SetActive(false);
        checkPanel.SetActive(true);
    }

    // ============================================================
    // IMAGE LAYOUT
    // ============================================================

    IEnumerator UpdateImageLayout(Sprite sprite)
    {
        // Wait for Unity to update the activated container.
        yield return null;

        if (sprite == null)
            yield break;

        RectTransform imageContainerRect =
            imageContainer.GetComponent<RectTransform>();

        if (imageContainerRect == null)
            yield break;

        // Make sure the layout has been calculated first.
        Canvas.ForceUpdateCanvases();

        float imageWidth = imageContainerRect.rect.width;

        if (imageWidth <= 0)
        {
            Debug.LogWarning(
                "ArticleViewer: Image container has no width."
            );

            yield break;
        }

        // Calculate aspect ratio from the sprite.
        float aspectRatio =
            (float)sprite.rect.width / sprite.rect.height;

        // Calculate the required height.
        float imageHeight = imageWidth / aspectRatio;

        // Tell the Vertical Layout Group how much space the image needs.
        if (imageLayoutElement != null)
        {
            imageLayoutElement.preferredHeight = imageHeight;
        }

        // Also make sure the Image itself has the correct size.
        RectTransform imageRect = blockImage.rectTransform;

        imageRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            imageHeight
        );

        ForceLayoutUpdate();
    }

    // ============================================================
    // LAYOUT REFRESH
    // ============================================================

    IEnumerator RefreshLayoutNextFrame()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (blockContentText != null)
            blockContentText.ForceMeshUpdate();

        ForceLayoutUpdate();
    }

    void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();

        if (articleContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                articleContent
            );
        }
    }

    // ============================================================
    // CHECK PANEL
    // ============================================================

    void UpdateCheckPanel(ArticleBlock block)
    {
        if (block.IsFalseInformation &&
            !string.IsNullOrEmpty(block.Hint))
        {
            hintText.text = block.Hint;
            hintText.gameObject.SetActive(true);

            hintText.ForceMeshUpdate();
        }
        else
        {
            hintText.gameObject.SetActive(false);
        }

        // Enable all check buttons.
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = true;
        }

        ForceLayoutUpdate();
    }

    // ============================================================
    // CHECK SELECTION
    // ============================================================

    void OnCheckSelected(CheckType selectedCheck)
    {
        ArticleBlock currentBlock =
            currentArticle.Bloco[currentBlockIndex];

        // Block is actually true.
        if (!currentBlock.IsFalseInformation)
        {
            ShowFeedback(
                "This information appears to be correct.\nNo issues found.",
                false
            );

            return;
        }

        // Get explanation for selected check.
        string explanation =
            currentBlock.GetExplanationForCheck(selectedCheck);

        // Player selected the correct check.
        if (selectedCheck == currentBlock.CorrectCheckType)
        {
            ShowFeedback(
                $"CORRECT! {explanation}",
                true
            );
        }

        // Player selected the wrong check.
        else
        {
            string correctExplanation =
                currentBlock.GetExplanationForCheck(
                    currentBlock.CorrectCheckType
                );

            string correctName =
                GetCheckTypeName(
                    currentBlock.CorrectCheckType
                );

            ShowFeedback(
                $"WRONG. {explanation}\n\n" +
                $"The correct check is {correctName}: " +
                $"{correctExplanation}",
                false
            );
        }
    }

    // ============================================================
    // FEEDBACK
    // ============================================================

    void ShowFeedback(string message, bool isCorrect)
    {
        feedbackText.text = message;

        feedbackPanel.SetActive(true);
        checkPanel.SetActive(false);

        feedbackText.ForceMeshUpdate();

        // Disable check buttons while feedback is showing.
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = false;
        }

        // Rebuild feedback layout.
        StartCoroutine(RefreshFeedbackLayout());

        // Score / penalty logic.
        if (isCorrect)
        {
            Debug.Log("+10 points");
        }
        else
        {
            Debug.Log("-5 points");
        }
    }

    IEnumerator RefreshFeedbackLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        feedbackText.ForceMeshUpdate();

        ForceLayoutUpdate();
    }

    void CloseFeedback()
    {
        feedbackPanel.SetActive(false);
        checkPanel.SetActive(true);

        // Re-enable check buttons.
        foreach (var btn in checkButtons.Values)
        {
            btn.interactable = true;
        }

        ForceLayoutUpdate();
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    void NextBlock()
    {
        if (currentBlockIndex <
            currentArticle.Bloco.Count - 1)
        {
            currentBlockIndex++;

            DisplayCurrentBlock();
        }
        else
        {
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

    // ============================================================
    // ARTICLE COMPLETE
    // ============================================================

    void OnArticleComplete()
    {
        ShowFeedback(
            "Article review complete!\nProceed to next article.",
            true
        );

        continueButton.onClick.RemoveListener(CloseFeedback);
        continueButton.onClick.AddListener(LoadNextArticle);
    }

    void LoadNextArticle()
    {
        // Load your next article here.
        Debug.Log("Loading next article...");
    }

    // ============================================================
    // CHECK TYPE NAME
    // ============================================================

    string GetCheckTypeName(CheckType checkType)
    {
        switch (checkType)
        {
            case CheckType.LabelCheck:
                return "Label Check";

            case CheckType.SourceCheck:
                return "Source Check";

            case CheckType.AICheck:
                return "AI Check";

            case CheckType.SpecialistCheck:
                return "Specialist Check";

            case CheckType.FalacyCheck:
                return "Falacy Check";

            default:
                return "None";
        }
    }
}