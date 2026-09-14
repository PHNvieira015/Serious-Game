using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArticleViewer : MonoBehaviour
{
    [Header("Article Data")]
    public ArticleData currentArticle;

    [Header("Article Header")]
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text authorNameText;
    public TMP_Text authorDescText;
    public UnityEngine.UI.Image authorPhotoImage;

    [Header("Article Scroll")]
    public UnityEngine.UI.ScrollRect articleScrollRect;
    public RectTransform articleContent;

    [Header("Article Blocks")]
    public ArticleBlockView[] blockViews;

    [Header("Article Verification")]
    public UnityEngine.UI.Button verifyArticleButton;

    [Header("Validator")]
    public ArticleValidator articleValidator;

    [Header("Scoring")]
    public int pointsPerCorrect = 10;
    public int penaltyPerWrong = 5;
    public int penaltyPerMissed = 5;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private List<Block> blockComponents =
        new List<Block>();

    private Coroutine scrollToTopRoutine;
    private bool scrollToTopWhenEnabled;

    private void Awake()
    {
        CacheScrollReferences();
    }

    private void OnEnable()
    {
        CacheScrollReferences();

        scrollToTopWhenEnabled = false;

        ScrollToTop();
    }

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
        else
        {
            ScrollToTop();
        }
    }

    public void LoadArticle(
        ArticleData article)
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

        if (showDebugMessages)
        {
            Debug.Log(
                "ArticleViewer: Loading article: " +
                currentArticle.Title
            );
        }

        DisplayArticleInfo();
        DisplayBlocks();

        if (articleValidator == null)
        {
            articleValidator =
                GetComponent<ArticleValidator>();
        }

        ScrollToTop();
    }

    public void ScrollToTop()
    {
        CacheScrollReferences();

        if (!isActiveAndEnabled)
        {
            scrollToTopWhenEnabled = true;

            if (showDebugMessages)
            {
                Debug.Log(
                    "ArticleViewer: Article screen is inactive. " +
                    "Scroll reset will run when it becomes active."
                );
            }

            return;
        }

        if (articleScrollRect == null)
        {
            Debug.LogWarning(
                "ArticleViewer: Article ScrollRect is not assigned."
            );

            return;
        }

        if (scrollToTopRoutine != null)
        {
            StopCoroutine(scrollToTopRoutine);
        }

        scrollToTopRoutine =
            StartCoroutine(
                ScrollToTopRoutine()
            );
    }

    private IEnumerator ScrollToTopRoutine()
    {
        /*
         * Wait for the Article screen to become visible
         * and for its layout components to update.
         */
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (articleContent != null)
        {
            UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    articleContent
                );
        }

        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();

        if (articleScrollRect != null)
        {
            articleScrollRect.StopMovement();
            articleScrollRect.velocity =
                Vector2.zero;

            articleScrollRect.verticalNormalizedPosition =
                1f;

            Canvas.ForceUpdateCanvases();

            articleScrollRect.verticalNormalizedPosition =
                1f;

            if (showDebugMessages)
            {
                Debug.Log(
                    "ArticleViewer: Article was moved to the top. " +
                    "Scroll position: " +
                    articleScrollRect.verticalNormalizedPosition
                );
            }
        }

        scrollToTopRoutine = null;
        scrollToTopWhenEnabled = false;
    }

    private void CacheScrollReferences()
    {
        if (articleScrollRect == null)
        {
            articleScrollRect =
                GetComponentInChildren<
                    UnityEngine.UI.ScrollRect
                >(true);
        }

        if (articleContent == null &&
            articleScrollRect != null)
        {
            articleContent =
                articleScrollRect.content;
        }
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

        int amountToDisplay =
            Mathf.Min(
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
                    blockView.GetComponentInChildren<
                        Block
                    >();
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

    public void OnBlockSolved(
        Block block)
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
        Debug.Log(
            "All blocks solved!"
        );

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

    private void OnDisable()
    {
        if (scrollToTopRoutine != null)
        {
            StopCoroutine(scrollToTopRoutine);
            scrollToTopRoutine = null;
        }
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