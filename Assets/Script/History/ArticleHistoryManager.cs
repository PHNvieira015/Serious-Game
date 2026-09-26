using System;
using System.Collections.Generic;
using UnityEngine;

public class ArticleHistoryManager : MonoBehaviour
{
    [Serializable]
    public class AnswerRecord
    {
        public int blockIndex;
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

    [Header("Article History")]
    [SerializeField]
    private List<ArticleRecord> history =
        new List<ArticleRecord>();

    public IReadOnlyList<ArticleRecord> History => history;

    public event Action OnHistoryChanged;

    public bool ContainsArticle(ArticleData article)
    {
        return GetRecord(article) != null;
    }

    public ArticleRecord GetRecord(ArticleData article)
    {
        if (article == null)
            return null;

        foreach (ArticleRecord record in history)
        {
            if (record != null && record.article == article)
                return record;
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
            return;

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

        // Replace the previous answers with independent copies.
        record.answers.Clear();

        List<BlockResult> source = result.AllBlockResults.Count > 0
            ? result.AllBlockResults
            : result.BlockResults;

        for (int i = 0; i < source.Count; i++)
        {
            BlockResult item = source[i];

            if (item == null)
                continue;

            record.answers.Add(new AnswerRecord
            {
                blockIndex = i,
                blockText = item.BlockText,
                expectedType = item.ExpectedCheck,
                markedType = item.PlayerCheck,
                resultType = item.ResultType,
                isCorrect = item.IsCorrect
            });
        }

        OnHistoryChanged?.Invoke();
    }

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

        // Loading resets the marks for a fresh attempt.
        // The old answers remain in history until validation.
        articleViewer.LoadArticle(article);

        // Always open the article, including completed articles.
        UIManager.Instance.OpenArticle();
    }

    public void OpenHistoryArticle(int index)
    {
        if (index < 0 || index >= history.Count)
            return;

        ArticleRecord record = history[index];

        if (record != null)
            OpenArticle(record.article);
    }

    public void ClearHistory()
    {
        history.Clear();
        OnHistoryChanged?.Invoke();
    }
}