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
    [SerializeField] private List<Block> blocks = new List<Block>();

    [Header("Block Dropdowns")]
    [SerializeField]
    private List<BlockDropdownReference> blockDropdowns =
        new List<BlockDropdownReference>();

    [Header("Validation Result")]
    [SerializeField] private bool articleSolved;

    [Header("Feedback")]
    [SerializeField] private ArticleFeedbackPanel feedbackPanel;

    [Header("Debug")]
    [SerializeField] private bool showDebugMessages = true;

    private readonly List<ValidatedBlock> validatedBlocks =
        new List<ValidatedBlock>();

    private bool feedbackInProgress;

    public bool ArticleSolved => articleSolved;
    public List<Block> Blocks => blocks;

    private void Awake()
    {
        if (articleViewer == null)
            articleViewer = GetComponent<ArticleViewer>();
    }

    // Called by ArticleViewer after loading the article.
    public void OnArticleLoaded(ArticleViewer viewer)
    {
        // Finish the old feedback before replacing its block list.
        if (feedbackInProgress && feedbackPanel != null)
            feedbackPanel.CloseFeedback();

        feedbackInProgress = false;
        articleViewer = viewer;
        ResetValidation();
    }

    public void FindBlocks()
    {
        blocks.Clear();

        if (articleViewer == null)
            articleViewer = GetComponent<ArticleViewer>();

        if (articleViewer != null)
        {
            List<Block> currentBlocks = articleViewer.GetBlockComponents();

            if (currentBlocks != null)
            {
                foreach (Block block in currentBlocks)
                    AddBlock(block);
            }
        }
        else
        {
            Block[] found = UnityEngine.Object.FindObjectsByType<Block>(
                FindObjectsSortMode.None
            );

            foreach (Block block in found)
                AddBlock(block);
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

            Transform searchRoot = block.transform;

            ArticleBlockView view = FindView(block);
            if (view != null)
                searchRoot = view.transform;

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
                    "[VALIDATION] Block " + block.name +
                    " has " + found.Length +
                    " dropdowns under its view. Assign its dropdown " +
                    "manually in Block Dropdowns.",
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

        ArticleBlockView ownView = block.GetComponent<ArticleBlockView>();

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

        ValidationResult result = ValidateArticle();

        if (validatedBlocks.Count == 0)
        {
            Debug.LogWarning("[VALIDATION] No article blocks.", this);
            return;
        }

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

            // Preserve the existing True-block rule.
            bool correct = isTrue
                ? selected == CheckType.None || selected == CheckType.True
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

            if (correct)
            {
                if (!block.IsSolved)
                    block.MarkAsSolved();
            }
            else
            {
                articleSolved = false;
            }

            if (!isTrue || !correct)
                result.BlockResults.Add(item);

            if (showDebugMessages)
            {
                Debug.Log(
                    "[VALIDATION] " + block.name +
                    " | Expected: " + expected +
                    " | Marked: " + selected +
                    " | Result: " + item.ResultType,
                    block
                );
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

        if (articleSolved)
            OnArticleSolved();
        else
            OnArticleIncorrect();

        foreach (ValidatedBlock entry in validatedBlocks)
        {
            if (entry.block != null &&
                entry.block.ArticleBlock == entry.data)
            {
                ResetBlock(entry.block);
            }
        }

        validatedBlocks.Clear();
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

    private void OnArticleSolved()
    {
        Debug.Log("ARTICLE SOLVED");

        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(50);
    }

    private void OnArticleIncorrect()
    {
        Debug.Log("ARTICLE NOT SOLVED");
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
    public bool IsArticleSolved;

    public int CorrectCount => CountResults(BlockResultType.Correct);
    public int WrongCount => CountResults(BlockResultType.Wrong);
    public int MissedCount => CountResults(BlockResultType.Missed);

    private int CountResults(BlockResultType type)
    {
        int count = 0;

        foreach (BlockResult result in BlockResults)
        {
            if (result != null && result.ResultType == type)
                count++;
        }

        return count;
    }
}