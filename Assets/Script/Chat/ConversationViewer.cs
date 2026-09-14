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
    public string openArticleLabel = "Open Article";

    [Header("Continue Button")]
    public string continueLabel = "Continue";
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

    private int currentNodeIndex;
    private bool isWaitingForChoice;
    private bool isTypingNPC;

    private ArticleData preparedArticle;
    private string preparedURL;

    private Coroutine flowRoutine;

    private readonly List<ChatBubble> spawnedBubbles =
        new List<ChatBubble>();

    private readonly List<UnityEngine.UI.Button> currentOptionButtons =
        new List<UnityEngine.UI.Button>();

    private void Awake()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                CloseConversation
            );

            closeButton.onClick.AddListener(
                CloseConversation
            );
        }
    }

    public void OpenConversation(
        ConversationData conversation)
    {
        if (conversation == null)
        {
            Debug.LogError(
                "[CHAT] ConversationData is null."
            );

            return;
        }

        if (conversation.Nodes == null ||
            conversation.Nodes.Count == 0)
        {
            Debug.LogError(
                "[CHAT] Conversation has no nodes: " +
                conversation.name
            );

            return;
        }

        bool wasOpen =
            currentConversation != null &&
            conversationPanel != null &&
            conversationPanel.activeSelf;

        Teardown();

        currentConversation = conversation;
        currentNodeIndex =
            currentConversation.StartNodeIndex;

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Opening conversation: " +
                currentConversation.name
            );

            Debug.Log(
                "[CHAT] Start node index: " +
                currentNodeIndex
            );

            Debug.Log(
                "[CHAT] Total nodes: " +
                currentConversation.Nodes.Count
            );
        }

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(true);
        }

        UpdateHeader();

        if (wasOpen)
        {
            if (OnConversationSwitched != null)
            {
                OnConversationSwitched.Invoke(
                    currentConversation
                );
            }
        }
        else
        {
            if (OnConversationOpened != null)
            {
                OnConversationOpened.Invoke(
                    currentConversation
                );
            }
        }

        flowRoutine = StartCoroutine(
            FlowRoutine()
        );
    }

    public void StartConversation(
        ConversationData conversation)
    {
        OpenConversation(conversation);
    }

    public void CloseConversation()
    {
        ConversationData closedConversation =
            currentConversation;

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Closing conversation."
            );
        }

        Teardown();

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        currentConversation = null;

        if (OnConversationClosed != null)
        {
            OnConversationClosed.Invoke(
                closedConversation
            );
        }
    }

    private void UpdateHeader()
    {
        if (currentConversation == null)
        {
            return;
        }

        if (npcNameText != null)
        {
            npcNameText.text =
                currentConversation.NPCSpeakerName;
        }

        if (npcAvatarImage != null)
        {
            npcAvatarImage.sprite =
                currentConversation.NPCSpeakerAvatar;

            npcAvatarImage.enabled =
                currentConversation.NPCSpeakerAvatar !=
                null;
        }
    }

    private void Teardown()
    {
        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
            flowRoutine = null;
        }

        StopAllCoroutines();
        StopAllTyping();
        ClearChat();

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }

        isWaitingForChoice = false;
        isTypingNPC = false;

        preparedArticle = null;
        preparedURL = string.Empty;

        currentNodeIndex = 0;
    }

    private IEnumerator FlowRoutine()
    {
        yield return new WaitForSeconds(
            initialDelay
        );

        while (true)
        {
            if (currentConversation == null)
            {
                Debug.LogWarning(
                    "[CHAT] Conversation became null."
                );

                flowRoutine = null;
                yield break;
            }

            if (currentNodeIndex < 0 ||
                currentNodeIndex >=
                currentConversation.Nodes.Count)
            {
                if (showDebugMessages)
                {
                    Debug.Log(
                        "[CHAT] Reached the end of the node list."
                    );
                }

                yield return EndRoutine(null);

                flowRoutine = null;
                yield break;
            }

            ConversationNode node =
                currentConversation.Nodes[
                    currentNodeIndex
                ];

            if (node == null)
            {
                Debug.LogWarning(
                    "[CHAT] Node is null at index: " +
                    currentNodeIndex
                );

                currentNodeIndex =
                    currentNodeIndex + 1;

                continue;
            }

            LogCurrentNode(node);

            /*
             * Is End Node is now the validation point.
             */
            if (node.IsEndNode)
            {
                if (showDebugMessages)
                {
                    Debug.Log(
                        "[CHAT] End node detected at index: " +
                        currentNodeIndex
                    );
                }

                PrepareArticleFromEndNode(node);

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

                flowRoutine = null;
                yield break;
            }
        }
    }

    private void LogCurrentNode(
        ConversationNode node)
    {
        if (!showDebugMessages)
        {
            return;
        }

        string articleName = "None";

        if (node.LinkedArticle != null)
        {
            articleName =
                node.LinkedArticle.name;
        }

        Debug.Log(
            "[CHAT] Processing node index: " +
            currentNodeIndex +
            " | IsNPC: " +
            node.IsNPC +
            " | IsEndNode: " +
            node.IsEndNode +
            " | DisplayType: " +
            node.DisplayType +
            " | LinkedArticle: " +
            articleName
        );
    }

    private void PrepareArticleFromEndNode(
        ConversationNode endNode)
    {
        preparedArticle = null;
        preparedURL = string.Empty;

        if (endNode == null)
        {
            Debug.LogWarning(
                "[CHAT] Cannot prepare article because " +
                "the end node is null."
            );

            return;
        }

        preparedArticle =
            endNode.LinkedArticle;

        preparedURL =
            GetNodeLinkURL(endNode);

        if (preparedArticle == null)
        {
            Debug.LogWarning(
                "[CHAT] End node has no LinkedArticle."
            );

            if (!string.IsNullOrWhiteSpace(
                preparedURL
            ))
            {
                Debug.Log(
                    "[CHAT] End node has an external URL: " +
                    preparedURL
                );
            }

            return;
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] End node LinkedArticle found: " +
                preparedArticle.name
            );

            Debug.Log(
                "[CHAT] End node article title: " +
                preparedArticle.Title
            );
        }

        if (articleViewer == null)
        {
            Debug.LogError(
                "[CHAT] ArticleViewer is not assigned " +
                "in ConversationViewer."
            );

            return;
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Loading end node article into " +
                "ArticleViewer."
            );
        }

        articleViewer.LoadArticle(
            preparedArticle
        );

        if (articleViewer.currentArticle ==
            preparedArticle)
        {
            Debug.Log(
                "[CHAT] ArticleViewer currentArticle was " +
                "changed successfully to: " +
                preparedArticle.name
            );
        }
        else
        {
            Debug.LogError(
                "[CHAT] ArticleViewer currentArticle did not " +
                "change to the expected article."
            );
        }
    }

    private IEnumerator PlayNPCNode(
        ConversationNode node)
    {
        isTypingNPC = true;

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(true);
            ScrollToBottom();
        }

        if (bubbleDelay > 0f)
        {
            yield return new WaitForSeconds(
                bubbleDelay
            );
        }

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }

        string displayText =
            BuildNodeMessage(node);

        if (!string.IsNullOrWhiteSpace(
            displayText
        ))
        {
            ChatBubble messageBubble =
                SpawnBubble(true);

            if (messageBubble != null)
            {
                messageBubble.ConfigureAsNormalBubble();

                messageBubble.StartTyping(
                    displayText,
                    true,
                    charInterval
                );

                float messageTypingTime =
                    Mathf.Max(
                        0.05f,
                        displayText.Length *
                        charInterval
                    );

                yield return new WaitForSeconds(
                    messageTypingTime
                );

                yield return RefreshLayoutAndScroll();
            }
        }

        isTypingNPC = false;

        if (node.AutoAdvance)
        {
            yield return new WaitForSeconds(
                node.AutoAdvanceDelay
            );

            currentNodeIndex =
                currentNodeIndex + 1;

            yield break;
        }

        if (requireContinueButton)
        {
            yield return WaitForContinueButton(
                continueLabel
            );
        }

        int nextPlayerIndex =
            FindNextPlayerNodeIndex(
                currentNodeIndex + 1
            );

        if (nextPlayerIndex >= 0)
        {
            currentNodeIndex =
                nextPlayerIndex;

            yield return PlayPlayerNode(
                currentConversation.Nodes[
                    nextPlayerIndex
                ]
            );
        }
        else
        {
            currentNodeIndex =
                currentNodeIndex + 1;
        }
    }

    private IEnumerator PlayPlayerNode(
        ConversationNode node)
    {
        ClearOptions();

        if (!string.IsNullOrEmpty(node.Message))
        {
            ChatBubble bubble =
                SpawnBubble(false);

            if (bubble != null)
            {
                bubble.ConfigureAsNormalBubble();

                bubble.StartTyping(
                    node.Message,
                    false,
                    charInterval
                );

                ScrollToBottom();
            }
        }

        if (node.PlayerOptions != null &&
            node.PlayerOptions.Count > 0)
        {
            yield return new WaitForSeconds(
                optionDelay
            );

            isWaitingForChoice = true;

            CreateOptionButtons(
                node.PlayerOptions
            );

            yield break;
        }

        if (requireContinueButton &&
            !string.IsNullOrEmpty(node.Message))
        {
            yield return WaitForContinueButton(
                continueLabel
            );
        }

        AdvanceToNextNode(
            currentNodeIndex + 1
        );
    }

    private IEnumerator WaitForContinueButton(
        string label)
    {
        if (optionsContainer == null)
        {
            Debug.LogError(
                "[CHAT] Options Container is not assigned."
            );

            yield break;
        }

        if (optionButtonPrefab == null)
        {
            Debug.LogError(
                "[CHAT] Option Button Prefab is not assigned."
            );

            yield break;
        }

        ClearOptions();

        bool clicked = false;

        GameObject buttonObject =
            Instantiate(
                optionButtonPrefab,
                optionsContainer
            );

        UnityEngine.UI.Button button =
            buttonObject.GetComponent<
                UnityEngine.UI.Button
            >();

        TMP_Text buttonText =
            buttonObject.GetComponentInChildren<
                TMP_Text
            >();

        if (buttonText != null)
        {
            buttonText.text = label;
        }

        if (button == null)
        {
            Debug.LogError(
                "[CHAT] Continue button prefab has no " +
                "Button component."
            );

            Destroy(buttonObject);
            yield break;
        }

        button.onClick.AddListener(
            delegate
            {
                if (showDebugMessages)
                {
                    Debug.Log(
                        "[CHAT] Continue button pressed."
                    );
                }

                clicked = true;
            }
        );

        yield return RefreshLayoutAndScroll();

        while (!clicked)
        {
            yield return null;
        }

        ClearOptions();
    }

    private IEnumerator EndRoutine(
        ConversationNode endNode)
    {
        ClearOptions();

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Starting EndRoutine."
            );
        }

        if (optionsContainer == null)
        {
            Debug.LogError(
                "[CHAT] Options Container is not assigned."
            );

            yield break;
        }

        if (optionButtonPrefab == null)
        {
            Debug.LogError(
                "[CHAT] Option Button Prefab is not assigned."
            );

            yield break;
        }

        GameObject buttonObject =
            Instantiate(
                optionButtonPrefab,
                optionsContainer
            );

        UnityEngine.UI.Button button =
            buttonObject.GetComponent<
                UnityEngine.UI.Button
            >();

        TMP_Text buttonText =
            buttonObject.GetComponentInChildren<
                TMP_Text
            >();

        if (button == null)
        {
            Debug.LogError(
                "[CHAT] Final button prefab has no " +
                "Button component."
            );

            Destroy(buttonObject);
            yield break;
        }

        bool hasPreparedArticle =
            preparedArticle != null;

        bool hasPreparedURL =
            IsValidWebURL(preparedURL);

        if (showDebugMessages)
        {
            string preparedArticleName = "None";

            if (preparedArticle != null)
            {
                preparedArticleName =
                    preparedArticle.name;
            }

            Debug.Log(
                "[CHAT] Final button validation. " +
                "PreparedArticle: " +
                preparedArticleName +
                " | PreparedURL: " +
                preparedURL
            );
        }

        if (hasPreparedArticle)
        {
            if (buttonText != null)
            {
                buttonText.text =
                    openArticleLabel;
            }

            button.onClick.AddListener(
                OpenPreparedArticle
            );

            Debug.Log(
                "[CHAT] Created Open Article button for: " +
                preparedArticle.name
            );
        }
        else if (hasPreparedURL)
        {
            if (buttonText != null)
            {
                buttonText.text =
                    openArticleLabel;
            }

            button.onClick.AddListener(
                OpenPreparedArticle
            );

            Debug.Log(
                "[CHAT] Created external URL button for: " +
                preparedURL
            );
        }
        else
        {
            if (buttonText != null)
            {
                buttonText.text =
                    "End Conversation";
            }

            button.onClick.AddListener(
                CloseConversation
            );

            Debug.LogWarning(
                "[CHAT] End node has no LinkedArticle. " +
                "Created End Conversation button instead."
            );
        }

        yield return RefreshLayoutAndScroll();
    }

    private void OpenPreparedArticle()
    {
        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Open Article button pressed."
            );
        }

        if (preparedArticle != null)
        {
            if (articleViewer == null)
            {
                Debug.LogError(
                    "[CHAT] Cannot open article because " +
                    "ArticleViewer is not assigned."
                );

                return;
            }

            if (showDebugMessages)
            {
                Debug.Log(
                    "[CHAT] Loading article from final button: " +
                    preparedArticle.name
                );
            }

            articleViewer.LoadArticle(
                preparedArticle
            );

            if (articleViewer.currentArticle !=
                preparedArticle)
            {
                Debug.LogError(
                    "[CHAT] ArticleViewer did not load " +
                    "the expected article."
                );

                return;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError(
                    "[CHAT] UIManager.Instance was not found."
                );

                return;
            }

            if (showDebugMessages)
            {
                Debug.Log(
                    "[CHAT] Activating Article screen."
                );
            }

            UIManager.Instance.OpenArticle();

            if (UIManager.Instance.IsArticleOpen())
            {
                Debug.Log(
                    "[CHAT] Article screen opened successfully."
                );
            }
            else
            {
                Debug.LogError(
                    "[CHAT] Article screen did not open."
                );
            }

            return;
        }

        if (IsValidWebURL(preparedURL))
        {
            Debug.Log(
                "[CHAT] Opening external URL: " +
                preparedURL
            );

            Application.OpenURL(preparedURL);
            return;
        }

        Debug.LogError(
            "[CHAT] OpenPreparedArticle was called, but " +
            "there is no prepared article or valid URL."
        );
    }

    private void CreateOptionButtons(
        List<PlayerOption> options)
    {
        if (optionsContainer == null ||
            optionButtonPrefab == null)
        {
            Debug.LogWarning(
                "[CHAT] Options Container or " +
                "Option Button Prefab is missing."
            );

            return;
        }

        currentOptionButtons.Clear();

        for (
            int i = 0;
            i < options.Count;
            i++
        )
        {
            PlayerOption option =
                options[i];

            GameObject buttonObject =
                Instantiate(
                    optionButtonPrefab,
                    optionsContainer
                );

            UnityEngine.UI.Button button =
                buttonObject.GetComponent<
                    UnityEngine.UI.Button
                >();

            TMP_Text buttonText =
                buttonObject.GetComponentInChildren<
                    TMP_Text
                >();

            if (buttonText != null)
            {
                buttonText.text =
                    option.OptionText;
            }

            if (button != null)
            {
                PlayerOption capturedOption =
                    option;

                button.onClick.AddListener(
                    delegate
                    {
                        OnOptionSelected(
                            capturedOption
                        );
                    }
                );

                currentOptionButtons.Add(
                    button
                );
            }
        }

        if (lockOptionsUntilTypingDone &&
            isTypingNPC)
        {
            SetOptionsInteractable(false);

            StartCoroutine(
                EnableOptionsWhenTypingDone()
            );
        }

        StartCoroutine(
            RefreshLayoutAndScroll()
        );
    }

    private IEnumerator EnableOptionsWhenTypingDone()
    {
        while (isTypingNPC)
        {
            yield return null;
        }

        SetOptionsInteractable(true);
    }

    private void SetOptionsInteractable(
        bool value)
    {
        for (
            int i = 0;
            i < currentOptionButtons.Count;
            i++
        )
        {
            UnityEngine.UI.Button currentButton =
                currentOptionButtons[i];

            if (currentButton != null)
            {
                currentButton.interactable =
                    value;
            }
        }
    }

    private void OnOptionSelected(
        PlayerOption option)
    {
        if (!isWaitingForChoice)
        {
            return;
        }

        if (lockOptionsUntilTypingDone &&
            isTypingNPC)
        {
            return;
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Player option selected: " +
                option.OptionText
            );
        }

        if (OnOptionChosen != null)
        {
            OnOptionChosen.Invoke(option);
        }

        isWaitingForChoice = false;

        if (disableOptionOnClick)
        {
            SetOptionsInteractable(false);
        }

        if (echoOptionAsPlayerBubble &&
            !string.IsNullOrEmpty(
                option.OptionText
            ))
        {
            ChatBubble echoBubble =
                SpawnBubble(false);

            if (echoBubble != null)
            {
                echoBubble.ConfigureAsNormalBubble();

                echoBubble.SetMessage(
                    option.OptionText,
                    false
                );

                ScrollToBottom();
            }
        }

        ClearOptions();

        if (currentConversation == null)
        {
            return;
        }

        if (option.NextNodeIndex < 0 ||
            option.NextNodeIndex >=
            currentConversation.Nodes.Count)
        {
            Debug.LogWarning(
                "[CHAT] Option has invalid " +
                "NextNodeIndex: " +
                option.NextNodeIndex
            );

            if (closeIfOptionTargetInvalid)
            {
                CloseConversation();
            }

            return;
        }

        AdvanceToNextNode(
            option.NextNodeIndex
        );
    }

    private void AdvanceToNextNode(
        int nextIndex)
    {
        if (currentConversation == null)
        {
            return;
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Advancing from node " +
                currentNodeIndex +
                " to node " +
                nextIndex
            );
        }

        currentNodeIndex = nextIndex;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(
            FlowRoutine()
        );
    }

    private int FindNextPlayerNodeIndex(
        int startIndex)
    {
        if (currentConversation == null)
        {
            return -1;
        }

        for (
            int i = startIndex;
            i < currentConversation.Nodes.Count;
            i++
        )
        {
            ConversationNode candidate =
                currentConversation.Nodes[i];

            if (candidate == null)
            {
                continue;
            }

            if (candidate.IsNPC)
            {
                return -1;
            }

            return i;
        }

        return -1;
    }

    private ChatBubble SpawnBubble(
        bool isNPC)
    {
        ChatBubble bubblePrefab;

        if (isNPC)
        {
            bubblePrefab = npcBubblePrefab;
        }
        else
        {
            bubblePrefab = playerBubblePrefab;
        }

        if (bubblePrefab == null ||
            messageContainer == null)
        {
            Debug.LogWarning(
                "[CHAT] Bubble Prefab or " +
                "Message Container is missing."
            );

            return null;
        }

        if (conversationPanel != null &&
            !conversationPanel.activeSelf)
        {
            conversationPanel.SetActive(true);
        }

        ChatBubble bubble =
            Instantiate(
                bubblePrefab,
                messageContainer
            );

        if (!bubble.gameObject.activeSelf)
        {
            bubble.gameObject.SetActive(true);
        }

        spawnedBubbles.Add(bubble);

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHAT] Spawned bubble. IsNPC: " +
                isNPC
            );
        }

        return bubble;
    }

    private string BuildNodeMessage(
        ConversationNode node)
    {
        if (node == null)
        {
            return string.Empty;
        }

        string displayText = node.Message;

        if (displayText == null)
        {
            displayText = string.Empty;
        }

        if (node.DisplayType != BlockType.Link)
        {
            return displayText;
        }

        string linkURL =
            GetNodeLinkURL(node);

        if (string.IsNullOrWhiteSpace(
            linkURL
        ))
        {
            return displayText;
        }

        if (string.IsNullOrWhiteSpace(
            displayText
        ))
        {
            return linkURL;
        }

        if (!displayText.Contains(linkURL))
        {
            displayText =
                displayText +
                "\n" +
                linkURL;
        }

        return displayText;
    }

    private string GetNodeLinkURL(
        ConversationNode node)
    {
        if (node == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(
            node.LinkURL
        ))
        {
            return node.LinkURL.Trim();
        }

        if (node.LinkedArticle != null &&
            !string.IsNullOrWhiteSpace(
                node.LinkedArticle.URL_site
            ))
        {
            return node.LinkedArticle
                .URL_site
                .Trim();
        }

        return string.Empty;
    }

    private bool IsValidWebURL(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        bool validURL =
            Uri.TryCreate(
                url,
                UriKind.Absolute,
                out Uri result
            );

        if (!validURL)
        {
            return false;
        }

        bool isHTTP =
            result.Scheme ==
            Uri.UriSchemeHttp;

        bool isHTTPS =
            result.Scheme ==
            Uri.UriSchemeHttps;

        return isHTTP || isHTTPS;
    }

    private IEnumerator RefreshLayoutAndScroll()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        if (messageContainer != null)
        {
            UnityEngine.UI.LayoutRebuilder
                .ForceRebuildLayoutImmediate(
                    messageContainer
                );
        }

        Canvas.ForceUpdateCanvases();

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition =
                0f;
        }
    }

    private void ClearChat()
    {
        for (
            int i = 0;
            i < spawnedBubbles.Count;
            i++
        )
        {
            if (spawnedBubbles[i] != null)
            {
                Destroy(
                    spawnedBubbles[i].gameObject
                );
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

        for (
            int i = optionsContainer.childCount - 1;
            i >= 0;
            i--
        )
        {
            Destroy(
                optionsContainer
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    private void StopAllTyping()
    {
        for (
            int i = 0;
            i < spawnedBubbles.Count;
            i++
        )
        {
            if (spawnedBubbles[i] != null)
            {
                spawnedBubbles[i].StopTyping();
            }
        }
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        scrollRect.verticalNormalizedPosition =
            0f;
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                CloseConversation
            );
        }
    }
}