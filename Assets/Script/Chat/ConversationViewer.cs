using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class ArticleDataEvent : UnityEvent<ArticleData>
{
}

public class ConversationViewer : MonoBehaviour
{
    [Header("Conversation Data")]
    public ConversationData currentConversation;

    [Header("Panel")]
    public GameObject conversationPanel;

    [Header("Header")]
    public TMP_Text npcNameText;
    public Image npcAvatarImage;
    public Button closeButton;

    [Header("Chat List")]
    public RectTransform messageContainer;
    public ScrollRect scrollRect;
    public ChatBubble npcBubblePrefab;
    public ChatBubble playerBubblePrefab;

    [Header("Typing Indicator")]
    public GameObject typingIndicator;

    [Header("Options")]
    public Transform optionsContainer;
    public GameObject optionButtonPrefab;

    [Header("Article Link")]
    [SerializeField]
    private ArticleDataEvent onArticleRequested =
        new ArticleDataEvent();

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

    [Header("Events")]
    public Action<ConversationData> OnConversationOpened;
    public Action<ConversationData> OnConversationSwitched;
    public Action<ConversationData> OnConversationClosed;
    public Action<PlayerOption> OnOptionChosen;

    private int currentNodeIndex;
    private bool isWaitingForChoice;
    private bool isTypingNPC;
    private bool pauseConversationForLink;

    private Coroutine flowRoutine;

    private readonly List<ChatBubble> spawnedBubbles =
        new List<ChatBubble>();

    private readonly List<Button> currentOptionButtons =
        new List<Button>();

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
                "[CHAT] '" +
                conversation.name +
                "' has no nodes."
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
        pauseConversationForLink = false;
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
                flowRoutine = null;
                yield break;
            }

            if (currentNodeIndex < 0 ||
                currentNodeIndex >=
                currentConversation.Nodes.Count)
            {
                yield return EndRoutine();

                flowRoutine = null;
                yield break;
            }

            ConversationNode node =
                currentConversation.Nodes[
                    currentNodeIndex
                ];

            if (node == null || node.IsEndNode)
            {
                yield return EndRoutine();

                flowRoutine = null;
                yield break;
            }

            if (node.IsNPC)
            {
                yield return PlayNPCNode(node);

                if (pauseConversationForLink)
                {
                    flowRoutine = null;
                    yield break;
                }
            }
            else
            {
                yield return PlayPlayerNode(node);

                flowRoutine = null;
                yield break;
            }
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

        ChatBubble bubble =
            SpawnBubble(true);

        string displayText = node.Message;
        string linkURL =
            GetNodeLinkURL(node);

        bool isLinkNode =
            node.DisplayType == BlockType.Link;

        bool hasValidLink =
            node.LinkedArticle != null ||
            !string.IsNullOrWhiteSpace(linkURL);

        if (isLinkNode)
        {
            displayText = BuildLinkMessage(
                displayText,
                linkURL
            );
        }

        if (displayText == null)
        {
            displayText = string.Empty;
        }

        float estimatedTime = 0f;

        if (bubble != null)
        {
            bubble.StartTyping(
                displayText,
                true,
                charInterval
            );

            if (isLinkNode && hasValidLink)
            {
                bubble.ConfigureAsLink(
                    node.LinkedArticle,
                    linkURL,
                    OpenChatLink
                );
            }
            else
            {
                bubble.ConfigureAsNormalBubble();
            }

            ScrollToBottom();

            estimatedTime = Mathf.Max(
                0.05f,
                displayText.Length * charInterval
            );
        }

        yield return new WaitForSeconds(
            estimatedTime
        );

        isTypingNPC = false;

        if (isLinkNode && hasValidLink)
        {
            pauseConversationForLink = true;
            yield break;
        }

        if (isLinkNode && !hasValidLink)
        {
            Debug.LogWarning(
                "[CHAT] Link node has no " +
                "LinkedArticle or LinkURL."
            );
        }

        if (node.AutoAdvance)
        {
            yield return new WaitForSeconds(
                node.AutoAdvanceDelay
            );

            AdvanceToNextNode(
                currentNodeIndex + 1
            );

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
        if (optionsContainer == null ||
            optionButtonPrefab == null)
        {
            yield return new WaitForSeconds(
                0.3f
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

        Button button =
            buttonObject.GetComponent<Button>();

        TMP_Text buttonText =
            buttonObject.GetComponentInChildren<
                TMP_Text
            >();

        if (buttonText != null)
        {
            buttonText.text = label;
        }

        if (button != null)
        {
            button.onClick.AddListener(
                delegate
                {
                    clicked = true;
                }
            );
        }

        ScrollToBottom();

        while (!clicked)
        {
            yield return null;
        }

        ClearOptions();
    }

    private IEnumerator EndRoutine()
    {
        ClearOptions();

        if (optionsContainer != null &&
            optionButtonPrefab != null)
        {
            GameObject endButtonObject =
                Instantiate(
                    optionButtonPrefab,
                    optionsContainer
                );

            Button endButton =
                endButtonObject.GetComponent<Button>();

            TMP_Text endButtonText =
                endButtonObject
                    .GetComponentInChildren<
                        TMP_Text
                    >();

            if (endButtonText != null)
            {
                endButtonText.text =
                    "End Conversation";
            }

            if (endButton != null)
            {
                endButton.onClick.AddListener(
                    CloseConversation
                );
            }

            ScrollToBottom();
        }

        yield break;
    }

    private void CreateOptionButtons(
        List<PlayerOption> options)
    {
        if (optionsContainer == null ||
            optionButtonPrefab == null)
        {
            Debug.LogWarning(
                "[CHAT] Options container or " +
                "option prefab is missing."
            );

            return;
        }

        currentOptionButtons.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            PlayerOption option = options[i];

            GameObject buttonObject =
                Instantiate(
                    optionButtonPrefab,
                    optionsContainer
                );

            Button button =
                buttonObject.GetComponent<Button>();

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

        ScrollToBottom();
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
            Button button =
                currentOptionButtons[i];

            if (button != null)
            {
                button.interactable = value;
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
                "[CHAT] Option '" +
                option.OptionText +
                "' has invalid NextNodeIndex: " +
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

        currentNodeIndex = nextIndex;
        pauseConversationForLink = false;

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
                "[CHAT] Bubble prefab or " +
                "message container is missing."
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

        return bubble;
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

    private string BuildLinkMessage(
        string message,
        string url)
    {
        string finalMessage = message;

        if (string.IsNullOrWhiteSpace(
            finalMessage
        ))
        {
            return url;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return finalMessage;
        }

        if (!finalMessage.Contains(url))
        {
            finalMessage =
                finalMessage +
                "\n" +
                url;
        }

        return finalMessage;
    }

    private void OpenChatLink(
        ArticleData linkedArticle,
        string linkURL)
    {
        if (linkedArticle != null)
        {
            onArticleRequested.Invoke(
                linkedArticle
            );

            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenArticle();
            }
            else
            {
                Debug.LogWarning(
                    "[CHAT] UIManager.Instance " +
                    "was not found."
                );
            }

            return;
        }

        if (IsValidWebURL(linkURL))
        {
            Application.OpenURL(linkURL);
            return;
        }

        Debug.LogWarning(
            "[CHAT] Link bubble has no valid " +
            "LinkedArticle or LinkURL."
        );
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