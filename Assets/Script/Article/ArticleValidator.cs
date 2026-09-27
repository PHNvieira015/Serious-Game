using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ArticleValidator : MonoBehaviour
{
    [Serializable]
    public class BlockDropdownReference
    {
        public Block block;
        public TMP_Dropdown dropdown;
    }

    private class ValidatedBlock
    {
        public Block block;
        public ArticleBlock data;
    }

    [Header("Article Viewer")]
    [SerializeField] private ArticleViewer articleViewer;

    [Header("Article Blocks")]
    [SerializeField]
    private List<Block> blocks = new List<Block>();

    [Header("Block Dropdowns")]
    [SerializeField]
    private List<BlockDropdownReference> blockDropdowns =
        new List<BlockDropdownReference>();

    [Header("Validation Result")]
    [SerializeField] private bool articleSolved;

    [Header("Feedback")]
    [SerializeField] private ArticleFeedbackPanel feedbackPanel;

    [Header("History")]
    [SerializeField] private ArticleHistoryManager historyManager;

    [Header("Score")]
    [SerializeField] private ArticleScoreManager scoreManager;
    [SerializeField] private bool openScoreAfterFeedback = true;

    private readonly List<ValidatedBlock> validatedBlocks =
        new List<ValidatedBlock>();

    private bool feedbackInProgress;
    private bool changingArticle;

    public bool ArticleSolved => articleSolved;
    public List<Block> Blocks => blocks;

    private void Awake()
    {
        if (articleViewer == null)
            articleViewer = GetComponent<ArticleViewer>();
    }

    public void OnArticleLoaded(ArticleViewer viewer)
    {
        changingArticle = true;

        if (feedbackInProgress && feedbackPanel != null)
            feedbackPanel.CloseFeedback();

        feedbackInProgress = false;
        articleViewer = viewer;

        ResetValidation();

        changingArticle = false;
    }

    public void FindBlocks()
    {
        blocks.Clear();

        if (articleViewer == null)
            articleViewer = GetComponent<ArticleViewer>();

        if (articleViewer != null)
        {
            List<Block> currentBlocks =
                articleViewer.GetBlockComponents();

            if (currentBlocks != null)
            {
                foreach (Block block in currentBlocks)
                    AddBlock(block);
            }
        }

        DiscoverDropdowns();
    }

    private void AddBlock(Block block)
    {
        if (block != null &&
            block.ArticleBlock != null &&
            !blocks.Contains(block))
        {
            blocks.Add(block);
        }
    }

    private void DiscoverDropdowns()
    {
        blockDropdowns.RemoveAll(
            entry => entry == null ||
                     entry.block == null ||
                     entry.dropdown == null
        );

        foreach (Block block in blocks)
        {
            if (GetDropdown(block) != null)
                continue;

            ArticleBlockView view = FindView(block);

            Transform searchRoot = view != null
                ? view.transform
                : block.transform;

            TMP_Dropdown[] found =
                searchRoot.GetComponentsInChildren<TMP_Dropdown>(true);

            if (found.Length == 1)
            {
                blockDropdowns.Add(new BlockDropdownReference
                {
                    block = block,
                    dropdown = found[0]
                });
            }
            else
            {
                Debug.LogWarning(
                    "[VALIDATION] Assign the dropdown for " + block.name,
                    block
                );
            }
        }
    }

    private ArticleBlockView FindView(Block block)
    {
        if (articleViewer != null && articleViewer.blockViews != null)
        {
            foreach (ArticleBlockView view in articleViewer.blockViews)
            {
                if (view == null)
                    continue;

                if (view.blockComponent == block ||
                    view.GetComponent<Block>() == block ||
                    view.GetComponentInChildren<Block>(true) == block)
                {
                    return view;
                }
            }
        }

        ArticleBlockView ownView =
            block.GetComponent<ArticleBlockView>();

        return ownView != null
            ? ownView
            : block.GetComponentInParent<ArticleBlockView>();
    }

    private TMP_Dropdown GetDropdown(Block block)
    {
        foreach (BlockDropdownReference entry in blockDropdowns)
        {
            if (entry != null &&
                entry.block == block &&
                entry.dropdown != null)
            {
                return entry.dropdown;
            }
        }

        return null;
    }

    public void OnValidateButtonPressed()
    {
        if (feedbackInProgress)
            return;

        if (articleViewer == null ||
            articleViewer.currentArticle == null ||
            historyManager == null)
        {
            Debug.LogError(
                "[VALIDATION] Assign Article Viewer and History Manager.",
                this
            );
            return;
        }

        ArticleData article = articleViewer.currentArticle;

        // Check before replacing or adding the history entry.
        bool alreadyInHistory = historyManager.ContainsArticle(article);

        ValidationResult result = ValidateArticle();

        if (result.TotalCount == 0)
        {
            Debug.LogWarning("[VALIDATION] No article blocks.", this);
            return;
        }

        if (scoreManager != null)
        {
            scoreManager.RecordArticle(
                article,
                result,
                !alreadyInHistory
            );
        }

        // Stores snapshots, not references to mutable marks.
        historyManager.SaveAttempt(article, result);

        feedbackInProgress = true;

        if (feedbackPanel != null)
            feedbackPanel.StartFeedback(result, OnFeedbackComplete);
        else
            OnFeedbackComplete();
    }

    public ValidationResult ValidateArticle()
    {
        FindBlocks();
        validatedBlocks.Clear();

        ValidationResult result = new ValidationResult();
        articleSolved = blocks.Count > 0;

        foreach (Block block in blocks)
        {
            validatedBlocks.Add(new ValidatedBlock
            {
                block = block,
                data = block.ArticleBlock
            });

            CheckType expected = block.BlockType;
            CheckType selected = block.MarkType;
            bool isTrue = expected == CheckType.True;

            bool correct = isTrue
                ? selected == CheckType.None ||
                  selected == CheckType.True
                : selected == expected;

            BlockResult item = new BlockResult
            {
                Block = block,
                BlockText = block.ArticleBlock.Text,
                ExpectedCheck = expected,
                PlayerCheck = selected,
                IsTrueCheck = isTrue,
                HasDraggable = block.CurrentDraggable != null,
                IsCorrect = correct,
                ResultType = correct
                    ? BlockResultType.Correct
                    : selected == CheckType.None
                        ? BlockResultType.Missed
                        : BlockResultType.Wrong
            };

            result.AllBlockResults.Add(item);
            result.BlockResults.Add(item);

            if (correct)
            {
                if (!block.IsSolved)
                    block.MarkAsSolved();
            }
            else
            {
                articleSolved = false;
            }
        }

        result.IsArticleSolved = articleSolved;
        return result;
    }

    private void OnFeedbackComplete()
    {
        if (!feedbackInProgress)
            return;

        feedbackInProgress = false;

        // Avoid resetting newly loaded blocks through old feedback.
        if (!changingArticle)
        {
            foreach (ValidatedBlock entry in validatedBlocks)
            {
                if (entry.block != null &&
                    entry.block.ArticleBlock == entry.data)
                {
                    ResetBlock(entry.block);
                }
            }
        }

        validatedBlocks.Clear();

        if (!changingArticle &&
            openScoreAfterFeedback &&
            scoreManager != null)
        {
            scoreManager.OpenScoreScreen();
        }
    }

    private void ResetBlock(Block block)
    {
        if (block == null)
            return;

        ArticleBlock data = block.ArticleBlock;

        if (block.CurrentDraggable != null)
            block.CurrentDraggable.ResetDraggable();

        if (data != null)
            block.Initialize(data);

        block.SetMarkType(CheckType.None);

        ArticleBlockView view = FindView(block);

        if (view != null)
            view.ClearPlayerSelection();

        TMP_Dropdown dropdown = GetDropdown(block);

        if (dropdown != null)
        {
            dropdown.Hide();
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }
    }

    public void ResetValidation()
    {
        if (feedbackInProgress)
            return;

        FindBlocks();

        foreach (Block block in blocks)
            ResetBlock(block);

        validatedBlocks.Clear();
        articleSolved = false;
    }

    public bool IsArticleSolved()
    {
        return articleSolved;
    }

    public int GetBlockCount()
    {
        return blocks.Count;
    }

    public int GetCorrectBlockCount()
    {
        int count = 0;

        foreach (Block block in blocks)
        {
            if (block != null && block.IsSolved)
                count++;
        }

        return count;
    }
}

