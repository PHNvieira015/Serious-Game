using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ConversationListUI : MonoBehaviour
{
    [Header("Viewer")]
    public ConversationViewer viewer;

    [Header("Contacts")]
    [Tooltip("Contact buttons already placed in the scene. Each one holds its own ConversationData.")]
    public List<ConversationButton> contactButtons = new List<ConversationButton>();

    [Header("Header")]
    [Tooltip("Label that shows the currently open conversation's name.")]
    public TMP_Text conversationTitleText;

    [Tooltip("Fallback text when no conversation is open.")]
    public string emptyTitle = "";

    private ConversationButton activeButton;

    private void Awake()
    {
        if (viewer == null)
        {
            viewer = FindFirstObjectByType<ConversationViewer>();
        }

        if (viewer == null)
        {
            Debug.LogError("[CHAT] ConversationListUI: no ConversationViewer found in the scene.");
        }
    }

    private void Start()
    {
        for (int i = 0; i < contactButtons.Count; i++)
        {
            ConversationButton btn = contactButtons[i];

            if (btn == null)
            {
                continue;
            }

            btn.Setup(OnContactClicked);
        }

        if (viewer != null)
        {
            viewer.OnConversationOpened += HandleConversationOpened;
            viewer.OnConversationSwitched += HandleConversationOpened;
            viewer.OnConversationClosed += HandleConversationClosed;
        }

        UpdateTitle(null);
    }

    private void OnDestroy()
    {
        if (viewer != null)
        {
            viewer.OnConversationOpened -= HandleConversationOpened;
            viewer.OnConversationSwitched -= HandleConversationOpened;
            viewer.OnConversationClosed -= HandleConversationClosed;
        }
    }

    private void OnContactClicked(ConversationButton clicked)
    {
        if (clicked == null || clicked.Conversation == null)
        {
            return;
        }

        if (viewer == null)
        {
            Debug.LogWarning("[CHAT] ConversationListUI has no viewer assigned.");
            return;
        }

        // Clicking the active contact closes the chat.
        if (activeButton == clicked && viewer.currentConversation == clicked.Conversation)
        {
            viewer.CloseConversation();
            return;
        }

        viewer.OpenConversation(clicked.Conversation);
        SetActiveButton(clicked);
    }

    private void SetActiveButton(ConversationButton btn)
    {
        if (activeButton != null)
        {
            activeButton.SetSelected(false);
        }

        activeButton = btn;

        if (activeButton != null)
        {
            activeButton.SetSelected(true);
        }
    }

    private void HandleConversationOpened(ConversationData data)
    {
        UpdateTitle(data);
        HighlightButtonFor(data);
    }

    private void HandleConversationClosed(ConversationData data)
    {
        UpdateTitle(null);
        SetActiveButton(null);
    }

    private void UpdateTitle(ConversationData data)
    {
        if (conversationTitleText == null)
        {
            return;
        }

        conversationTitleText.text = data != null ? data.NPCSpeakerName : emptyTitle;
    }

    private void HighlightButtonFor(ConversationData data)
    {
        for (int i = 0; i < contactButtons.Count; i++)
        {
            ConversationButton btn = contactButtons[i];

            if (btn == null)
            {
                continue;
            }

            bool match = btn.Conversation == data;
            btn.SetSelected(match);

            if (match)
            {
                activeButton = btn;
            }
        }
    }
}