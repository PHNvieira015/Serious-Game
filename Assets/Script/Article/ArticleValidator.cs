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

    public bool ArticleSolved => articleSolved;

    private void Awake()
    {
        FindBlocks();
    }

    public void FindBlocks()
    {
        blocks.Clear();

        Block[] foundBlocks = FindObjectsOfType<Block>(true);

        foreach (Block block in foundBlocks)
        {
            if (block != null)
            {
                blocks.Add(block);
            }
        }

        Debug.Log($"ArticleValidator: Found {blocks.Count} blocks.");
    }

    public void ValidateArticle()
    {
        if (blocks == null || blocks.Count == 0)
        {
            FindBlocks();
        }

        articleSolved = true;

        int correctBlocks = 0;
        int incorrectBlocks = 0;
        int emptyBlocks = 0;

        foreach (Block block in blocks)
        {
            if (block == null)
            {
                continue;
            }

            if (block.CurrentDraggable == null)
            {
                emptyBlocks++;

                articleSolved = false;

                Debug.Log(
                    $"Block {block.name}: EMPTY. " +
                    $"Expected: {block.BlockType}"
                );

                continue;
            }

            bool isCorrect = block.ValidateCurrentDraggable();

            if (isCorrect)
            {
                correctBlocks++;

                block.MarkAsSolved();

                Debug.Log(
                    $"Block {block.name}: CORRECT. " +
                    $"Expected: {block.BlockType}. " +
                    $"Marked: {block.MarkType}."
                );
            }
            else
            {
                incorrectBlocks++;

                articleSolved = false;

                Debug.Log(
                    $"Block {block.name}: WRONG. " +
                    $"Expected: {block.BlockType}. " +
                    $"Marked: {block.MarkType}."
                );
            }
        }

        Debug.Log(
            $"Article Validation Complete. " +
            $"Correct: {correctBlocks}, " +
            $"Wrong: {incorrectBlocks}, " +
            $"Empty: {emptyBlocks}, " +
            $"Article Solved: {articleSolved}"
        );

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
        Debug.Log("ARTICLE SOLVED!");
    }

    private void OnArticleIncorrect()
    {
        Debug.Log("ARTICLE NOT SOLVED.");
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