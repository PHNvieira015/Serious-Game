using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    private List<Block> blockComponents =
        new List<Block>();

    private void Start()
    {
        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveListener(
                OnVerifyButtonPressed
            );

            verifyArticleButton.onClick.AddListener(
                OnVerifyButtonPressed
            );
        }

        if (articleValidator == null)
        {
            articleValidator =
                GetComponent<ArticleValidator>();
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
            Debug.LogError(
                "ArticleViewer: Article is null."
            );

            return;
        }

        ClearCurrentArticle();

        currentArticle = article;

        DisplayArticleInfo();
        DisplayBlocks();

        if (articleValidator == null)
        {
            articleValidator =
                GetComponent<ArticleValidator>();
        }

        Debug.Log(
            "ArticleViewer: Loaded article: " +
            currentArticle.Title
        );
    }

    private void ClearCurrentArticle()
    {
        for (
            int i = 0;
            i < blockComponents.Count;
            i++
        )
        {
            Block blockComponent =
                blockComponents[i];

            if (blockComponent != null)
            {
                blockComponent.OnBlockSolved -=
                    OnBlockSolved;
            }
        }

        blockComponents.Clear();

        if (blockViews == null)
        {
            return;
        }

        for (
            int i = 0;
            i < blockViews.Length;
            i++
        )
        {
            if (blockViews[i] != null)
            {
                blockViews[i].gameObject.SetActive(
                    false
                );
            }
        }
    }

    private void DisplayArticleInfo()
    {
        if (currentArticle == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text =
                currentArticle.Title;
        }

        if (subtitleText != null)
        {
            subtitleText.text =
                currentArticle.Subtitle;
        }

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
                    currentArticle.Writer
                        .WriterDescription;
            }

            if (authorPhotoImage != null)
            {
                authorPhotoImage.sprite =
                    currentArticle.Writer.WriterPhoto;

                authorPhotoImage.enabled =
                    currentArticle.Writer.WriterPhoto !=
                    null;
            }
        }
        else
        {
            if (authorNameText != null)
            {
                authorNameText.text =
                    string.Empty;
            }

            if (authorDescText != null)
            {
                authorDescText.text =
                    string.Empty;
            }

            if (authorPhotoImage != null)
            {
                authorPhotoImage.sprite = null;
                authorPhotoImage.enabled = false;
            }
        }
    }

    private void DisplayBlocks()
    {
        if (blockViews == null ||
            blockViews.Length == 0)
        {
            Debug.LogWarning(
                "ArticleViewer: No block views assigned."
            );

            return;
        }

        if (currentArticle == null ||
            currentArticle.Blocks == null ||
            currentArticle.Blocks.Count == 0)
        {
            Debug.LogWarning(
                "ArticleViewer: Article has no blocks."
            );

            return;
        }

        int amountToDisplay = Mathf.Min(
            currentArticle.Blocks.Count,
            blockViews.Length
        );

        for (
            int i = 0;
            i < amountToDisplay;
            i++
        )
        {
            ArticleBlockView blockView =
                blockViews[i];

            ArticleBlock blockData =
                currentArticle.Blocks[i];

            if (blockView == null ||
                blockData == null)
            {
                continue;
            }

            blockView.Initialize(blockData);
            blockView.gameObject.SetActive(true);

            Block blockComponent =
                blockView.GetComponent<Block>();

            if (blockComponent == null)
            {
                blockComponent =
                    blockView
                        .GetComponentInChildren<Block>();
            }

            if (blockComponent != null)
            {
                blockComponent.Initialize(blockData);

                blockComponent.OnBlockSolved -=
                    OnBlockSolved;

                blockComponent.OnBlockSolved +=
                    OnBlockSolved;

                blockComponents.Add(
                    blockComponent
                );
            }
        }

        if (currentArticle.Blocks.Count >
            blockViews.Length)
        {
            Debug.LogWarning(
                "ArticleViewer: The article has more " +
                "blocks than available block views."
            );
        }
    }

    public void OnBlockSolved(Block block)
    {
        bool allSolved = true;

        for (
            int i = 0;
            i < blockComponents.Count;
            i++
        )
        {
            Block currentBlock =
                blockComponents[i];

            if (currentBlock == null)
            {
                continue;
            }

            if (currentBlock.ArticleBlock == null)
            {
                continue;
            }

            if (!currentBlock.IsSolved &&
                currentBlock.ArticleBlock.CheckType !=
                CheckType.True)
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

    public void OnVerifyButtonPressed()
    {
        if (articleValidator == null)
        {
            Debug.LogError(
                "ArticleValidator not assigned!"
            );

            return;
        }

        articleValidator.OnValidateButtonPressed();
    }

    public string GetCheckTypeName(
        CheckType type)
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

    private void OnDestroy()
    {
        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveListener(
                OnVerifyButtonPressed
            );
        }

        for (
            int i = 0;
            i < blockComponents.Count;
            i++
        )
        {
            Block blockComponent =
                blockComponents[i];

            if (blockComponent != null)
            {
                blockComponent.OnBlockSolved -=
                    OnBlockSolved;
            }
        }
    }
}