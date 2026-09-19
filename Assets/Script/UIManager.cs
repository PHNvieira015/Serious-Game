using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [System.Serializable]
    public enum Screens
    {
        None,
        Chat,
        Article,
        Feedback,
        History,
        Score
    }

    [Header("Current Screen")]
    public Screens currentScreen= Screens.Chat;

    [Header("Screens")]
    [SerializeField] private GameObject chatFrame;
    [SerializeField] private GameObject articleFrame;
    [SerializeField] private GameObject feedbackFrame;
    [SerializeField] private GameObject historyFrame;
    [SerializeField] private GameObject scoreFrame;

    private void Awake()
    {
        Instance = this;

        DetectCurrentScreen();
    }

    public void ShowScreen(Screens screen)
    {
        Debug.Log("Showing screen: " + screen);

        currentScreen = screen;

        if (chatFrame != null)
        {
            chatFrame.SetActive(false);
        }

        if (articleFrame != null)
        {
            articleFrame.SetActive(false);
        }

        if (feedbackFrame != null)
        {
            feedbackFrame.SetActive(false);
        }

        if (historyFrame != null)
        {
            historyFrame.SetActive(false);
        }

        if (scoreFrame != null)
        {
            scoreFrame.SetActive(false);
        }

        switch (currentScreen)
        {
            case Screens.None:
                break;

            case Screens.Chat:
                if (chatFrame != null)
                {
                    chatFrame.SetActive(true);
                }
                break;

            case Screens.Article:
                if (articleFrame != null)
                {
                    articleFrame.SetActive(true);
                }
                break;

            case Screens.Feedback:
                if (feedbackFrame != null)
                {
                    feedbackFrame.SetActive(true);
                }
                break;

            case Screens.History:
                if (historyFrame != null)
                {
                    historyFrame.SetActive(true);
                }
                break;

            case Screens.Score:
                if (scoreFrame != null)
                {
                    scoreFrame.SetActive(true);
                }
                break;
        }
    }

    public void ShowScreen(int screen)
    {
        ShowScreen((Screens)screen);
    }

    private void DetectCurrentScreen()
    {
        if (chatFrame != null && chatFrame.activeSelf)
        {
            currentScreen = Screens.Chat;
        }
        else if (articleFrame != null && articleFrame.activeSelf)
        {
            currentScreen = Screens.Article;
        }
        else if (feedbackFrame != null && feedbackFrame.activeSelf)
        {
            currentScreen = Screens.Feedback;
        }
        else if (historyFrame != null && historyFrame.activeSelf)
        {
            currentScreen = Screens.History;
        }
        else if (scoreFrame != null && scoreFrame.activeSelf)
        {
            currentScreen = Screens.Score;
        }
        else
        {
            currentScreen = Screens.None;
        }

        Debug.Log("Current screen: " + currentScreen);
    }

    public void OpenChat()
    {
        Debug.LogWarning($"[UIMANAGER] OpenChat called from:\n{System.Environment.StackTrace}");
        ShowScreen(Screens.Chat);
        
    }

    public void OpenArticle()
    {
        ShowScreen(Screens.Article);
    }

    public void OpenFeedback()
    {
        ShowScreen(Screens.Feedback);
    }

    public void OpenHistory()
    {
        ShowScreen(Screens.History);
    }

    public void OpenScore()
    {
        ShowScreen(Screens.Score);
    }

    public void CloseCurrentScreen()
    {
        ShowScreen(Screens.None);
    }

    public bool IsScreenOpen(Screens screen)
    {
        return currentScreen == screen;
    }

    public bool IsChatOpen()
    {
        return currentScreen == Screens.Chat;
    }

    public bool IsArticleOpen()
    {
        return currentScreen == Screens.Article;
    }

    public bool IsFeedbackOpen()
    {
        return currentScreen == Screens.Feedback;
    }

    public bool IsHistoryOpen()
    {
        return currentScreen == Screens.History;
    }

    public bool IsScoreOpen()
    {
        return currentScreen == Screens.Score;
    }

    public Screens GetCurrentScreen()
    {
        return currentScreen;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}