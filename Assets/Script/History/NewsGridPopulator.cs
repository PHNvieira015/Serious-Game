using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NewsGridPopulator : MonoBehaviour
{
    [Header("History")]
    public ArticleHistoryManager historyManager;

    [Header("Existing Cells")]
    [Tooltip("Assign the 12 existing cell objects in display order.")]
    public GameObject[] cells = new GameObject[12];

    private ArticleHistoryManager subscribedHistory;

    private readonly Dictionary<Button, UnityAction> buttonListeners =
        new Dictionary<Button, UnityAction>();

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

        if (isActiveAndEnabled)
        {
            SubscribeToHistory();
        }

        if (historyManager == null)
        {
            Debug.LogError(
                "[HISTORY] Assign Article History Manager.",
                this
            );
            return;
        }

        ClearButtonListeners();

        if (cells == null)
        {
            return;
        }

        IReadOnlyList<ArticleHistoryManager.ArticleRecord> records =
            historyManager.History;

        int recordIndex = 0;

        foreach (GameObject cell in cells)
        {
            if (cell == null)
            {
                continue;
            }

            // Keep the cell container active, even without an article.
            cell.SetActive(true);

            ArticleHistoryManager.ArticleRecord record = null;

            while (recordIndex < records.Count)
            {
                ArticleHistoryManager.ArticleRecord candidate =
                    records[recordIndex];

                recordIndex++;

                if (candidate != null && candidate.article != null)
                {
                    record = candidate;
                    break;
                }
            }

            if (record == null)
            {
                SetCellInteractable(cell, false);
                continue;
            }

            NewsCell newsCell = cell.GetComponent<NewsCell>();

            if (newsCell == null)
            {
                newsCell = cell.GetComponentInChildren<NewsCell>(true);
            }

            if (newsCell != null)
            {
                newsCell.Initialize(record.article, OnCellClicked);
                SetCellInteractable(cell, true);
                continue;
            }

            TMP_Text title = cell.GetComponentInChildren<TMP_Text>(true);
            Button button = cell.GetComponentInChildren<Button>(true);

            if (title != null)
            {
                title.text = record.title;
            }

            if (button == null)
            {
                Debug.LogWarning(
                    "[HISTORY] Cell needs a NewsCell or Button: " +
                    cell.name,
                    cell
                );
                continue;
            }

            ArticleData capturedArticle = record.article;
            UnityAction action = () => OnCellClicked(capturedArticle);

            if (buttonListeners.TryGetValue(
                button,
                out UnityAction oldAction))
            {
                button.onClick.RemoveListener(oldAction);
            }

            buttonListeners[button] = action;
            button.onClick.AddListener(action);

            SetCellInteractable(cell, true);
        }
    }

    private void SetCellInteractable(GameObject cell, bool value)
    {
        Button[] buttons = cell.GetComponentsInChildren<Button>(true);

        foreach (Button button in buttons)
        {
            button.interactable = value;
        }
    }

    private void OnCellClicked(ArticleData article)
    {
        if (historyManager != null &&
            article != null &&
            historyManager.ContainsArticle(article))
        {
            historyManager.OpenArticleFeedback(article);
        }
    }

    private void ClearButtonListeners()
    {
        foreach (KeyValuePair<Button, UnityAction> entry in buttonListeners)
        {
            if (entry.Key != null)
            {
                entry.Key.onClick.RemoveListener(entry.Value);
            }
        }

        buttonListeners.Clear();
    }

    // Retained for existing callers.
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
        ClearButtonListeners();
    }
}