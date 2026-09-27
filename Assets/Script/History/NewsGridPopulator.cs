using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewsGridPopulator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GridLayoutGroup gridLayout;
    public GameObject cellPrefab;

    [Header("Target Parent")]
    public Transform targetParent;

    [Header("History")]
    public ArticleHistoryManager historyManager;

    private ArticleHistoryManager subscribedHistory;

    private readonly List<GameObject> spawnedCells =
        new List<GameObject>();

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        SubscribeToHistory();
        PopulateGrid();
    }

    private void CacheReferences()
    {
        if (targetParent == null)
        {
            targetParent = transform;
        }

        if (gridLayout == null)
        {
            gridLayout = targetParent.GetComponent<GridLayoutGroup>();
        }

        if (historyManager == null)
        {
            historyManager = GetComponent<ArticleHistoryManager>();
        }
    }

    private void SubscribeToHistory()
    {
        if (subscribedHistory == historyManager)
        {
            return;
        }

        UnsubscribeFromHistory();

        subscribedHistory = historyManager;

        if (subscribedHistory != null)
        {
            subscribedHistory.OnHistoryChanged += PopulateGrid;
        }
    }

    private void UnsubscribeFromHistory()
    {
        if (subscribedHistory != null)
        {
            subscribedHistory.OnHistoryChanged -= PopulateGrid;
        }

        subscribedHistory = null;
    }

    public void PopulateGrid()
    {
        CacheReferences();

        if (historyManager == null)
        {
            Debug.LogError(
                "[HISTORY GRID] Assign Article History Manager.",
                this
            );
            return;
        }

        if (cellPrefab == null)
        {
            Debug.LogError(
                "[HISTORY GRID] Assign Cell Prefab.",
                this
            );
            return;
        }

        ClearSpawnedCells();

        IReadOnlyList<ArticleHistoryManager.ArticleRecord> records =
            historyManager.History;

        foreach (ArticleHistoryManager.ArticleRecord record in records)
        {
            if (record == null || record.article == null)
            {
                continue;
            }

            GameObject cell = Instantiate(cellPrefab, targetParent);
            spawnedCells.Add(cell);
            cell.SetActive(true);

            NewsCell newsCell = cell.GetComponent<NewsCell>();

            if (newsCell != null)
            {
                newsCell.Initialize(record.article, OnCellClicked);
                continue;
            }

            TMP_Text title = cell.GetComponentInChildren<TMP_Text>(true);

            if (title != null)
            {
                title.text = record.title;
            }

            Button button = cell.GetComponent<Button>();

            if (button == null)
            {
                Debug.LogWarning(
                    "[HISTORY GRID] Cell needs NewsCell or Button.",
                    cell
                );
                continue;
            }

            ArticleData capturedArticle = record.article;

            button.onClick.AddListener(
                () => OnCellClicked(capturedArticle)
            );
        }
    }

    private void OnCellClicked(ArticleData article)
    {
        if (historyManager != null)
        {
            historyManager.OpenArticleFeedback(article);
        }
    }

    private void ClearSpawnedCells()
    {
        foreach (GameObject cell in spawnedCells)
        {
            if (cell == null)
            {
                continue;
            }

            cell.SetActive(false);
            Destroy(cell);
        }

        spawnedCells.Clear();
    }

    // Kept for compatibility with existing callers.
    // The grid now gets its articles from saved history.
    public void SetNewsData(List<ArticleData> articles)
    {
        PopulateGrid();
    }

    private void OnDisable()
    {
        UnsubscribeFromHistory();
    }

    private void OnDestroy()
    {
        UnsubscribeFromHistory();
    }
}