using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Main UI Frames")]
    public GameObject chatFrame;
    public GameObject articleFrame;
    public GameObject feedbackFrame;

    private Dictionary<string, GameObject> frames = new Dictionary<string, GameObject>();
    private string currentActiveFrame = "";

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

        InitializeFrames();
    }

    private void InitializeFrames()
    {
        if (chatFrame != null)
        {
            frames.Add("Chat", chatFrame);
        }

        if (articleFrame != null)
        {
            frames.Add("Article", articleFrame);
        }

        if (feedbackFrame != null)
        {
            frames.Add("Feedback", feedbackFrame);
        }

        // Do NOT close all frames - keep the scene's current state
        // Just detect which frame is currently active
        DetectCurrentActiveFrame();
    }

    private void DetectCurrentActiveFrame()
    {
        foreach (var frame in frames)
        {
            if (frame.Value != null && frame.Value.activeSelf)
            {
                currentActiveFrame = frame.Key;
                Debug.Log("UIManager: Currently active frame - " + currentActiveFrame);
                return;
            }
        }

        currentActiveFrame = "";
        Debug.Log("UIManager: No frame is currently active.");
    }

    public void OpenFrame(string frameName)
    {
        if (string.IsNullOrEmpty(frameName))
        {
            Debug.LogWarning("UIManager: Frame name is empty.");
            return;
        }

        if (!frames.ContainsKey(frameName))
        {
            Debug.LogWarning("UIManager: Frame '" + frameName + "' not found.");
            return;
        }

        // Close all frames first
        CloseAllFrames();

        // Open the requested frame
        frames[frameName].SetActive(true);
        currentActiveFrame = frameName;

        Debug.Log("UIManager: Opened frame - " + frameName);
    }

    public void CloseFrame(string frameName)
    {
        if (string.IsNullOrEmpty(frameName))
        {
            Debug.LogWarning("UIManager: Frame name is empty.");
            return;
        }

        if (!frames.ContainsKey(frameName))
        {
            Debug.LogWarning("UIManager: Frame '" + frameName + "' not found.");
            return;
        }

        frames[frameName].SetActive(false);

        if (currentActiveFrame == frameName)
        {
            currentActiveFrame = "";
        }

        Debug.Log("UIManager: Closed frame - " + frameName);
    }

    public void CloseAllFrames()
    {
        foreach (var frame in frames)
        {
            if (frame.Value != null)
            {
                frame.Value.SetActive(false);
            }
        }

        currentActiveFrame = "";
        Debug.Log("UIManager: All frames closed.");
    }

    public void ToggleFrame(string frameName)
    {
        if (string.IsNullOrEmpty(frameName))
        {
            Debug.LogWarning("UIManager: Frame name is empty.");
            return;
        }

        if (!frames.ContainsKey(frameName))
        {
            Debug.LogWarning("UIManager: Frame '" + frameName + "' not found.");
            return;
        }

        if (currentActiveFrame == frameName)
        {
            CloseFrame(frameName);
        }
        else
        {
            OpenFrame(frameName);
        }
    }

    public bool IsFrameOpen(string frameName)
    {
        if (string.IsNullOrEmpty(frameName))
        {
            return false;
        }

        if (!frames.ContainsKey(frameName))
        {
            return false;
        }

        return frames[frameName].activeSelf;
    }

    public string GetCurrentActiveFrame()
    {
        return currentActiveFrame;
    }

    public void OpenChat()
    {
        OpenFrame("Chat");
    }

    public void OpenArticle()
    {
        OpenFrame("Article");
    }

    public void OpenFeedback()
    {
        OpenFrame("Feedback");
    }

    public void CloseChat()
    {
        CloseFrame("Chat");
    }

    public void CloseArticle()
    {
        CloseFrame("Article");
    }

    public void CloseFeedback()
    {
        CloseFrame("Feedback");
    }

    public bool IsChatOpen()
    {
        return IsFrameOpen("Chat");
    }

    public bool IsArticleOpen()
    {
        return IsFrameOpen("Article");
    }

    public bool IsFeedbackOpen()
    {
        return IsFrameOpen("Feedback");
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}