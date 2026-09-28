using System;
using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerMode
    {
        Manual,
        ScreenOpened,
        ArticleLinkClicked
    }

    [Header("References")]
    public TutorialManager tutorialManager;

    [Header("Trigger")]
    public TriggerMode triggerMode = TriggerMode.ScreenOpened;

    [Tooltip("Disable this until this tutorial is allowed to run.")]
    public bool triggerEnabled = true;

    [Tooltip("Run only once during this component's lifetime.")]
    public bool triggerOnce = true;

    [Header("Screen Trigger")]
    public UIManager.Screens targetScreen = UIManager.Screens.Article;

    [Header("Link Filter")]
    [Tooltip(
        "When assigned, only a bubble linked to this article can trigger."
    )]
    public ArticleData requiredArticle;

    [Tooltip(
        "Optional exact URL filter. If both filters are assigned, " +
        "both must match."
    )]
    public string requiredURL;

    [Header("Wait After Link Click")]
    public bool waitForScreenAfterLink = true;

    public UIManager.Screens screenAfterLink =
        UIManager.Screens.Article;

    [Min(0.1f)]
    public float screenWaitTimeout = 10f;

    [Header("Tutorial UI")]
    [Tooltip("Optional panel activated when this sequence starts.")]
    public GameObject tutorialUI;

    public bool hideUIOnStart = true;
    public bool hideUIWhenFinished = true;

    [Header("Steps For This Trigger")]
    public TutorialStep[] steps;

    private static event Action<ArticleData, string> LinkClicked;

    private bool hasTriggered;
    private bool sequencePending;
    private bool waitingForScreen;
    private float waitStarted;
    private bool wasOnTargetScreen;

    public bool HasTriggered => hasTriggered;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents()
    {
        LinkClicked = null;
    }

    private void Awake()
    {
        if (hideUIOnStart && tutorialUI != null)
        {
            tutorialUI.SetActive(false);
        }
    }

    private void OnEnable()
    {
        LinkClicked += HandleLinkClicked;
        wasOnTargetScreen = false;
    }

    private void Update()
    {
        if (!triggerEnabled)
        {
            return;
        }

        UIManager manager = UIManager.Instance;

        if (waitingForScreen)
        {
            if (manager != null &&
                manager.IsScreenOpen(screenAfterLink))
            {
                waitingForScreen = false;
                TriggerTutorial();
            }
            else if (Time.unscaledTime - waitStarted >= screenWaitTimeout)
            {
                waitingForScreen = false;
            }
        }

        if (triggerMode != TriggerMode.ScreenOpened)
        {
            return;
        }

        bool onTargetScreen =
            manager != null &&
            manager.IsScreenOpen(targetScreen);

        if (onTargetScreen && !wasOnTargetScreen)
        {
            TriggerTutorial();
        }

        wasOnTargetScreen = onTargetScreen;
    }

    public static void NotifyArticleLinkClicked(
        ArticleData article,
        string url)
    {
        LinkClicked?.Invoke(article, url);
    }

    private void HandleLinkClicked(ArticleData article, string url)
    {
        if (triggerMode != TriggerMode.ArticleLinkClicked ||
            !CanTrigger() ||
            waitingForScreen)
        {
            return;
        }

        if (requiredArticle != null && article != requiredArticle)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(requiredURL))
        {
            string clickedURL = (url ?? string.Empty).Trim();

            if (!string.Equals(
                clickedURL,
                requiredURL.Trim(),
                StringComparison.Ordinal))
            {
                return;
            }
        }

        if (waitForScreenAfterLink)
        {
            waitingForScreen = true;
            waitStarted = Time.unscaledTime;
        }
        else
        {
            TriggerTutorial();
        }
    }

    private bool CanTrigger()
    {
        return isActiveAndEnabled &&
            triggerEnabled &&
            !sequencePending &&
            (!triggerOnce || !hasTriggered);
    }

    public void TriggerTutorial()
    {
        if (!CanTrigger())
        {
            return;
        }

        if (tutorialManager == null)
        {
            Debug.LogWarning(
                "[TUTORIAL] Assign Tutorial Manager.",
                this
            );
            return;
        }

        sequencePending = true;

        bool accepted = tutorialManager.PlaySteps(
            steps,
            OnSequenceStarted,
            OnSequenceFinished
        );

        if (accepted)
        {
            hasTriggered = true;
        }
        else
        {
            sequencePending = false;
        }
    }

    private void OnSequenceStarted()
    {
        if (this == null)
        {
            return;
        }

        if (tutorialUI != null)
        {
            tutorialUI.SetActive(true);
        }
    }

    private void OnSequenceFinished()
    {
        if (this == null)
        {
            return;
        }

        sequencePending = false;

        if (hideUIWhenFinished && tutorialUI != null)
        {
            tutorialUI.SetActive(false);
        }
    }

    public void EnableTrigger()
    {
        triggerEnabled = true;
        wasOnTargetScreen = false;
    }

    public void DisableTrigger()
    {
        triggerEnabled = false;
        waitingForScreen = false;
    }

    public void ResetTrigger()
    {
        if (sequencePending)
        {
            return;
        }

        hasTriggered = false;
        waitingForScreen = false;
        wasOnTargetScreen = false;
    }

    private void OnDisable()
    {
        LinkClicked -= HandleLinkClicked;
        waitingForScreen = false;
    }
}