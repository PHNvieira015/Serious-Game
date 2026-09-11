using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public enum Screens
    {
        None,
        Chat,
        Article,
        Feedback
    }

    [Header("Current Screen")]
    public Screens currentScreen;

    [Header("Screens")]
    [SerializeField] private GameObject chatFrame;
    [SerializeField] private GameObject articleFrame;
    [SerializeField] private GameObject feedbackFrame;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DetectCurrentScreen();
    }

    public void ShowScreen(Screens screen)
    {
        currentScreen = screen;

        // Close all screens
        if (chatFrame != null)
            chatFrame.SetActive(false);

        if (articleFrame != null)
            articleFrame.SetActive(false);

        if (feedbackFrame != null)
            feedbackFrame.SetActive(false);

        // Open selected screen
        switch (currentScreen)
        {
            case Screens.None:
                break;

            case Screens.Chat:
                if (chatFrame != null)
                    chatFrame.SetActive(true);
                break;

            case Screens.Article:
                if (articleFrame != null)
                    articleFrame.SetActive(true);
                break;

            case Screens.Feedback:
                if (feedbackFrame != null)
                    feedbackFrame.SetActive(true);
                break;
        }

        Debug.Log("UIManager: Current screen = " + currentScreen);
    }

    // Useful for Unity Button / Inspector
    public void ShowScreen(int screen)
    {
        ShowScreen((Screens)screen);
    }

    public void ToggleScreen(Screens screen)
    {
        if (currentScreen == screen)
        {
            ShowScreen(Screens.None);
        }
        else
        {
            ShowScreen(screen);
        }
    }

    public void CloseCurrentScreen()
    {
        ShowScreen(Screens.None);
    }

    public bool IsScreenOpen(Screens screen)
    {
        return currentScreen == screen;
    }

    public Screens GetCurrentScreen()
    {
        return currentScreen;
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
        else
        {
            currentScreen = Screens.None;
        }

        Debug.Log("UIManager: Detected current screen = " + currentScreen);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
