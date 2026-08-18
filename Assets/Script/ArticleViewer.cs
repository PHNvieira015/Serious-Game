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
    public Transform articleContent;
    public ArticleBlockView articleBlockPrefab;

    [Header("Drag & Drop")]
    public Transform draggableContainer;
    public DraggableObject[] draggableObjects;

    [Header("Check Panel - DEPRECATED (Using Drag & Drop)")]
    public GameObject checkPanel;
    public TMP_Text hintText;

    [Header("Check Buttons - DEPRECATED (Using Drag & Drop)")]
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
    private readonly List<ArticleBlockView> blockViews = new List<ArticleBlockView>();
    private List<Block> blockComponents = new List<Block>();
    private ArticleBlockView currentlySelectedBlock;

    private void Start()
    {
        SetupButtons();

        if (currentArticle != null)
            LoadArticle(currentArticle);
    }

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
            button.onClick.AddListener(() => OnCheckSelected(type));
        }

        if (verifyArticleButton != null)
        {
            verifyArticleButton.onClick.RemoveAllListeners();
            verifyArticleButton.onClick.AddListener(VerifyArticle);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseFeedback);
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
        currentlySelectedBlock = null;

        ClearArticleContent();

        DisplayArticleInfo();
        BuildArticleBlocks();

        HideCheckPanel();
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

        ForceLayoutUpdate();
    }

    private void BuildArticleBlocks()
    {
        if (articleContent == null)
        {
            Debug.LogError("ArticleViewer: Article Content is not assigned.");
            return;
        }

        if (articleBlockPrefab == null)
        {
            Debug.LogError("ArticleViewer: Article Block Prefab is not assigned.");
            return;
        }

        if (currentArticle.Blocks == null)
        {
            Debug.LogWarning("ArticleViewer: Article has no blocks.");
            return;
        }

        blockComponents.Clear();

        foreach (ArticleBlock block in currentArticle.Blocks)
        {
            if (block == null)
                continue;

            ArticleBlockView blockView = Instantiate(articleBlockPrefab, articleContent);
            blockView.Initialize(block, this);
            blockViews.Add(blockView);

            Block blockComponent = blockView.GetComponent<Block>();
            if (blockComponent != null)
            {
                blockComponent.Initialize(block);
                blockComponent.OnBlockSolved += OnBlockSolved;
                blockComponents.Add(blockComponent);
            }
        }

        SetupDraggableObjects();

        StartCoroutine(RefreshArticleLayout());
    }

    private void SetupDraggableObjects()
    {
        if (draggableObjects == null || draggableObjects.Length == 0)
            return;

        foreach (var obj in draggableObjects)
        {
            if (obj != null)
                obj.ResetDraggable();
        }

        if (GameManager.Instance != null)
        {
            foreach (var obj in draggableObjects)
            {
                if (obj == null) continue;

                int remaining = GameManager.Instance.GetRemainingChecks(obj.CheckType);
                obj.gameObject.SetActive(remaining > 0);
            }
        }
    }

    private void ClearArticleContent()
    {
        currentlySelectedBlock = null;

        foreach (ArticleBlockView view in blockViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        blockViews.Clear();
        blockComponents.Clear();

        if (articleContent == null)
            return;

        for (int i = articleContent.childCount - 1; i >= 0; i--)
        {
            Destroy(articleContent.GetChild(i).gameObject);
        }
    }

    public void SelectBlock(ArticleBlockView blockView)
    {
        if (blockView == null)
            return;

        currentlySelectedBlock = blockView;
        ShowCheckPanel(blockView);
    }

    private void ShowCheckPanel(ArticleBlockView blockView)
    {
        if (checkPanel != null)
            checkPanel.SetActive(true);

        ArticleBlock block = blockView.Block;

        if (hintText != null)
        {
            hintText.text = block.Hint;
            hintText.gameObject.SetActive(!string.IsNullOrEmpty(block.Hint));
            hintText.ForceMeshUpdate();
        }

        foreach (Button button in checkButtons.Values)
        {
            if (button != null)
                button.interactable = true;
        }

        ForceLayoutUpdate();
    }

    private void OnCheckSelected(CheckType selectedCheck)
    {
        if (currentlySelectedBlock == null)
            return;

        if (!TryUseCheck(selectedCheck))
        {
            Debug.Log($"Not enough {selectedCheck} remaining!");
            return;
        }

        currentlySelectedBlock.SetPlayerCheckType(selectedCheck);
        HideCheckPanel();
    }

    private bool TryUseCheck(CheckType checkType)
    {
        if (GameManager.Instance != null)
        {
            return GameManager.Instance.UseCheck(checkType);
        }
        return true;
    }

    public void OnBlockSolved(Block block)
    {
        Debug.Log($"Block solved: {block.ArticleBlock.Text}");

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
        Debug.Log("All blocks solved!");

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

        for (int i = 0; i < blockViews.Count; i++)
        {
            ArticleBlockView view = blockViews[i];

            if (view == null || view.Block == null)
                continue;

            ArticleBlock block = view.Block;

            if (block.CheckType == CheckType.None)
            {
                continue;
            }

            if (block.CheckType == CheckType.TrueCheck)
            {
                if (!view.IsMarked)
                {
                    continue;
                }

                wrong++;
                score -= penaltyPerWrong;
                results.AppendLine($"Block {i + 1}: WRONG");
                results.AppendLine("This block was true and should not have been marked.");
                if (!string.IsNullOrEmpty(block.Explanation))
                {
                    results.AppendLine(block.Explanation);
                }
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
                {
                    results.AppendLine(block.Explanation);
                }
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
                {
                    results.AppendLine(block.Explanation);
                }
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
                {
                    results.AppendLine(block.Explanation);
                }
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

        HideCheckPanel();

        StartCoroutine(RefreshFeedbackLayout());
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

    private void HideCheckPanel()
    {
        if (checkPanel != null)
            checkPanel.SetActive(false);

        currentlySelectedBlock = null;
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
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
    }
}