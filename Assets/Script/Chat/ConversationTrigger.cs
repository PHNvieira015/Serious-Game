using UnityEngine;

public class ConversationTrigger : MonoBehaviour
{
    [Header("Conversation Data")]
    public ConversationData conversationToStart;

    [Header("Settings")]
    public KeyCode interactKey = KeyCode.E;

    private ConversationViewer conversationViewer;
    private bool playerInRange;

    private void Start()
    {
        conversationViewer = Object.FindFirstObjectByType<ConversationViewer>();
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            StartConversation();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    public void StartConversation()
    {
        if (conversationViewer != null && conversationToStart != null)
        {
            conversationViewer.StartConversation(conversationToStart);
        }
    }
}