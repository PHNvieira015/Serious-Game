using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubble : MonoBehaviour
{
    [Header("References")]
    public RectTransform bubbleRect;
    public Image bubbleBackground;
    public TMP_Text messageText;
    public LayoutElement layoutElement;

    [Header("Colors")]
    public Color npcBubbleColor = new Color(1f, 1f, 1f, 1f);
    public Color playerBubbleColor = new Color(0.85f, 0.97f, 0.79f, 1f);
    public Color npcTextColor = Color.black;
    public Color playerTextColor = Color.black;

    [Header("Layout")]
    public float sidePadding = 40f;
    public float maxBubbleWidth = 700f;
    public float minBubbleHeight = 60f;
    public float verticalPadding = 40f;

    private Coroutine typingRoutine;

    public void SetMessage(string text, bool isNPC)
    {
        if (bubbleBackground != null)
        {
            bubbleBackground.color = isNPC ? npcBubbleColor : playerBubbleColor;
        }

        if (messageText != null)
        {
            messageText.color = isNPC ? npcTextColor : playerTextColor;
            messageText.text = text;
            messageText.ForceMeshUpdate();
        }

        if (bubbleRect != null)
        {
            bubbleRect.anchorMin = new Vector2(isNPC ? 0f : 1f, 0f);
            bubbleRect.anchorMax = new Vector2(isNPC ? 0f : 1f, 0f);
            bubbleRect.pivot = new Vector2(isNPC ? 0f : 1f, 0f);

            Vector2 pos = bubbleRect.anchoredPosition;
            pos.x = isNPC ? sidePadding : -sidePadding;
            bubbleRect.anchoredPosition = pos;
        }

        UpdateLayoutElement();
    }

    public void StartTyping(string fullText, bool isNPC, float charInterval)
    {
        // A MonoBehaviour can't start a coroutine while its GameObject is inactive.
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        StopTyping();
        typingRoutine = StartCoroutine(TypeRoutine(fullText, isNPC, charInterval));
    }

    public void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }
    }

    private IEnumerator TypeRoutine(string fullText, bool isNPC, float charInterval)
    {
        SetMessage(string.Empty, isNPC);

        if (messageText == null)
        {
            yield break;
        }

        for (int i = 0; i <= fullText.Length; i++)
        {
            messageText.text = fullText.Substring(0, i);
            messageText.ForceMeshUpdate();
            UpdateLayoutElement();

            if (charInterval > 0f)
            {
                yield return new WaitForSeconds(charInterval);
            }
            else
            {
                yield return null;
            }
        }

        typingRoutine = null;
    }

    private void UpdateLayoutElement()
    {
        if (layoutElement == null || messageText == null)
        {
            return;
        }

        float preferredHeight = messageText.preferredHeight + verticalPadding;
        float preferredWidth = Mathf.Min(messageText.preferredWidth + verticalPadding, maxBubbleWidth);

        layoutElement.preferredHeight = Mathf.Max(minBubbleHeight, preferredHeight);
        layoutElement.preferredWidth = preferredWidth;
    }
}