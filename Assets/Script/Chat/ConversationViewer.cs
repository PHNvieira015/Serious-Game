using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ConversationViewer : MonoBehaviour
{
    [Header("Conversation Data")]
    public ConversationData currentConversation;

    [Header("UI References")]
    public GameObject conversationPanel;
    public Transform messageContainer;

    [Header("Prefabs")]
    public GameObject npcMessagePrefab;
    public GameObject playerMessagePrefab;
    public GameObject optionButtonPrefab;

    [Header("Option Panel")]
    public Transform optionsContainer;

    [Header("NPC Info Display")]
    public TMP_Text npcNameText;
    public Image npcAvatarImage;

    [Header("Settings")]
    public float typingSpeed = 0.05f;

    private int currentNodeIndex = 0;
    private bool isTyping = false;
    private List<GameObject> instantiatedMessages = new List<GameObject>();

    private void Start()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }
    }

    public void StartConversation(ConversationData conversation)
    {
        if (conversation == null)
        {
            Debug.LogError("ConversationViewer: ConversationData is null!");
            return;
        }

        if (conversation.Nodes == null || conversation.Nodes.Count == 0)
        {
            Debug.LogError("ConversationViewer: No conversation nodes found!");
            return;
        }

        currentConversation = conversation;

        if (conversationPanel != null)
        {
            conversationPanel.SetActive(true);
        }

        currentNodeIndex = currentConversation.StartNodeIndex;

        if (npcNameText != null)
        {
            npcNameText.text = currentConversation.NPCSpeakerName;
        }

        if (npcAvatarImage != null && currentConversation.NPCSpeakerAvatar != null)
        {
            npcAvatarImage.sprite = currentConversation.NPCSpeakerAvatar;
        }

        ClearMessages();

        ShowNode(currentNodeIndex);
    }

    private void ShowNode(int nodeIndex)
    {
        if (currentConversation == null)
        {
            Debug.LogError("ConversationViewer: No current conversation!");
            return;
        }

        if (nodeIndex < 0 || nodeIndex >= currentConversation.Nodes.Count)
        {
            EndConversation();
            return;
        }

        ConversationNode node = currentConversation.Nodes[nodeIndex];

        if (node.IsEndNode)
        {
            EndConversation();
            return;
        }

        if (node.IsNPC)
        {
            ShowNPCMessage(node, nodeIndex);
        }
        else
        {
            ShowPlayerNode(node, nodeIndex);
        }
    }

    private void ShowNPCMessage(ConversationNode node, int nodeIndex)
    {
        GameObject messageObj = Instantiate(npcMessagePrefab, messageContainer);
        instantiatedMessages.Add(messageObj);

        ArticleBlockView blockView = messageObj.GetComponent<ArticleBlockView>();

        if (blockView != null)
        {
            ArticleBlock block = new ArticleBlock();

            block.Type = node.DisplayType;
            block.Text = node.Message;
            block.Image = node.Image;

            blockView.Initialize(block);
        }

        if (node.AutoAdvance)
        {
            StartCoroutine(AutoAdvanceNext(node.AutoAdvanceDelay));
        }
        else
        {
            int nextIndex = nodeIndex + 1;

            while (nextIndex < currentConversation.Nodes.Count)
            {
                if (!currentConversation.Nodes[nextIndex].IsNPC)
                {
                    StartCoroutine(DelayedShowOptions(nextIndex));
                    return;
                }

                nextIndex++;
            }

            EndConversation();
        }
    }

    private IEnumerator AutoAdvanceNext(float delay)
    {
        yield return new WaitForSeconds(delay);

        GoToNextNode(currentNodeIndex + 1);
    }

    private IEnumerator DelayedShowOptions(int playerNodeIndex)
    {
        yield return new WaitForSeconds(0.5f);

        ShowNode(playerNodeIndex);
    }

    private void ShowPlayerNode(ConversationNode node, int nodeIndex)
    {
        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject messageObj = Instantiate(playerMessagePrefab, messageContainer);
        instantiatedMessages.Add(messageObj);

        ArticleBlockView blockView = messageObj.GetComponent<ArticleBlockView>();

        if (blockView != null)
        {
            ArticleBlock block = new ArticleBlock();

            block.Type = node.DisplayType;
            block.Text = node.Message;
            block.Image = node.Image;

            blockView.Initialize(block);
        }

        if (node.PlayerOptions != null && node.PlayerOptions.Count > 0)
        {
            CreateOptionButtons(node.PlayerOptions);
        }
        else
        {
            GoToNextNode(nodeIndex + 1);
        }
    }

    private void CreateOptionButtons(List<PlayerOption> options)
    {
        foreach (PlayerOption option in options)
        {
            GameObject optionObj = Instantiate(optionButtonPrefab, optionsContainer);

            Button button = optionObj.GetComponent<Button>();
            TMP_Text buttonText = optionObj.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = option.OptionText;
            }

            if (button != null)
            {
                int nextIndex = option.NextNodeIndex;

                button.onClick.AddListener(() =>
                {
                    OnPlayerOptionSelected(nextIndex);
                });
            }
        }
    }

    private void OnPlayerOptionSelected(int nextNodeIndex)
    {
        if (isTyping)
        {
            return;
        }

        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        if (nextNodeIndex < 0 || nextNodeIndex >= currentConversation.Nodes.Count)
        {
            EndConversation();
            return;
        }

        currentNodeIndex = nextNodeIndex;

        ShowNode(currentNodeIndex);
    }

    private void GoToNextNode(int nextIndex)
    {
        if (nextIndex < 0 || nextIndex >= currentConversation.Nodes.Count)
        {
            EndConversation();
            return;
        }

        currentNodeIndex = nextIndex;

        ShowNode(currentNodeIndex);
    }

    private void ClearMessages()
    {
        foreach (GameObject msg in instantiatedMessages)
        {
            if (msg != null)
            {
                Destroy(msg);
            }
        }

        instantiatedMessages.Clear();

        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void EndConversation()
    {
        if (optionsContainer == null)
        {
            return;
        }

        foreach (Transform child in optionsContainer)
        {
            Destroy(child.gameObject);
        }

        GameObject endBtn = Instantiate(optionButtonPrefab, optionsContainer);

        Button button = endBtn.GetComponent<Button>();
        TMP_Text buttonText = endBtn.GetComponentInChildren<TMP_Text>();

        if (buttonText != null)
        {
            buttonText.text = "End Conversation";
        }

        if (button != null)
        {
            button.onClick.AddListener(CloseConversation);
        }
    }

    public void CloseConversation()
    {
        if (conversationPanel != null)
        {
            conversationPanel.SetActive(false);
        }

        ClearMessages();

        Debug.Log("Conversation ended!");
    }
}