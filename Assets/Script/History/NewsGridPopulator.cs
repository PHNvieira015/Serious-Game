using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NewsGridPopulator : MonoBehaviour
{
    [Header("Grid Settings")]
    public GridLayoutGroup gridLayout;
    public GameObject cellPrefab;

    [Header("Target Parent")]
    public Transform targetParent; // Where cells will be spawned

    [Header("Article Viewer")]
    public ArticleViewer articleViewer;

    [Header("News Data")]
    public List<ArticleData> newsArticles = new List<ArticleData>();

    private void Start()
    {
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        if (targetParent == null)
        {
            targetParent = transform;
        }

        if (articleViewer == null)
        {
            articleViewer = FindFirstObjectByType<ArticleViewer>();
        }

        PopulateGrid();
    }

    public void PopulateGrid()
    {
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        if (cellPrefab == null)
        {
            Debug.LogError("Cell Prefab not assigned!");
            return;
        }

        if (articleViewer == null)
        {
            Debug.LogError("ArticleViewer not assigned!");
            return;
        }

        if (targetParent == null)
        {
            targetParent = transform;
        }

        // Clear existing cells from target parent
        foreach (Transform child in targetParent)
        {
            Destroy(child.gameObject);
        }

        // Create cells from news articles
        foreach (ArticleData article in newsArticles)
        {
            if (article == null)
                continue;

            GameObject cell = Instantiate(cellPrefab, targetParent);
            cell.SetActive(true);

            NewsCell newsCell = cell.GetComponent<NewsCell>();
            if (newsCell != null)
            {
                newsCell.Initialize(article, OnCellClicked);
            }
            else
            {
                TMP_Text text = cell.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = article.Title;
                }

                Button button = cell.GetComponent<Button>();
                if (button != null)
                {
                    ArticleData articleRef = article;
                    button.onClick.AddListener(() => OnCellClicked(articleRef));
                }
            }
        }
    }

    private void OnCellClicked(ArticleData article)
    {
        if (articleViewer != null && article != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenArticle();
            }

            articleViewer.LoadArticle(article);
            Debug.Log("Loaded article: " + article.Title);
        }
    }

    public void SetNewsData(List<ArticleData> articles)
    {
        newsArticles = articles;
        PopulateGrid();
    }
}