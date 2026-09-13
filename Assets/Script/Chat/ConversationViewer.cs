using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private Coroutine flowRoutine;

    private readonly List<ChatBubble> spawnedBubbles = new List<ChatBubble>();
    private readonly List<Button> currentOptionButtons = new List<Button>();

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
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseConversation);
        }
    }

    public void OpenConversation(ConversationData conversation)
    {
        if (conversation == null)
        {
            Debug.LogError("[CHAT] ConversationData is null.");
            return;
        }

        if (conversation.Nodes == null || conversation.Nodes.Count == 0)
        {
            Debug.LogError($"[CHAT] '{conversation.name}' has no nodes.");
            return;
        }

        bool wasOpen = currentConversation != null
                       && conversationPanel != null
                       && conversationPanel.activeSelf;

        Teardown();

        currentConversation = conversation;
        currentNodeIndex = currentConversation.StartNodeIndex;

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(true);
        }

        UpdateHeader();

        if (wasOpen)
        {
            OnConversationSwitched?.Invoke(currentConversation);
        }
        else
        {
            OnConversationOpened?.Invoke(currentConversation);
        }

        flowRoutine = StartCoroutine(FlowRoutine());
    }

    public void StartConversation(ConversationData conversation)
    {
        OpenConversation(conversation);
    }

    public void CloseConversation()
    {
        ConversationData closed = currentConversation;

        Teardown();

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        currentConversation = null;

        OnConversationClosed?.Invoke(closed);
    }

    private void UpdateHeader()
    {
        if (npcNameText != null)
        {
            npcNameText.text = currentConversation.NPCSpeakerName;
        }

        if (npcAvatarImage != null)
        {
            npcAvatarImage.sprite = currentConversation.NPCSpeakerAvatar;
            npcAvatarImage.enabled = currentConversation.NPCSpeakerAvatar != null;
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
        currentNodeIndex = 0;
    }

    private IEnumerator FlowRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            if (currentConversation == null)
            {
                yield break;
            }

            if (currentNodeIndex < 0 || currentNodeIndex >= currentConversation.Nodes.Count)
            {
                yield return EndRoutine();
                yield break;
            }

            ConversationNode node = currentConversation.Nodes[currentNodeIndex];

            if (node == null || node.IsEndNode)
            {
                yield return EndRoutine();
                yield break;
            }

            if (node.IsNPC)
            {
                yield return PlayNPCNode(node);
            }
            else
            {
                yield return PlayPlayerNode(node);
                yield break;
            }
        }
    }

    private IEnumerator PlayNPCNode(ConversationNode node)
    {
        isTypingNPC = true;

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(true);
            ScrollToBottom();
        }

        if (bubbleDelay > 0f)
        {
            yield return new WaitForSeconds(bubbleDelay);
        }

        if (typingIndicator != null)
        {
            typingIndicator.SetActive(false);
        }

        ChatBubble bubble = SpawnBubble(isNPC: true);

        float estimatedTime = 0f;

        if (bubble != null)
        {
            bubble.StartTyping(node.Message, isNPC: true, charInterval);
            ScrollToBottom();

            estimatedTime = Mathf.Max(0.05f, node.Message.Length * charInterval);
        }

        yield return new WaitForSeconds(estimatedTime);

        isTypingNPC = false;

        if (node.AutoAdvance)
        {
            yield return new WaitForSeconds(node.AutoAdvanceDelay);
            AdvanceToNextNode(currentNodeIndex + 1);
            yield break;
        }

        int nextPlayerIndex = FindNextPlayerNodeIndex(currentNodeIndex + 1);

        if (nextPlayerIndex >= 0)
        {
            currentNodeIndex = nextPlayerIndex;
            yield return PlayPlayerNode(currentConversation.Nodes[nextPlayerIndex]);
        }
        else
        {
            currentNodeIndex = currentNodeIndex + 1;
        }
    }

    private IEnumerator PlayPlayerNode(ConversationNode node)
    {
        ClearOptions();

        if (!string.IsNullOrEmpty(node.Message))
        {
            ChatBubble bubble = SpawnBubble(isNPC: false);

            if (bubble != null)
            {
                bubble.StartTyping(node.Message, isNPC: false, charInterval);
                ScrollToBottom();
            }
        }

        if (node.PlayerOptions != null && node.PlayerOptions.Count > 0)
        {
            yield return new WaitForSeconds(optionDelay);

            isWaitingForChoice = true;
            CreateOptionButtons(node.PlayerOptions);
            yield break;
        }

        AdvanceToNextNode(currentNodeIndex + 1);
        yield break;
    }

    private IEnumerator EndRoutine()
    {
        ClearOptions();

        if (optionsContainer != null && optionButtonPrefab != null)
        {
            GameObject endBtn = Instantiate(optionButtonPrefab, optionsContainer);
            Button button = endBtn.GetComponent<Button>();
            TMP_Text label = endBtn.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = "End Conversation";
            }

            if (button != null)
            {
                button.onClick.AddListener(CloseConversation);
            }

            ScrollToBottom();
        }

        yield break;
    }

    private void CreateOptionButtons(List<PlayerOption> options)
    {
        if (optionsContainer == null || optionButtonPrefab == null)
        {
            Debug.LogWarning("[CHAT] Options container or prefab missing.");
            return;
        }

        currentOptionButtons.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            PlayerOption option = options[i];

            GameObject obj = Instantiate(optionButtonPrefab, optionsContainer);
            Button button = obj.GetComponent<Button>();
            TMP_Text label = obj.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = option.OptionText;
            }

            if (button != null)
            {
                PlayerOption captured = option;
                button.onClick.AddListener(() => OnOptionSelected(captured));
                currentOptionButtons.Add(button);
            }
        }

        if (lockOptionsUntilTypingDone && isTypingNPC)
        {
            SetOptionsInteractable(false);
            StartCoroutine(EnableOptionsWhenTypingDone());
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

    private void SetOptionsInteractable(bool value)
    {
        for (int i = 0; i < currentOptionButtons.Count; i++)
        {
            if (currentOptionButtons[i] != null)
            {
                currentOptionButtons[i].interactable = value;
            }
        }
    }

    private void OnOptionSelected(PlayerOption option)
    {
        if (!isWaitingForChoice)
        {
            return;
        }

        if (lockOptionsUntilTypingDone && isTypingNPC)
        {
            return;
        }

        OnOptionChosen?.Invoke(option);

        isWaitingForChoice = false;

        if (disableOptionOnClick)
        {
            SetOptionsInteractable(false);
        }

        if (echoOptionAsPlayerBubble && !string.IsNullOrEmpty(option.OptionText))
        {
            ChatBubble echo = SpawnBubble(isNPC: false);

            if (echo != null)
            {
                echo.SetMessage(option.OptionText, isNPC: false);
                ScrollToBottom();
            }
        }

        ClearOptions();

        if (currentConversation == null)
        {
            return;
        }

        if (option.NextNodeIndex < 0 || option.NextNodeIndex >= currentConversation.Nodes.Count)
        {
            Debug.LogWarning($"[CHAT] Option '{option.OptionText}' has invalid NextNodeIndex ({option.NextNodeIndex}).");

            if (closeIfOptionTargetInvalid)
            {
                CloseConversation();
            }

            return;
        }

        AdvanceToNextNode(option.NextNodeIndex);
    }

    private void AdvanceToNextNode(int nextIndex)
    {
        if (currentConversation == null)
        {
            return;
        }

        currentNodeIndex = nextIndex;

        if (flowRoutine != null)
        {
            StopCoroutine(flowRoutine);
        }

        flowRoutine = StartCoroutine(FlowRoutine());
    }

    private int FindNextPlayerNodeIndex(int startIndex)
    {
        if (currentConversation == null)
        {
            return -1;
        }

        for (int i = startIndex; i < currentConversation.Nodes.Count; i++)
        {
            ConversationNode candidate = currentConversation.Nodes[i];

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

    private ChatBubble SpawnBubble(bool isNPC)
    {
        ChatBubble prefab = isNPC ? npcBubblePrefab : playerBubblePrefab;

        if (prefab == null || messageContainer == null)
        {
            Debug.LogWarning("[CHAT] Bubble prefab or container missing.");
            return null;
        }

        ChatBubble bubble = Instantiate(prefab, messageContainer);
        spawnedBubbles.Add(bubble);
        return bubble;
    }

    private void ClearChat()
    {
        for (int i = 0; i < spawnedBubbles.Count; i++)
        {
            if (spawnedBubbles[i] != null)
            {
                Destroy(spawnedBubbles[i].gameObject);
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
            Destroy(optionsContainer.GetChild(i).gameObject);
        }
    }

    private void StopAllTyping()
    {
        for (int i = 0; i < spawnedBubbles.Count; i++)
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
        scrollRect.verticalNormalizedPosition = 0f;
    }
}