using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NewsGridPopulator : MonoBehaviour
{
    [Header("References")]
    public GridLayoutGroup gridLayout;
    public GameObject newsCellPrefab;
    public ArticleViewer articleViewer;

    [Header("News Data")]
    public List<ArticleData> newsArticles = new List<ArticleData>();

    private void Start()
    {
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
        }

        if (articleViewer == null)
        {
            articleViewer = FindObjectOfType<ArticleViewer>();
        }

        PopulateGrid();
    }

    public void PopulateGrid()
    {
        if (gridLayout == null)
        {
            Debug.LogError("GridLayoutGroup not assigned!");
            return;
        }

        if (newsCellPrefab == null)
        {
            Debug.LogError("News Cell Prefab not assigned!");
            return;
        }

        if (articleViewer == null)
        {
            Debug.LogError("ArticleViewer not assigned!");
            return;
        }

        // Clear existing cells
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create cells from news articles
        foreach (ArticleData article in newsArticles)
        {
            if (article == null)
                continue;

            GameObject cell = Instantiate(newsCellPrefab, transform);
            cell.SetActive(true);

            // Set the title text
            TMP_Text titleText = cell.GetComponentInChildren<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = article.Title;
            }

            // Get the button and add click listener
            Button button = cell.GetComponent<Button>();
            if (button == null)
            {
                button = cell.AddComponent<Button>();
            }

            // Store the article reference and add listener
            ArticleData articleRef = article;
            button.onClick.AddListener(() => OnCellClicked(articleRef));
        }
    }

    private void OnCellClicked(ArticleData article)
    {
        if (articleViewer != null)
        {
            // Open the article frame if UIManager exists
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenArticle();
            }

            // Load the article
            articleViewer.LoadArticle(article);
            Debug.Log("Loaded article: " + article.Title);
        }
        else
        {
            Debug.LogError("ArticleViewer not assigned!");
        }
    }

    public void SetNewsData(List<ArticleData> articles)
    {
        newsArticles = articles;
        PopulateGrid();
    }
}