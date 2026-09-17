using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "NewConversation",
    menuName = "Conversation/Conversation Data"
)]
public class ConversationData : ScriptableObject
{
    [Header("Conversation Info")]
    public string ConversationTitle;

    [Header("NPC Info")]
    public string NPCSpeakerName;
    public Sprite NPCSpeakerAvatar;

    [Tooltip("Background image shown behind/around the NPC avatar in the contact list.")]
    public Sprite NPCSpeakerBackground;

    [Header("Conversation Flow")]
    public List<ConversationNode> Nodes = new List<ConversationNode>();

    [Header("Settings")]
    public int StartNodeIndex = 0;
}

[System.Serializable]
public class ConversationNode
{
    [Header("Speaker")]
    public bool IsNPC = true;

    [Header("Message")]
    [TextArea(2, 4)]
    public string Message;

    [Header("Display")]
    public BlockType DisplayType = BlockType.Paragraph;
    public Sprite Image;

    [Header("Link")]
    public ArticleData LinkedArticle;
    public string LinkURL;

    [Header("Player Options")]
    public List<PlayerOption> PlayerOptions = new List<PlayerOption>();

    [Header("Node Settings")]
    public bool IsEndNode;
    public bool AutoAdvance = false;
    public float AutoAdvanceDelay = 2.0f;
}

[System.Serializable]
public class PlayerOption
{
    [Header("Option")]
    public string OptionText;

    [Header("Action")]
    public int NextNodeIndex = -1;
}