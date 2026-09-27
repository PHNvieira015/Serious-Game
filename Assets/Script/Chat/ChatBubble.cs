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

    [Header("Typing Audio")]
    [Tooltip("Use a dedicated AudioSource on this bubble prefab.")]
    public AudioSource typingAudioSource;

    public AudioClip typingAudioClip;

    [Range(0f, 1f)]
    public float typingAudioVolume = 0.5f;

    [Tooltip("When enabled, only NPC bubbles play typing audio.")]
    public bool typingAudioNPCOnly = true;

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
    private bool isTyping;

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
                bubbleButton.targetGraphic = bubbleBackground;
            }
        }

        if (typingAudioSource != null)
        {
            typingAudioSource.playOnAwake = false;
            typingAudioSource.loop = true;
            typingAudioSource.spatialBlend = 0f;
            typingAudioSource.Stop();
        }
    }

    public void SetMessage(string text, bool isNPC)
    {
        isNPCBubble = isNPC;

        if (bubbleBackground != null)
        {
            bubbleBackground.color = isNPC
                ? npcBubbleColor
                : playerBubbleColor;
        }

        if (messageText != null)
        {
            messageText.color = isLinkBubble
                ? linkTextColor
                : isNPC
                    ? npcTextColor
                    : playerTextColor;

            messageText.text = text;
            messageText.ForceMeshUpdate();
        }

        if (bubbleRect != null)
        {
            Vector2 alignment = isNPC
                ? new Vector2(0f, 0f)
                : new Vector2(1f, 0f);

            bubbleRect.anchorMin = alignment;
            bubbleRect.anchorMax = alignment;
            bubbleRect.pivot = alignment;

            Vector2 position = bubbleRect.anchoredPosition;

            position.x = isNPC
                ? sidePadding
                : -sidePadding;

            bubbleRect.anchoredPosition = position;
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
            messageText.fontStyle |= FontStyles.Underline;
        }

        if (bubbleButton != null)
        {
            bubbleButton.interactable = !isTyping;
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
            messageText.fontStyle &= ~FontStyles.Underline;

            messageText.color = isNPCBubble
                ? npcTextColor
                : playerTextColor;
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
        StopTyping();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        if (!isActiveAndEnabled)
        {
            SetMessage(fullText ?? string.Empty, isNPC);
            return;
        }

        if (messageText == null ||
            string.IsNullOrEmpty(fullText))
        {
            SetMessage(fullText ?? string.Empty, isNPC);
            return;
        }

        isTyping = true;

        if (bubbleButton != null)
        {
            bubbleButton.interactable = false;
        }

        typingRoutine = StartCoroutine(
            TypeRoutine(fullText, isNPC, charInterval)
        );
    }

    public void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        isTyping = false;
        StopTypingAudio();

        if (bubbleButton != null)
        {
            bubbleButton.interactable = isLinkBubble;
        }
    }

    private IEnumerator TypeRoutine(
        string fullText,
        bool isNPC,
        float charInterval)
    {
        SetMessage(string.Empty, isNPC);

        if (!string.IsNullOrWhiteSpace(fullText))
        {
            StartTypingAudio(isNPC);
        }

        for (int i = 1; i <= fullText.Length; i++)
        {
            messageText.text = fullText.Substring(0, i);
            messageText.ForceMeshUpdate();
            UpdateLayoutElement();

            if (i < fullText.Length)
            {
                if (charInterval > 0f)
                {
                    yield return new WaitForSeconds(charInterval);
                }
                else
                {
                    yield return null;
                }
            }
        }

        StopTypingAudio();
        isTyping = false;

        if (bubbleButton != null)
        {
            bubbleButton.interactable = isLinkBubble;
        }

        // Ensure even a one-character message yields before
        // clearing the coroutine reference.
        yield return null;
        typingRoutine = null;
    }

    private void StartTypingAudio(bool isNPC)
    {
        if (typingAudioSource == null ||
            typingAudioClip == null)
        {
            return;
        }

        if (typingAudioNPCOnly && !isNPC)
        {
            return;
        }

        if (!typingAudioSource.isActiveAndEnabled)
        {
            return;
        }

        typingAudioSource.Stop();
        typingAudioSource.clip = typingAudioClip;
        typingAudioSource.volume = typingAudioVolume;
        typingAudioSource.loop = true;
        typingAudioSource.spatialBlend = 0f;
        typingAudioSource.Play();
    }

    private void StopTypingAudio()
    {
        if (typingAudioSource != null)
        {
            typingAudioSource.Stop();
        }
    }

    private void HandleBubbleClicked()
    {
        if (!isLinkBubble || isTyping)
        {
            return;
        }

        if (linkCallback == null)
        {
            Debug.LogWarning(
                "[CHAT] Link bubble has no callback.",
                this
            );

            return;
        }

        linkCallback.Invoke(linkedArticle, linkedURL);
    }

    private void UpdateLayoutElement()
    {
        if (layoutElement == null || messageText == null)
        {
            return;
        }

        float preferredHeight =
            messageText.preferredHeight + verticalPadding;

        float preferredWidth = Mathf.Min(
            messageText.preferredWidth + verticalPadding,
            maxBubbleWidth
        );

        layoutElement.preferredHeight = Mathf.Max(
            minBubbleHeight,
            preferredHeight
        );

        layoutElement.preferredWidth = preferredWidth;
    }

    private void OnDisable()
    {
        StopTyping();
    }

    private void OnDestroy()
    {
        StopTypingAudio();

        if (bubbleButton != null)
        {
            bubbleButton.onClick.RemoveListener(
                HandleBubbleClicked
            );
        }
    }
}