public enum BlockResultType
{
    Correct,
    Wrong,
    Missed
}

public class BlockResult
{
    public Block Block;
    public string BlockText;
    public CheckType ExpectedCheck;
    public CheckType PlayerCheck;
    public bool IsCorrect;
    public bool IsTrueCheck;
    public bool HasDraggable;
    public BlockResultType ResultType;

    public string GetResultText()
    {
        switch (ResultType)
        {
            case BlockResultType.Correct: return "Correct";
            case BlockResultType.Wrong: return "Wrong";
            case BlockResultType.Missed: return "Missed";
            default: return "Unknown";
        }
    }

    public string GetExpectedCheckName()
    {
        return GetCheckTypeName(ExpectedCheck);
    }

    public string GetPlayerCheckName()
    {
        return GetCheckTypeName(PlayerCheck);
    }

    private string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True: return "Verdadeiro";
            case CheckType.Label: return "Tendencioso";
            case CheckType.Source: return "Fonte";
            case CheckType.AI: return "IA";
            case CheckType.Specialist: return "Especialista";
            case CheckType.Falacy: return "Falacia";
            default: return "Nenhum";
        }
    }
}

public class ValidationResult
{
    public List<BlockResult> BlockResults = new List<BlockResult>();
    public List<BlockResult> AllBlockResults = new List<BlockResult>();
    public bool IsArticleSolved;

    private List<BlockResult> CountingResults =>
        AllBlockResults.Count > 0 ? AllBlockResults : BlockResults;

    public int TotalCount
    {
        get
        {
            int count = 0;

            foreach (BlockResult result in CountingResults)
            {
                if (result != null)
                    count++;
            }

            return count;
        }
    }

    public int CorrectCount => CountResults(BlockResultType.Correct);
    public int WrongCount => CountResults(BlockResultType.Wrong);
    public int MissedCount => CountResults(BlockResultType.Missed);

    private int CountResults(BlockResultType type)
    {
        int count = 0;

        foreach (BlockResult result in CountingResults)
        {
            if (result != null && result.ResultType == type)
                count++;
        }

        return count;
    }
}