using UnityEngine;
using System.Collections.Generic;

public class ArticleValidator : MonoBehaviour
{
    [Header("Article Blocks")]
    [SerializeField]
    private List<Block> blocks = new List<Block>();

    [Header("Validation Result")]
    [SerializeField]
    private bool articleSolved;

    [Header("Feedback")]
    [SerializeField]
    private ArticleFeedbackPanel feedbackPanel;

    public bool ArticleSolved => articleSolved;
    public List<Block> Blocks => blocks;

    private void Awake()
    {
        FindBlocks();
    }

    public void FindBlocks()
    {
        blocks.Clear();

        Block[] foundBlocks = Object.FindObjectsByType<Block>(FindObjectsSortMode.None);

        foreach (Block block in foundBlocks)
        {
            if (block != null && block.ArticleBlock != null)
            {
                blocks.Add(block);
            }
        }

        Debug.Log("ArticleValidator: Found " + blocks.Count + " blocks with ArticleBlock.");
    }

    public void OnValidateButtonPressed()
    {
        ValidationResult result = ValidateArticle();

        if (feedbackPanel != null)
        {
            feedbackPanel.StartFeedback(result, OnFeedbackComplete);
        }
        else
        {
            Debug.LogWarning("Feedback Panel not assigned");
            if (articleSolved)
            {
                OnArticleSolved();
            }
            else
            {
                OnArticleIncorrect();
            }
        }
    }

    public ValidationResult ValidateArticle()
    {
        if (blocks == null || blocks.Count == 0)
        {
            FindBlocks();
        }

        articleSolved = true;
        ValidationResult result = new ValidationResult();

        int totalBlocks = 0;
        int correctBlocks = 0;
        int wrongBlocks = 0;
        int missedBlocks = 0;

        foreach (Block block in blocks)
        {
            if (block == null || block.ArticleBlock == null)
            {
                continue;
            }

            totalBlocks++;

            BlockResult blockResult = new BlockResult();
            blockResult.Block = block;
            blockResult.BlockText = block.ArticleBlock.Text;
            blockResult.ExpectedCheck = block.ArticleBlock.CheckType;
            blockResult.IsTrueCheck = (block.ArticleBlock.CheckType == CheckType.True);
            blockResult.HasDraggable = (block.CurrentDraggable != null);

            if (blockResult.HasDraggable)
            {
                blockResult.PlayerCheck = block.CurrentDraggable.CheckType;
            }
            else
            {
                blockResult.PlayerCheck = CheckType.None;
            }

            bool shouldShowInFeedback = false;
            bool isCorrect = false;

            // Case 1: True block
            if (blockResult.IsTrueCheck)
            {
                // True block is correct if it's empty OR marked with True
                if (!blockResult.HasDraggable || blockResult.PlayerCheck == CheckType.True)
                {
                    isCorrect = true;
                    correctBlocks++;

                    if (blockResult.HasDraggable && blockResult.PlayerCheck == CheckType.True)
                    {
                        block.MarkAsSolved();
                    }
                    else if (!blockResult.HasDraggable)
                    {
                        block.MarkAsSolved();
                    }

                    // True block correct - skip feedback
                    shouldShowInFeedback = false;
                }
                else
                {
                    // True block marked with wrong type
                    isCorrect = false;
                    shouldShowInFeedback = true;
                    blockResult.ResultType = BlockResultType.Wrong;
                    articleSolved = false;
                    wrongBlocks++;
                    Debug.Log("True block marked wrong: " + block.name + " | Player: " + blockResult.PlayerCheck);
                }
            }
            // Case 2: Non-True block - always show in feedback
            else
            {
                shouldShowInFeedback = true;

                if (blockResult.HasDraggable && blockResult.PlayerCheck == blockResult.ExpectedCheck)
                {
                    isCorrect = true;
                    correctBlocks++;
                    block.MarkAsSolved();
                    blockResult.ResultType = BlockResultType.Correct;
                    Debug.Log("Correct mark: " + block.name + " | Expected: " + blockResult.ExpectedCheck + " | Player: " + blockResult.PlayerCheck);
                }
                else if (blockResult.HasDraggable && blockResult.PlayerCheck != blockResult.ExpectedCheck)
                {
                    isCorrect = false;
                    blockResult.ResultType = BlockResultType.Wrong;
                    articleSolved = false;
                    wrongBlocks++;
                    Debug.Log("Wrong mark: " + block.name + " | Expected: " + blockResult.ExpectedCheck + " | Player: " + blockResult.PlayerCheck);
                }
                else if (!blockResult.HasDraggable)
                {
                    isCorrect = false;
                    blockResult.ResultType = BlockResultType.Missed;
                    articleSolved = false;
                    missedBlocks++;
                    Debug.Log("Missed block: " + block.name + " | Expected: " + blockResult.ExpectedCheck);
                }
            }

            blockResult.IsCorrect = isCorrect;

            if (shouldShowInFeedback)
            {
                result.BlockResults.Add(blockResult);
            }
        }

        result.IsArticleSolved = articleSolved;

        Debug.Log("Validation Complete. Total: " + totalBlocks + " | Correct: " + correctBlocks + " | Wrong: " + wrongBlocks + " | Missed: " + missedBlocks + " | Feedback items: " + result.BlockResults.Count);

        return result;
    }

    private void OnFeedbackComplete()
    {
        if (articleSolved)
        {
            OnArticleSolved();
        }
        else
        {
            OnArticleIncorrect();
        }
    }

    private void OnArticleSolved()
    {
        Debug.Log("ARTICLE SOLVED");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(50);
        }
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
        int correctCount = 0;

        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            if (block.IsSolved)
            {
                correctCount++;
            }
        }

        return correctCount;
    }

    public void ResetValidation()
    {
        articleSolved = false;

        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            if (block.CurrentDraggable != null)
            {
                block.CurrentDraggable.ResetDraggable();
            }
        }
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
            case BlockResultType.Correct:
                return "Correct";
            case BlockResultType.Wrong:
                return "Wrong";
            case BlockResultType.Missed:
                return "Missed";
            default:
                return "Unknown";
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
            case CheckType.True:
                return "Verdadeiro";
            case CheckType.Label:
                return "Tendencioso";
            case CheckType.Source:
                return "Fonte";
            case CheckType.AI:
                return "IA";
            case CheckType.Specialist:
                return "Especialista";
            case CheckType.Falacy:
                return "Falacia";
            case CheckType.None:
            default:
                return "Nenhum";
        }
    }
}

public class ValidationResult
{
    public List<BlockResult> BlockResults = new List<BlockResult>();
    public bool IsArticleSolved;

    public int CorrectCount
    {
        get
        {
            int count = 0;
            foreach (var result in BlockResults)
            {
                if (result.ResultType == BlockResultType.Correct)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public int WrongCount
    {
        get
        {
            int count = 0;
            foreach (var result in BlockResults)
            {
                if (result.ResultType == BlockResultType.Wrong)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public int MissedCount
    {
        get
        {
            int count = 0;
            foreach (var result in BlockResults)
            {
                if (result.ResultType == BlockResultType.Missed)
                {
                    count++;
                }
            }
            return count;
        }
    }
}