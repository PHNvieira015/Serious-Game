using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

    [Header("Article Blocks")]
    public ArticleBlockView[] blockViews;

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

    private List<Block> blockComponents = new List<Block>();

    private void Start()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseFeedback);
        }

        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveAllListeners();
            verifyArticleButton.onClick.AddListener(VerifyArticle);
        }

        if (currentArticle != null)
            LoadArticle(currentArticle);
    }

    public void LoadArticle(ArticleData article)
    {
        if (article == null)
        {
            Debug.LogError("ArticleViewer: Article is null.");
            return;
        }

        currentArticle = article;

        DisplayArticleInfo();
        DisplayBlocks();

        HideFeedback();
    }

    private void DisplayArticleInfo()
    {
        if (titleText != null)
            titleText.text = currentArticle.Title;

        if (subtitleText != null)
            subtitleText.text = currentArticle.Subtitle;

        if (currentArticle.Writer != null)
        {
            if (authorNameText != null)
                authorNameText.text = currentArticle.Writer.WriterName;

            if (authorDescText != null)
                authorDescText.text = currentArticle.Writer.WriterDescription;

            if (authorPhotoImage != null)
                authorPhotoImage.sprite = currentArticle.Writer.WriterPhoto;
        }
    }

    private void DisplayBlocks()
    {
        if (currentArticle.Blocks == null || currentArticle.Blocks.Count == 0)
        {
            Debug.LogWarning("ArticleViewer: Article has no blocks.");
            return;
        }

        blockComponents.Clear();

        for (int i = 0; i < currentArticle.Blocks.Count && i < blockViews.Length; i++)
        {
            ArticleBlockView blockView = blockViews[i];
            ArticleBlock blockData = currentArticle.Blocks[i];

            if (blockView != null && blockData != null)
            {
                blockView.Initialize(blockData);
                blockView.gameObject.SetActive(true);

                Block blockComponent = blockView.GetComponent<Block>();
                if (blockComponent != null)
                {
                    blockComponent.Initialize(blockData);
                    blockComponent.OnBlockSolved += OnBlockSolved;
                    blockComponents.Add(blockComponent);
                }
            }
        }

        for (int i = currentArticle.Blocks.Count; i < blockViews.Length; i++)
        {
            if (blockViews[i] != null)
                blockViews[i].gameObject.SetActive(false);
        }
    }

    public void OnBlockSolved(Block block)
    {
        bool allSolved = true;
        foreach (var b in blockComponents)
        {
            if (!b.IsSolved && b.ArticleBlock.CheckType != CheckType.TrueCheck)
            {
                allSolved = false;
                break;
            }
        }

        if (allSolved)
        {
            OnAllBlocksSolved();
        }
    }

    private void OnAllBlocksSolved()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
            if (feedbackText != null)
            {
                feedbackText.text = "All blocks verified correctly!";
                feedbackText.ForceMeshUpdate();
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(50);
        }
    }

    public void VerifyArticle()
    {
        if (currentArticle == null)
            return;

        int correct = 0;
        int wrong = 0;
        int missed = 0;
        int score = 0;
        StringBuilder results = new StringBuilder();

        results.AppendLine("ARTICLE RESULTS");
        results.AppendLine();

        for (int i = 0; i < blockViews.Length; i++)
        {
            ArticleBlockView view = blockViews[i];

            if (view == null || view.Block == null)
                continue;

            ArticleBlock block = view.Block;

            if (block.CheckType == CheckType.None)
                continue;

            if (block.CheckType == CheckType.TrueCheck)
            {
                if (!view.IsMarked)
                    continue;

                wrong++;
                score -= penaltyPerWrong;
                results.AppendLine($"Block {i + 1}: WRONG");
                results.AppendLine("This block was true and should not have been marked.");
                if (!string.IsNullOrEmpty(block.Explanation))
                    results.AppendLine(block.Explanation);
                results.AppendLine();
                continue;
            }

            if (!view.IsMarked)
            {
                missed++;
                score -= penaltyPerMissed;
                results.AppendLine($"Block {i + 1}: MISSED");
                results.AppendLine($"Correct check: {GetCheckTypeName(block.CheckType)}");
                if (!string.IsNullOrEmpty(block.Explanation))
                    results.AppendLine(block.Explanation);
                results.AppendLine();
                continue;
            }

            CheckType playerChoice = view.PlayerCheckType;

            if (playerChoice == block.CheckType)
            {
                correct++;
                score += pointsPerCorrect;
                results.AppendLine($"Block {i + 1}: CORRECT");
                results.AppendLine($"Check: {GetCheckTypeName(block.CheckType)}");
                if (!string.IsNullOrEmpty(block.Explanation))
                    results.AppendLine(block.Explanation);
                results.AppendLine();
            }
            else
            {
                wrong++;
                score -= penaltyPerWrong;
                results.AppendLine($"Block {i + 1}: WRONG");
                results.AppendLine($"Your check: {GetCheckTypeName(playerChoice)}");
                results.AppendLine($"Correct check: {GetCheckTypeName(block.CheckType)}");
                if (!string.IsNullOrEmpty(block.Explanation))
                    results.AppendLine(block.Explanation);
                results.AppendLine();
            }
        }

        results.AppendLine("--------------------------------");
        results.AppendLine("SUMMARY");
        results.AppendLine();
        results.AppendLine($"Correct: {correct}");
        results.AppendLine($"Wrong: {wrong}");
        results.AppendLine($"Missed: {missed}");
        results.AppendLine($"Score: {score}");

        ShowFeedback(results.ToString());
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.ForceMeshUpdate();
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);
    }

    private void CloseFeedback()
    {
        HideFeedback();
    }

    private void HideFeedback()
    {
        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);
    }

    public string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.TrueCheck: return "True Check";
            case CheckType.LabelCheck: return "Label Check";
            case CheckType.SourceCheck: return "Source Check";
            case CheckType.AICheck: return "AI Check";
            case CheckType.SpecialistCheck: return "Specialist Check";
            case CheckType.FalacyCheck: return "Fallacy Check";
            case CheckType.None:
            default: return "None";
        }
    }
}