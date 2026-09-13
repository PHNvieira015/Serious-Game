using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConversationButton : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The conversation this button opens when clicked.")]
    public ConversationData conversation;

    [Header("UI References")]
    public Button button;
    public TMP_Text label;
    public Image avatarImage;

    [Header("Selection Colors")]
    public Color selectedColor = new Color(0.85f, 0.95f, 1f, 1f);

    public ConversationData Conversation => conversation;

    private System.Action<ConversationButton> onClicked;
    private Image buttonImage;
    private Color normalColor;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (button != null)
        {
            buttonImage = button.targetGraphic as Image;

            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        if (buttonImage != null)
        {
            normalColor = buttonImage.color;
        }
    }

    public void Setup(System.Action<ConversationButton> callback)
    {
        onClicked = callback;

        if (label != null)
        {
            label.text = conversation != null ? conversation.NPCSpeakerName : string.Empty;
        }

        if (avatarImage != null)
        {
            avatarImage.sprite = conversation != null ? conversation.NPCSpeakerAvatar : null;
            avatarImage.enabled = avatarImage.sprite != null;
        }

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (buttonImage == null)
        {
            return;
        }

        buttonImage.color = selected ? selectedColor : normalColor;
    }

    private void HandleClick()
    {
        onClicked?.Invoke(this);
    }
}