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
            if (block != null)
            {
                blocks.Add(block);
            }
        }

        Debug.Log("ArticleValidator: Found " + blocks.Count + " blocks.");
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

        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            BlockResult blockResult = new BlockResult();
            blockResult.Block = block;

            if (block.ArticleBlock != null)
            {
                blockResult.BlockText = block.ArticleBlock.Text;
                blockResult.ExpectedCheck = block.ArticleBlock.CheckType;
                blockResult.IsTrueCheck = (block.ArticleBlock.CheckType == CheckType.True);
            }
            else
            {
                blockResult.BlockText = "No text";
                blockResult.ExpectedCheck = CheckType.None;
                blockResult.IsTrueCheck = false;
            }

            if (block.CurrentDraggable == null)
            {
                blockResult.IsCorrect = false;
                blockResult.PlayerCheck = CheckType.None;
                blockResult.ResultType = BlockResultType.Missed;
                articleSolved = false;
            }
            else
            {
                blockResult.PlayerCheck = block.CurrentDraggable.CheckType;
                blockResult.IsCorrect = block.ValidateCurrentDraggable();

                if (blockResult.IsCorrect)
                {
                    block.MarkAsSolved();
                    blockResult.ResultType = BlockResultType.Correct;
                }
                else
                {
                    blockResult.ResultType = BlockResultType.Wrong;
                    articleSolved = false;
                }
            }

            result.BlockResults.Add(blockResult);
        }

        result.IsArticleSolved = articleSolved;

        Debug.Log("Validation Complete. Solved: " + articleSolved);

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