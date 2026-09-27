using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticleHistoryManager : MonoBehaviour
{
    [Serializable]
    public class AnswerRecord
    {
        public int blockIndex;

        // Position in ArticleData.Blocks.
        // Separate from the validation list, which can skip blocks.
        public int articleBlockIndex = -1;

        public string blockText;
        public CheckType expectedType;
        public CheckType markedType;
        public BlockResultType resultType;
        public bool isCorrect;
    }

    [Serializable]
    public class ArticleRecord
    {
        public ArticleData article;
        public string title;
        public bool allCorrect;
        public int attempts;
        public int correctCount;
        public int totalCount;
        public string lastAttemptTime;

        public List<AnswerRecord> answers =
            new List<AnswerRecord>();
    }

    [Header("References")]
    public ArticleViewer articleViewer;
    public ArticleFeedbackPanel feedbackPanel;

    [Header("Article History")]
    [SerializeField]
    private List<ArticleRecord> history =
        new List<ArticleRecord>();

    public IReadOnlyList<ArticleRecord> History => history;

    public event Action OnHistoryChanged;

    private Coroutine openFeedbackRoutine;
    private bool openingFeedback;

    public bool ContainsArticle(ArticleData article)
    {
        return GetRecord(article) != null;
    }

    public ArticleRecord GetRecord(ArticleData article)
    {
        if (article == null)
        {
            return null;
        }

        foreach (ArticleRecord record in history)
        {
            if (record != null && record.article == article)
            {
                return record;
            }
        }

        return null;
    }

    public void SaveAttempt(
        ArticleData article,
        ValidationResult result)
    {
        if (article == null ||
            result == null ||
            result.TotalCount == 0)
        {
            return;
        }

        ArticleRecord record = GetRecord(article);

        if (record == null)
        {
            record = new ArticleRecord
            {
                article = article
            };

            history.Add(record);
        }

        record.title = article.Title;
        record.allCorrect = result.IsArticleSolved;
        record.correctCount = result.CorrectCount;
        record.totalCount = result.TotalCount;
        record.attempts++;

        record.lastAttemptTime =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (record.answers == null)
        {
            record.answers = new List<AnswerRecord>();
        }

        record.answers.Clear();

        List<BlockResult> source = result.AllBlockResults.Count > 0
            ? result.AllBlockResults
            : result.BlockResults;

        for (int i = 0; i < source.Count; i++)
        {
            BlockResult item = source[i];

            if (item == null)
            {
                continue;
            }

            int dataIndex = -1;

            if (article.Blocks != null &&
                item.Block != null &&
                item.Block.ArticleBlock != null)
            {
                dataIndex = article.Blocks.IndexOf(
                    item.Block.ArticleBlock
                );
            }

            record.answers.Add(new AnswerRecord
            {
                blockIndex = i,
                articleBlockIndex = dataIndex,
                blockText = item.BlockText,
                expectedType = item.ExpectedCheck,
                markedType = item.PlayerCheck,
                resultType = item.ResultType,
                isCorrect = item.IsCorrect
            });
        }

        OnHistoryChanged?.Invoke();
    }

    public void OpenArticleFeedback(ArticleData article)
    {
        if (openingFeedback)
        {
            return;
        }

        ArticleRecord record = GetRecord(article);

        if (record == null ||
            record.answers == null ||
            record.answers.Count == 0)
        {
            Debug.LogWarning(
                "[HISTORY] No saved answers for this article.",
                this
            );
            return;
        }

        if (articleViewer == null || feedbackPanel == null)
        {
            Debug.LogError(
                "[HISTORY] Assign Article Viewer and Feedback Panel.",
                this
            );
            return;
        }

        UIManager manager = UIManager.Instance;

        if (manager == null)
        {
            Debug.LogError(
                "[HISTORY] UIManager is missing.",
                this
            );
            return;
        }

        if (!isActiveAndEnabled)
        {
            Debug.LogError(
                "[HISTORY] Keep ArticleHistoryManager active " +
                "while changing screens.",
                this
            );
            return;
        }

        openingFeedback = true;

        openFeedbackRoutine = StartCoroutine(
            OpenSavedFeedback(record, manager)
        );
    }

    private IEnumerator OpenSavedFeedback(
        ArticleRecord record,
        UIManager manager)
    {
        // Initialize the block views used by feedback for
        // explanation text and answer colors.
        // Saved answers are independent of the reset live marks.
        articleViewer.LoadArticle(record.article);

        ValidationResult savedResult = BuildSavedResult(record);

        if (savedResult.TotalCount == 0)
        {
            openingFeedback = false;
            openFeedbackRoutine = null;
            yield break;
        }

        manager.OpenFeedback();

        // UIManager changes screens after its transition delay.
        yield return null;

        while (manager != null && !manager.IsFeedbackOpen())
        {
            yield return null;
        }

        if (manager != null && feedbackPanel != null)
        {
            // No validation, scoring, or history save takes place.
            feedbackPanel.StartFeedback(
                savedResult,
                ReturnToHistory
            );
        }

        openingFeedback = false;
        openFeedbackRoutine = null;
    }

    private ValidationResult BuildSavedResult(ArticleRecord record)
    {
        ValidationResult result = new ValidationResult
        {
            IsArticleSolved = record.allCorrect
        };

        List<Block> loadedBlocks = articleViewer.GetBlockComponents();

        foreach (AnswerRecord answer in record.answers)
        {
            if (answer == null)
            {
                continue;
            }

            Block block = FindLoadedBlock(
                record.article,
                answer,
                loadedBlocks
            );

            BlockResult item = new BlockResult
            {
                Block = block,
                BlockText = answer.blockText,
                ExpectedCheck = answer.expectedType,
                PlayerCheck = answer.markedType,
                IsCorrect = answer.isCorrect,
                IsTrueCheck = answer.expectedType == CheckType.True,
                HasDraggable = false,
                ResultType = answer.resultType
            };

            result.AllBlockResults.Add(item);
            result.BlockResults.Add(item);
        }

        return result;
    }

    private Block FindLoadedBlock(
        ArticleData article,
        AnswerRecord answer,
        List<Block> loadedBlocks)
    {
        if (loadedBlocks == null)
        {
            return null;
        }

        if (article != null &&
            article.Blocks != null &&
            answer.articleBlockIndex >= 0 &&
            answer.articleBlockIndex < article.Blocks.Count)
        {
            ArticleBlock data = article.Blocks[answer.articleBlockIndex];

            foreach (Block block in loadedBlocks)
            {
                if (block != null && block.ArticleBlock == data)
                {
                    return block;
                }
            }
        }

        // Supports records created before articleBlockIndex existed.
        if (answer.blockIndex >= 0 &&
            answer.blockIndex < loadedBlocks.Count)
        {
            return loadedBlocks[answer.blockIndex];
        }

        return null;
    }

    private void ReturnToHistory()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenHistory();
        }
    }

    // Retained for callers that want a fresh article attempt.
    public void OpenArticle(ArticleData article)
    {
        if (article == null)
        {
            Debug.LogWarning("[HISTORY] Article is null.", this);
            return;
        }

        if (articleViewer == null)
        {
            Debug.LogError(
                "[HISTORY] Assign Article Viewer.",
                this
            );
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError(
                "[HISTORY] UIManager is missing.",
                this
            );
            return;
        }

        articleViewer.LoadArticle(article);
        UIManager.Instance.OpenArticle();
    }

    // Retains the existing fresh-attempt behavior.
    public void OpenHistoryArticle(int index)
    {
        if (index < 0 || index >= history.Count)
        {
            return;
        }

        ArticleRecord record = history[index];

        if (record != null)
        {
            OpenArticle(record.article);
        }
    }

    public void OpenHistoryFeedback(int index)
    {
        if (index < 0 || index >= history.Count)
        {
            return;
        }

        ArticleRecord record = history[index];

        if (record != null)
        {
            OpenArticleFeedback(record.article);
        }
    }

    public void ClearHistory()
    {
        history.Clear();
        OnHistoryChanged?.Invoke();
    }

    private void OnDisable()
    {
        if (openFeedbackRoutine != null)
        {
            StopCoroutine(openFeedbackRoutine);
            openFeedbackRoutine = null;
        }

        openingFeedback = false;
    }
}