using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConversationViewer : MonoBehaviour
{
    [Header("Conversation Data")]
    public ConversationData currentConversation;

    [Header("Panel")]
    public GameObject conversationPanel;

    [Header("Header")]
    public TMP_Text npcNameText;
    public UnityEngine.UI.Image npcAvatarImage;
    public UnityEngine.UI.Image npcAvatarBackgroundImage;
    public UnityEngine.UI.Button closeButton;

    [Header("Chat List")]
    public RectTransform messageContainer;
    public UnityEngine.UI.ScrollRect scrollRect;
    public ChatBubble npcBubblePrefab;
    public ChatBubble playerBubblePrefab;

    [Header("Typing Indicator")]
    public GameObject typingIndicator;

    [Header("Options")]
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    [Header("Article")]
    public ArticleViewer articleViewer;
    public string openArticleLabel = "Verificar Artigo";

    [Header("Continue Button")]
    public string continueLabel = "Continuar";
    public bool requireContinueButton = true;

    [Header("Settings")]
    public float initialDelay = 0.3f;
    public float bubbleDelay = 0.4f;
    public float optionDelay = 0.3f;
    public float charInterval = 0.02f;

    [Header("Option Behavior")]
    public bool echoOptionAsPlayerBubble = true;
    public bool lockOptionsUntilTypingDone = true;
    public bool disableOptionOnClick = true;
    public bool closeIfOptionTargetInvalid = false;

    [Header("Debug")]
    public bool showDebugMessages = true;

    [Header("Events")]
    public Action<ConversationData> OnConversationOpened;
    public Action<ConversationData> OnConversationSwitched;
    public Action<ConversationData> OnConversationClosed;
    public Action<PlayerOption> OnOptionChosen;

    private class BubbleRecord
    {
        public string Text;
        public bool IsNPC;
        public bool IsLink;
        public ArticleData LinkedArticle;
        public string LinkedURL;
    }

    private class ConversationProgress
    {
        public int NodeIndex;
        public bool MessageShown;

        public readonly List<BubbleRecord> Bubbles =
            new List<BubbleRecord>();
    }

    private static readonly Dictionary<
        ConversationData,
        ConversationProgress
    > conversationProgress =
        new Dictionary<ConversationData, ConversationProgress>();

    private ConversationProgress activeProgress;

    private bool openingConversation;
    private bool suspended;
    private bool isWaitingForChoice;
    private bool isTypingNPC;

    private ArticleData preparedArticle;
    private string preparedURL;

    private Coroutine flowRoutine;

    private readonly List<ChatBubble> spawnedBubbles =
        new List<ChatBubble>();

    private readonly List<UnityEngine.UI.Button> currentOptionButtons =
        new List<UnityEngine.UI.Button>();

    private int currentNodeIndex
    {
        get
        {
            return activeProgress != null
                ? activeProgress.NodeIndex
                : 0;
        }
        set
        {
            if (activeProgress != null)
            {
                activeProgress.NodeIndex = value;
                activeProgress.MessageShown = false;
            }
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearSessionProgress()
    {
        conversationProgress.Clear();
    }

    private void Awake()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        SetTypingIndicator(false);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseConversation);
            closeButton.onClick.AddListener(CloseConversation);
        }
    }

    public void OpenConversation(ConversationData conversation)
    {
        if (conversation == null ||
            conversation.Nodes == null ||
            conversation.Nodes.Count == 0)
        {
            Debug.LogError("[CHAT] Conversation is null or has no nodes.");
            return;
        }

        bool wasOpen =
            currentConversation != null &&
            conversationPanel != null &&
            conversationPanel.activeInHierarchy;

        Teardown();

        currentConversation = conversation;

        if (!conversationProgress.TryGetValue(
            conversation,
            out activeProgress))
        {
            activeProgress = new ConversationProgress
            {
                NodeIndex = conversation.StartNodeIndex
            };

            conversationProgress.Add(conversation, activeProgress);
        }

        openingConversation = true;

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(true);
        }

        openingConversation = false;
        suspended = false;

        UpdateHeader();
        RestoreBubbles();

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Opening " +
                conversation.name +
                " at node " +
                currentNodeIndex
            );
        }

        if (wasOpen)
        {
            OnConversationSwitched?.Invoke(conversation);
        }
        else
        {
            OnConversationOpened?.Invoke(conversation);
        }

        StartFlow();
    }

    public void StartConversation(ConversationData conversation)
    {
        OpenConversation(conversation);
    }

    public void CloseConversation()
    {
        ConversationData closedConversation = currentConversation;

        Teardown();

        currentConversation = null;
        activeProgress = null;

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        OnConversationClosed?.Invoke(closedConversation);
    }

    private void UpdateHeader()
    {
        if (currentConversation == null)
        {
            return;
        }

        if (npcNameText != null)
        {
            npcNameText.text = currentConversation.NPCSpeakerName;
        }

        if (npcAvatarImage != null)
        {
            npcAvatarImage.sprite =
                currentConversation.NPCSpeakerAvatar;

            npcAvatarImage.enabled =
                currentConversation.NPCSpeakerAvatar != null;
        }

        if (npcAvatarBackgroundImage != null)
        {
            npcAvatarBackgroundImage.sprite =
                currentConversation.NPCSpeakerBackground;

            npcAvatarBackgroundImage.enabled =
                currentConversation.NPCSpeakerBackground != null;
        }
    }

    private void Teardown()
    {
        StopAllCoroutines();
        flowRoutine = null;

        StopAllTyping();
        ClearChat();
        SetTypingIndicator(false);

        isWaitingForChoice = false;
        isTypingNPC = false;
        suspended = false;

        preparedArticle = null;
        preparedURL = string.Empty;
    }

    private bool CanRunFlow()
    {
        return isActiveAndEnabled &&
            (conversationPanel == null ||
             conversationPanel.activeInHierarchy);
    }

    private void StartFlow()
    {
        if (currentConversation == null || activeProgress == null)
        {
            return;
        }

        if (!CanRunFlow())
        {
            suspended = true;
            return;
        }

        suspended = false;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(FlowRoutine());
    }

    private IEnumerator FlowRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, initialDelay));
        yield return RefreshLayoutAndScroll();

        while (currentConversation != null && activeProgress != null)
        {
            if (currentNodeIndex < 0 ||
                currentNodeIndex >= currentConversation.Nodes.Count)
            {
                yield return EndRoutine(null);
                flowRoutine = null;
                yield break;
            }

            ConversationNode node =
                currentConversation.Nodes[currentNodeIndex];

            if (node == null)
            {
                currentNodeIndex++;
                continue;
            }

            if (showDebugMessages)
            {
                Debug.Log(
                    "[CHAT] Node: " +
                    currentNodeIndex +
                    " | Type: " +
                    node.DisplayType +
                    " | End: " +
                    node.IsEndNode
                );
            }

            if (node.IsEndNode)
            {
                PrepareArticleFromEndNode(node);

                // Link end nodes also get a clickable bubble.
                if (node.DisplayType == BlockType.Link)
                {
                    yield return DisplayNodeBubble(node);
                }

                yield return EndRoutine(node);
                flowRoutine = null;
                yield break;
            }

            if (node.IsNPC)
            {
                yield return PlayNPCNode(node);
            }
            else
            {
                yield return PlayPlayerNode(node);

                if (isWaitingForChoice)
                {
                    flowRoutine = null;
                    yield break;
                }
            }
        }

        flowRoutine = null;
    }

    private IEnumerator DisplayNodeBubble(ConversationNode node)
    {
        if (activeProgress.MessageShown)
        {
            yield break;
        }

        isTypingNPC = node.IsNPC;

        if (node.IsNPC)
        {
            SetTypingIndicator(true);
            ScrollToBottom();

            if (bubbleDelay > 0f)
            {
                yield return new WaitForSeconds(bubbleDelay);
            }

            SetTypingIndicator(false);
        }

        string text = BuildNodeMessage(node);

        if (!string.IsNullOrWhiteSpace(text))
        {
            ChatBubble bubble = ShowNewBubble(
                text,
                node.IsNPC,
                true,
                node
            );

            if (bubble != null && bubble.messageText != null)
            {
                while (bubble != null &&
                       bubble.messageText != null &&
                       bubble.messageText.text != text)
                {
                    yield return null;
                }
            }
        }

        activeProgress.MessageShown = true;
        isTypingNPC = false;

        yield return RefreshLayoutAndScroll();
    }

    private IEnumerator PlayNPCNode(ConversationNode node)
    {
        yield return DisplayNodeBubble(node);

        // Continue controls dialogue progression.
        // The link bubble itself can already open its article.
        if (node.AutoAdvance)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, node.AutoAdvanceDelay)
            );

            currentNodeIndex++;
        }
        else if (requireContinueButton)
        {
            yield return WaitForContinueButton(continueLabel);
        }
        else
        {
            currentNodeIndex++;
        }
    }

    private IEnumerator PlayPlayerNode(ConversationNode node)
    {
        ClearOptions();

        yield return DisplayNodeBubble(node);

        if (node.PlayerOptions != null &&
            node.PlayerOptions.Count > 0)
        {
            yield return new WaitForSeconds(
                Mathf.Max(0f, optionDelay)
            );

            isWaitingForChoice = true;
            CreateOptionButtons(node.PlayerOptions);
            yield break;
        }

        if (requireContinueButton &&
            !string.IsNullOrEmpty(node.Message))
        {
            yield return WaitForContinueButton(continueLabel);
        }
        else
        {
            currentNodeIndex++;
        }
    }

    private ChatBubble ShowNewBubble(
        string text,
        bool isNPC,
        bool type,
        ConversationNode node = null)
    {
        ChatBubble bubble = SpawnBubble(isNPC);

        if (bubble == null)
        {
            return null;
        }

        BubbleRecord record = new BubbleRecord
        {
            Text = text,
            IsNPC = isNPC,
            IsLink = node != null &&
                node.DisplayType == BlockType.Link,
            LinkedArticle = node != null
                ? node.LinkedArticle
                : null,
            LinkedURL = node != null
                ? GetNodeLinkURL(node)
                : string.Empty
        };

        activeProgress.Bubbles.Add(record);
        activeProgress.MessageShown = true;

        ConfigureBubble(bubble, record);

        if (type)
        {
            bubble.StartTyping(text, isNPC, charInterval);
        }
        else
        {
            bubble.SetMessage(text, isNPC);
        }

        return bubble;
    }

    private void ConfigureBubble(
        ChatBubble bubble,
        BubbleRecord record)
    {
        if (record.IsLink)
        {
            bubble.ConfigureAsLink(
                record.LinkedArticle,
                record.LinkedURL,
                OpenLinkedArticle
            );

            if (record.LinkedArticle == null &&
                !IsValidWebURL(record.LinkedURL))
            {
                Debug.LogWarning(
                    "[CHAT] Link node has no LinkedArticle or valid URL."
                );
            }
        }
        else
        {
            bubble.ConfigureAsNormalBubble();
        }
    }

    private void RestoreBubbles()
    {
        if (activeProgress == null)
        {
            return;
        }

        foreach (BubbleRecord record in activeProgress.Bubbles)
        {
            ChatBubble bubble = SpawnBubble(record.IsNPC);

            if (bubble == null)
            {
                continue;
            }

            ConfigureBubble(bubble, record);
            bubble.SetMessage(record.Text, record.IsNPC);
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Restored " +
                activeProgress.Bubbles.Count +
                " bubbles, including their article links."
            );
        }
    }

    private void OpenLinkedArticle(ArticleData article, string url)
    {
        if (article != null)
        {
            if (articleViewer == null)
            {
                Debug.LogError("[CHAT] Assign ArticleViewer.");
                return;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError("[CHAT] UIManager.Instance is missing.");
                return;
            }

            if (showDebugMessages)
            {
                Debug.Log(
                    "[CHAT] Opening article from bubble: " +
                    article.name
                );
            }

            // Finish loading before changing screens.
            articleViewer.LoadArticle(article);

            // Preserve dialogue progress even if this viewer stays active.
            SuspendConversation();

            UIManager.Instance.OpenArticle();
            return;
        }

        if (IsValidWebURL(url))
        {
            Application.OpenURL(url);
            return;
        }

        Debug.LogWarning(
            "[CHAT] This bubble has no article or valid URL assigned."
        );
    }

    private void PrepareArticleFromEndNode(ConversationNode node)
    {
        preparedArticle = node != null ? node.LinkedArticle : null;
        preparedURL = node != null
            ? GetNodeLinkURL(node)
            : string.Empty;

        // Load only when clicked, avoiding article resets on chat restore.
    }

    private void OpenPreparedArticle()
    {
        OpenLinkedArticle(preparedArticle, preparedURL);
    }

    private IEnumerator EndRoutine(ConversationNode endNode)
    {
        ClearOptions();

        bool canOpenArticle =
            preparedArticle != null ||
            IsValidWebURL(preparedURL);

        UnityEngine.UI.Button button = CreateButton(
            canOpenArticle ? openArticleLabel : "End Conversation"
        );

        if (button != null)
        {
            if (canOpenArticle)
            {
                button.onClick.AddListener(OpenPreparedArticle);
            }
            else
            {
                button.onClick.AddListener(CloseConversation);
            }
        }

        yield return RefreshLayoutAndScroll();
    }

    private IEnumerator WaitForContinueButton(string label)
    {
        ClearOptions();

        UnityEngine.UI.Button button = CreateButton(label);

        if (button == null)
        {
            // Stop instead of repeatedly processing the same node.
            while (true)
            {
                yield return null;
            }
        }

        bool clicked = false;

        button.onClick.AddListener(() =>
        {
            if (clicked)
            {
                return;
            }

            currentNodeIndex++;
            clicked = true;
            button.interactable = false;
        });

        yield return RefreshLayoutAndScroll();

        while (!clicked)
        {
            yield return null;
        }

        ClearOptions();
    }

    private UnityEngine.UI.Button CreateButton(string label)
    {
        if (optionsContainer == null || optionButtonPrefab == null)
        {
            Debug.LogError(
                "[CHAT] Assign Options Container and Option Button Prefab."
            );

            return null;
        }

        GameObject instance = Instantiate(
            optionButtonPrefab,
            optionsContainer
        );

        UnityEngine.UI.Button button =
            instance.GetComponent<UnityEngine.UI.Button>();

        TMP_Text text =
            instance.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
        {
            text.text = label;
        }

        if (button == null)
        {
            Debug.LogError("[CHAT] Option prefab needs a Button.");
            Destroy(instance);
            return null;
        }

        currentOptionButtons.Add(button);
        return button;
    }

    private void CreateOptionButtons(List<PlayerOption> options)
    {
        ClearOptions();

        foreach (PlayerOption option in options)
        {
            UnityEngine.UI.Button button =
                CreateButton(option.OptionText);

            if (button == null)
            {
                continue;
            }

            PlayerOption capturedOption = option;

            button.onClick.AddListener(() =>
            {
                OnOptionSelected(capturedOption);
            });
        }

        if (lockOptionsUntilTypingDone && isTypingNPC)
        {
            SetOptionsInteractable(false);
            StartCoroutine(EnableOptionsWhenTypingDone());
        }

        StartCoroutine(RefreshLayoutAndScroll());
    }

    private IEnumerator EnableOptionsWhenTypingDone()
    {
        while (isTypingNPC)
        {
            yield return null;
        }

        SetOptionsInteractable(true);
    }

    private void SetOptionsInteractable(bool value)
    {
        foreach (UnityEngine.UI.Button button in currentOptionButtons)
        {
            if (button != null)
            {
                button.interactable = value;
            }
        }
    }

    private void OnOptionSelected(PlayerOption option)
    {
        if (!isWaitingForChoice || currentConversation == null)
        {
            return;
        }

        if (lockOptionsUntilTypingDone && isTypingNPC)
        {
            return;
        }

        if (option.NextNodeIndex < 0 ||
            option.NextNodeIndex >= currentConversation.Nodes.Count)
        {
            Debug.LogWarning(
                "[CHAT] Invalid NextNodeIndex: " +
                option.NextNodeIndex
            );

            if (closeIfOptionTargetInvalid)
            {
                CloseConversation();
            }

            return;
        }

        isWaitingForChoice = false;

        if (disableOptionOnClick)
        {
            SetOptionsInteractable(false);
        }

        if (echoOptionAsPlayerBubble &&
            !string.IsNullOrEmpty(option.OptionText))
        {
            ShowNewBubble(option.OptionText, false, false);
            ScrollToBottom();
        }

        ClearOptions();
        AdvanceToNextNode(option.NextNodeIndex);

        OnOptionChosen?.Invoke(option);
    }

    private void AdvanceToNextNode(int nextIndex)
    {
        if (currentConversation == null)
        {
            return;
        }

        currentNodeIndex = nextIndex;
        StartFlow();
    }

    private ChatBubble SpawnBubble(bool isNPC)
    {
        ChatBubble prefab =
            isNPC ? npcBubblePrefab : playerBubblePrefab;

        if (prefab == null || messageContainer == null)
        {
            Debug.LogWarning(
                "[CHAT] Assign bubble prefabs and Message Container."
            );

            return null;
        }

        ChatBubble bubble = Instantiate(prefab, messageContainer);
        bubble.gameObject.SetActive(true);

        spawnedBubbles.Add(bubble);
        return bubble;
    }

    private string BuildNodeMessage(ConversationNode node)
    {
        string text = node.Message ?? string.Empty;

        if (node.DisplayType != BlockType.Link)
        {
            return text;
        }

        string url = GetNodeLinkURL(node);

        if (!string.IsNullOrWhiteSpace(url))
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return url;
            }

            if (!text.Contains(url))
            {
                text += "\n" + url;
            }
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return node.LinkedArticle != null
                ? openArticleLabel + ": " + node.LinkedArticle.Title
                : openArticleLabel;
        }

        return text;
    }

    private string GetNodeLinkURL(ConversationNode node)
    {
        if (node == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(node.LinkURL))
        {
            return node.LinkURL.Trim();
        }

        if (node.LinkedArticle != null &&
            !string.IsNullOrWhiteSpace(node.LinkedArticle.URL_site))
        {
            return node.LinkedArticle.URL_site.Trim();
        }

        return string.Empty;
    }

    private bool IsValidWebURL(string url)
    {
        Uri parsed;

        return !string.IsNullOrWhiteSpace(url) &&
            Uri.TryCreate(url, UriKind.Absolute, out parsed) &&
            (parsed.Scheme == Uri.UriSchemeHttp ||
             parsed.Scheme == Uri.UriSchemeHttps);
    }

    private IEnumerator RefreshLayoutAndScroll()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (messageContainer != null)
        {
            UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(messageContainer);
        }

        Canvas.ForceUpdateCanvases();
        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void SetTypingIndicator(bool visible)
    {
        if (typingIndicator != null)
        {
            typingIndicator.SetActive(visible);
        }
    }

    private void ClearChat()
    {
        foreach (ChatBubble bubble in spawnedBubbles)
        {
            if (bubble != null)
            {
                bubble.gameObject.SetActive(false);
                Destroy(bubble.gameObject);
            }
        }

        spawnedBubbles.Clear();
        ClearOptions();
    }

    private void ClearOptions()
    {
        currentOptionButtons.Clear();

        if (optionsContainer == null)
        {
            return;
        }

        for (int i = optionsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject instance = optionsContainer.GetChild(i).gameObject;

            instance.SetActive(false);
            Destroy(instance);
        }
    }

    private void StopAllTyping()
    {
        foreach (ChatBubble bubble in spawnedBubbles)
        {
            if (bubble != null)
            {
                bubble.StopTyping();
            }
        }
    }

    private void SuspendConversation()
    {
        if (currentConversation == null || activeProgress == null)
        {
            return;
        }

        suspended = true;

        StopAllCoroutines();
        flowRoutine = null;
        StopAllTyping();

        isTypingNPC = false;
        isWaitingForChoice = false;

        SetTypingIndicator(false);
    }

    private void ResumeConversation()
    {
        if (openingConversation ||
            !suspended ||
            currentConversation == null ||
            activeProgress == null ||
            !CanRunFlow())
        {
            return;
        }

        suspended = false;

        ClearChat();
        RestoreBubbles();
        StartFlow();
    }

    private void Update()
    {
        if (openingConversation ||
            currentConversation == null ||
            activeProgress == null ||
            conversationPanel == null)
        {
            return;
        }

        if (!conversationPanel.activeInHierarchy && !suspended)
        {
            SuspendConversation();
        }
        else if (conversationPanel.activeInHierarchy && suspended)
        {
            ResumeConversation();
        }
    }

    private void OnDisable()
    {
        SuspendConversation();
    }

    private void OnEnable()
    {
        ResumeConversation();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseConversation);
        }
    }
}