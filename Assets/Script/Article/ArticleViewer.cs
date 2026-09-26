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

    [Header("Article URL")]
    public TMP_Text articleURLText;

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

    private readonly List<Block> blockComponents = new List<Block>();
    private Coroutine scrollToTopRoutine;
    private bool hasLoadedArticle;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        ScrollToTop();
    }

    private void Start()
    {
        CacheReferences();

        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveListener(
                OnVerifyButtonPressed
            );
            verifyArticleButton.onClick.AddListener(
                OnVerifyButtonPressed
            );
        }

        if (!hasLoadedArticle && currentArticle != null)
            LoadArticle(currentArticle);
    }

    private void CacheReferences()
    {
        if (articleValidator == null)
            articleValidator = GetComponent<ArticleValidator>();

        if (articleScrollRect == null)
        {
            articleScrollRect =
                GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
        }

        if (articleContent == null && articleScrollRect != null)
            articleContent = articleScrollRect.content;
    }

    public void LoadArticle(ArticleData article)
    {
        if (article == null)
        {
            Debug.LogError("ArticleViewer: Article is null.", this);
            return;
        }

        CacheReferences();
        ClearCurrentArticle();

        currentArticle = article;
        hasLoadedArticle = true;

        DisplayArticleInfo();
        DisplayBlocks();

        if (articleValidator != null)
        {
            // Discover dropdowns and reset them to option zero.
            articleValidator.OnArticleLoaded(this);
        }
        else
        {
            Debug.LogWarning(
                "ArticleViewer: Assign Article Validator.",
                this
            );
        }

        ScrollToTop();

        if (showDebugMessages)
            Debug.Log("ArticleViewer: Loaded " + article.Title, this);
    }

    private void ClearCurrentArticle()
    {
        foreach (Block block in blockComponents)
        {
            if (block != null)
                block.OnBlockSolved -= OnBlockSolved;
        }

        blockComponents.Clear();

        if (blockViews == null)
            return;

        foreach (ArticleBlockView view in blockViews)
        {
            if (view != null)
                view.gameObject.SetActive(false);
        }
    }

    private void DisplayArticleInfo()
    {
        if (titleText != null)
            titleText.text = currentArticle.Title;

        if (subtitleText != null)
            subtitleText.text = currentArticle.Subtitle;

        if (articleURLText != null)
        {
            articleURLText.text =
                currentArticle.URL_site ?? string.Empty;
        }

        if (authorNameText != null)
        {
            authorNameText.text = currentArticle.Writer != null
                ? currentArticle.Writer.WriterName
                : string.Empty;
        }

        if (authorDescText != null)
        {
            authorDescText.text = currentArticle.Writer != null
                ? currentArticle.Writer.WriterDescription
                : string.Empty;
        }

        if (authorPhotoImage != null)
        {
            authorPhotoImage.sprite = currentArticle.Writer != null
                ? currentArticle.Writer.WriterPhoto
                : null;

            authorPhotoImage.enabled = authorPhotoImage.sprite != null;
        }
    }

    private void DisplayBlocks()
    {
        if (blockViews == null || currentArticle.Blocks == null)
            return;

        int count = Mathf.Min(
            blockViews.Length,
            currentArticle.Blocks.Count
        );

        for (int i = 0; i < count; i++)
        {
            ArticleBlockView view = blockViews[i];
            ArticleBlock data = currentArticle.Blocks[i];

            if (view == null || data == null)
                continue;

            Block block = view.blockComponent;

            if (block == null)
                block = view.GetComponent<Block>();

            if (block == null)
                block = view.GetComponentInChildren<Block>(true);

            view.blockComponent = block;
            view.Initialize(data);
            view.gameObject.SetActive(true);

            if (block != null)
            {
                block.OnBlockSolved -= OnBlockSolved;
                block.OnBlockSolved += OnBlockSolved;
                blockComponents.Add(block);
            }
        }

        if (currentArticle.Blocks.Count > blockViews.Length)
        {
            Debug.LogWarning(
                "ArticleViewer: Not enough block views for this article.",
                this
            );
        }
    }

    public void OnBlockSolved(Block block)
    {
        foreach (Block current in blockComponents)
        {
            if (current == null || current.ArticleBlock == null)
                continue;

            if (!current.IsSolved &&
                current.ArticleBlock.CheckType != CheckType.True)
            {
                return;
            }
        }

        OnAllBlocksSolved();
    }

    private void OnAllBlocksSolved()
    {
        // ArticleValidator awards the completion score.
        // Avoid awarding the same completion bonus here too.
        if (showDebugMessages)
            Debug.Log("All blocks solved!", this);
    }

    public void OnVerifyButtonPressed()
    {
        if (articleValidator == null)
        {
            Debug.LogError("ArticleValidator not assigned!", this);
            return;
        }

        articleValidator.OnValidateButtonPressed();
    }

    public List<Block> GetBlockComponents()
    {
        return blockComponents;
    }

    public string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True: return "True Check";
            case CheckType.Label: return "Label Check";
            case CheckType.Source: return "Source Check";
            case CheckType.AI: return "AI Check";
            case CheckType.Specialist: return "Specialist Check";
            case CheckType.Falacy: return "Fallacy Check";
            default: return "None";
        }
    }

    public void ScrollToTop()
    {
        CacheReferences();

        if (!isActiveAndEnabled || articleScrollRect == null)
            return;

        if (scrollToTopRoutine != null)
            StopCoroutine(scrollToTopRoutine);

        scrollToTopRoutine = StartCoroutine(ScrollToTopRoutine());
    }

    private IEnumerator ScrollToTopRoutine()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (articleContent != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(
                articleContent
            );
        }

        Canvas.ForceUpdateCanvases();

        if (articleScrollRect != null)
        {
            articleScrollRect.StopMovement();
            articleScrollRect.velocity = Vector2.zero;
            articleScrollRect.verticalNormalizedPosition = 1f;
        }

        scrollToTopRoutine = null;
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

        foreach (Block block in blockComponents)
        {
            if (block != null)
                block.OnBlockSolved -= OnBlockSolved;
        }
    }
}