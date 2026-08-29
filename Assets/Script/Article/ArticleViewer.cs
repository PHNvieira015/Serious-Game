using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

    [Header("Validator")]
    public ArticleValidator articleValidator;

    [Header("Scoring")]
    public int pointsPerCorrect = 10;
    public int penaltyPerWrong = 5;
    public int penaltyPerMissed = 5;

    private List<Block> blockComponents = new List<Block>();

    private void Start()
    {
        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveAllListeners();
            verifyArticleButton.onClick.AddListener(OnVerifyButtonPressed);
        }

        if (currentArticle != null)
        {
            LoadArticle(currentArticle);
        }
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

        // Find validator if not assigned
        if (articleValidator == null)
        {
            articleValidator = GetComponent<ArticleValidator>();
        }
    }

    private void DisplayArticleInfo()
    {
        if (titleText != null)
        {
            titleText.text = currentArticle.Title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = currentArticle.Subtitle;
        }

        if (currentArticle.Writer != null)
        {
            if (authorNameText != null)
            {
                authorNameText.text = currentArticle.Writer.WriterName;
            }

            if (authorDescText != null)
            {
                authorDescText.text = currentArticle.Writer.WriterDescription;
            }

            if (authorPhotoImage != null)
            {
                authorPhotoImage.sprite = currentArticle.Writer.WriterPhoto;
            }
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
            {
                blockViews[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnBlockSolved(Block block)
    {
        bool allSolved = true;
        foreach (var b in blockComponents)
        {
            if (!b.IsSolved && b.ArticleBlock.CheckType != CheckType.True)
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
        Debug.Log("All blocks solved!");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(50);
        }
    }

    // This calls the validator instead of doing validation here
    public void OnVerifyButtonPressed()
    {
        if (articleValidator == null)
        {
            Debug.LogError("ArticleValidator not assigned!");
            return;
        }

        // Use the validator - it will handle feedback
        articleValidator.OnValidateButtonPressed();
    }

    public string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True:
                return "True Check";
            case CheckType.Label:
                return "Label Check";
            case CheckType.Source:
                return "Source Check";
            case CheckType.AI:
                return "AI Check";
            case CheckType.Specialist:
                return "Specialist Check";
            case CheckType.Falacy:
                return "Fallacy Check";
            case CheckType.None:
            default:
                return "None";
        }
    }

    public List<Block> GetBlockComponents()
    {
        return blockComponents;
    }
}