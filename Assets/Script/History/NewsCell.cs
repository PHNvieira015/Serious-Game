using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NewsCell : MonoBehaviour
{
    [Header("References")]
    public TMP_Text titleText;
    public Button clickButton;

    [Header("Colors")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    public Color pressedColor = new Color(0.1f, 0.1f, 0.1f, 1f);

    private ArticleData linkedArticle;
    private System.Action<ArticleData> onClickAction;

    private void Awake()
    {
        if (clickButton == null)
        {
            clickButton = GetComponent<Button>();
        }

        if (titleText == null)
        {
            titleText = GetComponentInChildren<TMP_Text>();
        }

        if (clickButton != null)
        {
            ColorBlock colors = clickButton.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = hoverColor;
            colors.pressedColor = pressedColor;
            clickButton.colors = colors;
        }
    }

    public void Initialize(ArticleData article, System.Action<ArticleData> onClick)
    {
        linkedArticle = article;
        onClickAction = onClick;

        if (titleText != null && article != null)
        {
            titleText.text = article.Title;
        }

        if (clickButton != null)
        {
            clickButton.onClick.RemoveAllListeners();
            clickButton.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (onClickAction != null && linkedArticle != null)
        {
            onClickAction.Invoke(linkedArticle);
        }
    }

    public ArticleData GetArticleData()
    {
        return linkedArticle;
    }
}