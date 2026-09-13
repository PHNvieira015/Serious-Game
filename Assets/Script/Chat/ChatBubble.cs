using System;
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
    public Button bubbleButton;

    [Header("Colors")]
    public Color npcBubbleColor =
        new Color(1f, 1f, 1f, 1f);

    public Color playerBubbleColor =
        new Color(0.85f, 0.97f, 0.79f, 1f);

    public Color npcTextColor = Color.black;
    public Color playerTextColor = Color.black;

    public Color linkTextColor =
        new Color(0.15f, 0.5f, 0.94f, 1f);

    [Header("Layout")]
    public float sidePadding = 40f;
    public float maxBubbleWidth = 700f;
    public float minBubbleHeight = 60f;
    public float verticalPadding = 40f;

    private Coroutine typingRoutine;

    private ArticleData linkedArticle;
    private string linkedURL;
    private Action<ArticleData, string> linkCallback;

    private bool isLinkBubble;
    private bool isNPCBubble;

    private void Awake()
    {
        if (bubbleButton == null)
        {
            bubbleButton = GetComponent<Button>();
        }

        if (bubbleButton != null)
        {
            bubbleButton.onClick.AddListener(
                HandleBubbleClicked
            );

            bubbleButton.interactable = false;

            if (bubbleButton.targetGraphic == null &&
                bubbleBackground != null)
            {
                bubbleButton.targetGraphic =
                    bubbleBackground;
            }
        }
    }

    public void SetMessage(string text, bool isNPC)
    {
        isNPCBubble = isNPC;

        if (bubbleBackground != null)
        {
            if (isNPC)
            {
                bubbleBackground.color =
                    npcBubbleColor;
            }
            else
            {
                bubbleBackground.color =
                    playerBubbleColor;
            }
        }

        if (messageText != null)
        {
            if (isLinkBubble)
            {
                messageText.color = linkTextColor;
            }
            else if (isNPC)
            {
                messageText.color = npcTextColor;
            }
            else
            {
                messageText.color = playerTextColor;
            }

            messageText.text = text;
            messageText.ForceMeshUpdate();
        }

        if (bubbleRect != null)
        {
            if (isNPC)
            {
                bubbleRect.anchorMin =
                    new Vector2(0f, 0f);

                bubbleRect.anchorMax =
                    new Vector2(0f, 0f);

                bubbleRect.pivot =
                    new Vector2(0f, 0f);
            }
            else
            {
                bubbleRect.anchorMin =
                    new Vector2(1f, 0f);

                bubbleRect.anchorMax =
                    new Vector2(1f, 0f);

                bubbleRect.pivot =
                    new Vector2(1f, 0f);
            }

            Vector2 position =
                bubbleRect.anchoredPosition;

            if (isNPC)
            {
                position.x = sidePadding;
            }
            else
            {
                position.x = -sidePadding;
            }

            bubbleRect.anchoredPosition =
                position;
        }

        UpdateLayoutElement();
    }

    public void ConfigureAsLink(
        ArticleData article,
        string url,
        Action<ArticleData, string> callback)
    {
        linkedArticle = article;
        linkedURL = url;
        linkCallback = callback;
        isLinkBubble = true;

        if (messageText != null)
        {
            messageText.color = linkTextColor;

            messageText.fontStyle =
                messageText.fontStyle |
                FontStyles.Underline;
        }

        if (bubbleButton != null)
        {
            if (typingRoutine == null)
            {
                bubbleButton.interactable = true;
            }
            else
            {
                bubbleButton.interactable = false;
            }
        }
    }

    public void ConfigureAsNormalBubble()
    {
        linkedArticle = null;
        linkedURL = string.Empty;
        linkCallback = null;
        isLinkBubble = false;

        if (messageText != null)
        {
            messageText.fontStyle =
                messageText.fontStyle &
                ~FontStyles.Underline;

            if (isNPCBubble)
            {
                messageText.color = npcTextColor;
            }
            else
            {
                messageText.color = playerTextColor;
            }
        }

        if (bubbleButton != null)
        {
            bubbleButton.interactable = false;
        }
    }

    public void StartTyping(
        string fullText,
        bool isNPC,
        float charInterval)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }

        StopTyping();

        typingRoutine = StartCoroutine(
            TypeRoutine(
                fullText,
                isNPC,
                charInterval
            )
        );
    }

    public void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        if (bubbleButton != null)
        {
            bubbleButton.interactable =
                isLinkBubble;
        }
    }

    private IEnumerator TypeRoutine(
        string fullText,
        bool isNPC,
        float charInterval)
    {
        if (fullText == null)
        {
            fullText = string.Empty;
        }

        SetMessage(string.Empty, isNPC);

        if (messageText == null)
        {
            typingRoutine = null;
            yield break;
        }

        for (int i = 0; i <= fullText.Length; i++)
        {
            messageText.text =
                fullText.Substring(0, i);

            messageText.ForceMeshUpdate();

            UpdateLayoutElement();

            if (charInterval > 0f)
            {
                yield return new WaitForSeconds(
                    charInterval
                );
            }
            else
            {
                yield return null;
            }
        }

        messageText.text = fullText;
        messageText.ForceMeshUpdate();

        UpdateLayoutElement();

        typingRoutine = null;

        if (bubbleButton != null)
        {
            bubbleButton.interactable =
                isLinkBubble;
        }
    }

    private void HandleBubbleClicked()
    {
        if (!isLinkBubble)
        {
            return;
        }

        if (linkCallback == null)
        {
            Debug.LogWarning(
                "[CHAT] Link bubble has no callback."
            );

            return;
        }

        linkCallback.Invoke(
            linkedArticle,
            linkedURL
        );
    }

    private void UpdateLayoutElement()
    {
        if (layoutElement == null ||
            messageText == null)
        {
            return;
        }

        float preferredHeight =
            messageText.preferredHeight +
            verticalPadding;

        float preferredWidth =
            Mathf.Min(
                messageText.preferredWidth +
                verticalPadding,
                maxBubbleWidth
            );

        layoutElement.preferredHeight =
            Mathf.Max(
                minBubbleHeight,
                preferredHeight
            );

        layoutElement.preferredWidth =
            preferredWidth;
    }

    private void OnDestroy()
    {
        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(
                HandleBubbleClicked
            );
        }
    }
}