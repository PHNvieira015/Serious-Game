using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class ArticleViewer : MonoBehaviour
{
    [Header("Article Data")]
    public ArticleData currentArticle;

    [Header("Article Header")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text authorNameText;
    public TMP_Text authorDescText;
    public Image authorPhotoImage;

    [Header("Article Content")]
    [Tooltip("The Content transform inside your ScrollView.")]
    public Transform articleContent;

    [Tooltip("Prefab containing ArticleBlockView.")]
    public ArticleBlockView articleBlockPrefab;

    [Header("Check Panel")]
    public GameObject checkPanel;
    public TMP_Text hintText;

    [Header("Check Buttons")]
    public Button labelCheckButton;
    public Button sourceCheckButton;
    public Button aiCheckButton;
    public Button specialistCheckButton;
    public Button falacyCheckButton;

    [Header("Article Verification")]
    public Button verifyArticleButton;

    [Header("Feedback")]
    public GameObject feedbackPanel;
    public TMP_Text feedbackText;
    public Button continueButton;

    [Header("Scoring")]
    public int pointsPerCorrect = 10;
    public int penaltyPerWrong = 5;
    public int penaltyPerMissed = 5;

    private Dictionary<CheckType, Button> checkButtons;

    private readonly List<ArticleBlockView> blockViews =
        new List<ArticleBlockView>();

    private ArticleBlockView currentlySelectedBlock;

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        SetupButtons();

        if (currentArticle != null)
            LoadArticle(currentArticle);
    }

    // ============================================================
    // BUTTON SETUP
    // ============================================================

    private void SetupButtons()
    {
        checkButtons = new Dictionary<CheckType, Button>
        {
            { CheckType.LabelCheck, labelCheckButton },
            { CheckType.SourceCheck, sourceCheckButton },
            { CheckType.AICheck, aiCheckButton },
            { CheckType.SpecialistCheck, specialistCheckButton },
            { CheckType.FalacyCheck, falacyCheckButton }
        };

        foreach (var pair in checkButtons)
        {
            CheckType type = pair.Key;
            Button button = pair.Value;

            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(
                () => OnCheckSelected(type)
            );
        }

        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveAllListeners();

            verifyArticleButton.onClick.AddListener(
                VerifyArticle
            );
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();

            continueButton.onClick.AddListener(
                CloseFeedback
            );
        }
    }

    // ============================================================
    // LOAD ARTICLE
    // ============================================================

    public void LoadArticle(ArticleData article)
    {
        if (article == null)
        {
            Debug.LogError(
                "ArticleViewer: Article is null."
            );

            return;
        }

        currentArticle = article;
        currentlySelectedBlock = null;

        ClearArticleContent();

        DisplayArticleInfo();
        BuildArticleBlocks();

        HideCheckPanel();
        HideFeedback();
    }

    // ============================================================
    // ARTICLE HEADER
    // ============================================================

    private void DisplayArticleInfo()
    {
        if (titleText != null)
            titleText.text = currentArticle.Title;

        if (subtitleText != null)
            subtitleText.text = currentArticle.Subtitle;

        if (currentArticle.Writer != null)
        {
            if (authorNameText != null)
            {
                authorNameText.text =
                    currentArticle.Writer.WriterName;
            }

            if (authorDescText != null)
            {
                authorDescText.text =
                    currentArticle.Writer.WriterDescription;
            }

            if (authorPhotoImage != null)
            {
                authorPhotoImage.sprite =
                    currentArticle.Writer.WriterPhoto;
            }
        }

        ForceLayoutUpdate();
    }

    // ============================================================
    // BUILD WHOLE ARTICLE
    // ============================================================

    private void BuildArticleBlocks()
    {
        if (articleContent == null)
        {
            Debug.LogError(
                "ArticleViewer: Article Content is not assigned."
            );

            return;
        }

        if (articleBlockPrefab == null)
        {
            Debug.LogError(
                "ArticleViewer: Article Block Prefab is not assigned."
            );

            return;
        }

        if (currentArticle.Blocks == null)
        {
            Debug.LogWarning(
                "ArticleViewer: Article has no blocks."
            );

            return;
        }

        foreach (ArticleBlock block in currentArticle.Blocks)
        {
            if (block == null)
                continue;

            ArticleBlockView blockView =
                Instantiate(
                    articleBlockPrefab,
                    articleContent
                );

            blockView.Initialize(
                block,
                this
            );

            blockViews.Add(blockView);
        }

        StartCoroutine(
            RefreshArticleLayout()
        );
    }

    // ============================================================
    // CLEAR ARTICLE
    // ============================================================

    private void ClearArticleContent()
    {
        currentlySelectedBlock = null;

        foreach (ArticleBlockView view in blockViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        blockViews.Clear();

        if (articleContent == null)
            return;

        // Remove old block instances.
        for (int i = articleContent.childCount - 1; i >= 0; i--)
        {
            Destroy(
                articleContent.GetChild(i).gameObject
            );
        }
    }

    // ============================================================
    // SELECT BLOCK
    // ============================================================

    public void SelectBlock(ArticleBlockView blockView)
    {
        if (blockView == null)
            return;

        currentlySelectedBlock = blockView;

        ShowCheckPanel(blockView);
    }

    // ============================================================
    // SHOW CHECK PANEL
    // ============================================================

    private void ShowCheckPanel(
        ArticleBlockView blockView)
    {
        if (checkPanel != null)
            checkPanel.SetActive(true);

        ArticleBlock block = blockView.Block;

        if (hintText != null)
        {
            hintText.text = block.Hint;

            hintText.gameObject.SetActive(
                !string.IsNullOrEmpty(block.Hint)
            );

            hintText.ForceMeshUpdate();
        }

        foreach (Button button in checkButtons.Values)
        {
            if (button != null)
                button.interactable = true;
        }

        ForceLayoutUpdate();
    }

    // ============================================================
    // PLAYER CHOOSES A CHECK
    // ============================================================

    private void OnCheckSelected(CheckType selectedCheck)
    {
        if (currentlySelectedBlock == null)
            return;

        currentlySelectedBlock.SetPlayerCheckType(
            selectedCheck
        );

        HideCheckPanel();
    }

    // ============================================================
    // VERIFY ENTIRE ARTICLE
    // ============================================================

    public void VerifyArticle()
    {
        if (currentArticle == null)
            return;

        int correct = 0;
        int wrong = 0;
        int missed = 0;

        int score = 0;

        StringBuilder results =
            new StringBuilder();

        results.AppendLine("ARTICLE RESULTS");
        results.AppendLine();

        for (int i = 0; i < blockViews.Count; i++)
        {
            ArticleBlockView view = blockViews[i];

            if (view == null || view.Block == null)
                continue;

            ArticleBlock block = view.Block;

            // ====================================================
            // NONE
            // ====================================================
            //
            // None means the block is empty / unused.
            // It has no verification meaning at all.
            //

            if (block.CheckType == CheckType.None)
            {
                continue;
            }

            // ====================================================
            // TRUE CHECK
            // ====================================================
            //
            // TrueCheck means:
            //
            // "This block is true."
            //
            // The player does NOT need to mark it.
            //

            if (block.CheckType == CheckType.TrueCheck)
            {
                if (!view.IsMarked)
                {
                    // Correct behavior:
                    // player left a true block alone.
                    continue;
                }

                // Player incorrectly marked a true block.
                wrong++;
                score -= penaltyPerWrong;

                results.AppendLine(
                    $"Block {i + 1}: WRONG"
                );

                results.AppendLine(
                    "This block was true and should not have been marked."
                );

                if (!string.IsNullOrEmpty(block.Explanation))
                {
                    results.AppendLine(
                        block.Explanation
                    );
                }

                results.AppendLine();

                continue;
            }

            // ====================================================
            // FALSE / PROBLEMATIC BLOCK
            // ====================================================
            //
            // Any CheckType other than None or TrueCheck means
            // the player is expected to find this block.
            //

            if (!view.IsMarked)
            {
                // Player failed to identify it.
                missed++;
                score -= penaltyPerMissed;

                results.AppendLine(
                    $"Block {i + 1}: MISSED"
                );

                results.AppendLine(
                    $"Correct check: " +
                    $"{GetCheckTypeName(block.CheckType)}"
                );

                if (!string.IsNullOrEmpty(block.Explanation))
                {
                    results.AppendLine(
                        block.Explanation
                    );
                }

                results.AppendLine();

                continue;
            }

            // ====================================================
            // PLAYER MARKED THE BLOCK
            // ====================================================

            CheckType playerChoice =
                view.PlayerCheckType;

            if (playerChoice == block.CheckType)
            {
                // Correct block AND correct check.
                correct++;
                score += pointsPerCorrect;

                results.AppendLine(
                    $"Block {i + 1}: CORRECT"
                );

                results.AppendLine(
                    $"Check: " +
                    $"{GetCheckTypeName(block.CheckType)}"
                );

                if (!string.IsNullOrEmpty(block.Explanation))
                {
                    results.AppendLine(
                        block.Explanation
                    );
                }

                results.AppendLine();
            }
            else
            {
                // Correctly or incorrectly identified as something
                // suspicious, but chose the wrong verification type.
                wrong++;
                score -= penaltyPerWrong;

                results.AppendLine(
                    $"Block {i + 1}: WRONG"
                );

                results.AppendLine(
                    $"Your check: " +
                    $"{GetCheckTypeName(playerChoice)}"
                );

                results.AppendLine(
                    $"Correct check: " +
                    $"{GetCheckTypeName(block.CheckType)}"
                );

                if (!string.IsNullOrEmpty(block.Explanation))
                {
                    results.AppendLine(
                        block.Explanation
                    );
                }

                results.AppendLine();
            }
        }

        // ========================================================
        // SUMMARY
        // ========================================================

        results.AppendLine(
            "--------------------------------"
        );

        results.AppendLine("SUMMARY");
        results.AppendLine();

        results.AppendLine(
            $"Correct: {correct}"
        );

        results.AppendLine(
            $"Wrong: {wrong}"
        );

        results.AppendLine(
            $"Missed: {missed}"
        );

        results.AppendLine(
            $"Score: {score}"
        );

        ShowFeedback(
            results.ToString()
        );
    }

    // ============================================================
    // FEEDBACK
    // ============================================================

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.ForceMeshUpdate();
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        HideCheckPanel();

        StartCoroutine(
            RefreshFeedbackLayout()
        );
    }

    private IEnumerator RefreshFeedbackLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (feedbackText != null)
            feedbackText.ForceMeshUpdate();

        ForceLayoutUpdate();
    }

    private void CloseFeedback()
    {
        HideFeedback();

        ForceLayoutUpdate();
    }

    // ============================================================
    // HIDE CHECK PANEL
    // ============================================================

    private void HideCheckPanel()
    {
        if (checkPanel != null)
            checkPanel.SetActive(false);

        currentlySelectedBlock = null;
    }

    // ============================================================
    // HIDE FEEDBACK
    // ============================================================

    private void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    // ============================================================
    // CHECK TYPE NAME
    // ============================================================

    public string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.TrueCheck:
                return "True Check";

            case CheckType.LabelCheck:
                return "Label Check";

            case CheckType.SourceCheck:
                return "Source Check";

            case CheckType.AICheck:
                return "AI Check";

            case CheckType.SpecialistCheck:
                return "Specialist Check";

            case CheckType.FalacyCheck:
                return "Fallacy Check";

            case CheckType.None:
            default:
                return "None";
        }
    }

    // ============================================================
    // LAYOUT
    // ============================================================

    private IEnumerator RefreshArticleLayout()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        ForceLayoutUpdate();
    }

    private void ForceLayoutUpdate()
    {
        Canvas.ForceUpdateCanvases();

        if (articleContent is RectTransform rect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                rect
            );
        }
    }
}