using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArticleFeedbackPanel : MonoBehaviour
{
    [Serializable]
    public class TypeIcon
    {
        public CheckType type;
        public Sprite icon;
    }

    [Header("Feedback Panel")]
    public GameObject feedbackPanel;

    [Header("UI References")]
    public TMP_Text blockText;
    public TMP_Text blockIndexText;
    public TMP_Text resultText;
    public TMP_Text expectedAnswerText;
    public TMP_Text playerMarkText;
    public TMP_Text explanationText;

    [Header("Type Labels")]
    [Tooltip("Displays the correct answer.")]
    public TMP_Text blockTypeText;

    [Tooltip("Displays the player's answer.")]
    public TMP_Text markedTypeText;

    [Header("Answer Prefixes")]
    public string correctAnswerPrefix = "Checagem Correta: ";
    public string playerAnswerPrefix = "Sua marcacao: ";

    [Header("Result Text")]
    public string correctResultText = "Correto";
    public string wrongResultText = "Errado";
    public string missedResultText = "Errado";

    [Header("Result Image")]
    public Image feedbackResultImage;
    public Sprite correctResultSprite;
    public Sprite missedResultSprite;

    [Header("Result Background")]
    public Image feedbackResultBackground;
    public Sprite correctBackgroundSprite;
    public Sprite wrongBackgroundSprite;

    [Header("Answer Icons")]
    public Image blockTypeIcon;

        public Image markedTypeIcon;

    public List<TypeIcon> typeIcons = new List<TypeIcon>();

    [Header("Progress")]
    public Image progressFillImage;
    
    public Slider progressBar;
    public TMP_Text progressText;

    [Header("Block Navigation")]
    public RectTransform blockButtonsContainer;
    public Button blockButtonPrefab;
    public Color normalBlockButtonColor = Color.white;
    public Color selectedBlockButtonColor =
        new Color(0.3f, 0.7f, 1f, 1f);

    [Header("Navigation")]
    public Button backButton;
    public Button nextButton;
    public Button closeButton;

    [Header("Result Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color missedColor = Color.red;

    [Header("Settings")]
    [Min(0f)]
    public float displayDelay = 1.5f;

    private readonly List<BlockResult> results =
        new List<BlockResult>();

    private readonly List<Button> spawnedBlockButtons =
        new List<Button>();

    private Action onFeedbackComplete;
    private Coroutine enableNextCoroutine;
    private int currentIndex;
    private int lastNavigationFrame = -1;
    private bool initialized;
    private bool isBlockDisplayed;
    private bool canGoNext;

    private void Awake()
    {
        InitializeControls();
    }

    private void InitializeControls()
    {
        if (initialized)
            return;

        initialized = true;

        if (backButton != null)
            backButton.onClick.AddListener(ShowPreviousBlock);

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNextBlock);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseFeedback);

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.wholeNumbers = false;
            progressBar.interactable = false;
        }
    }

    public void StartFeedback(
        ValidationResult result,
        Action onComplete = null)
    {
        InitializeControls();
        StopNextDelay();
        ClearBlockButtons();
        results.Clear();

        if (result != null && result.BlockResults != null)
        {
            foreach (BlockResult item in result.BlockResults)
            {
                if (item != null)
                    results.Add(item);
            }
        }

        onFeedbackComplete = onComplete;
        currentIndex = 0;
        lastNavigationFrame = -1;
        isBlockDisplayed = false;
        canGoNext = false;

        if (results.Count == 0)
        {
            CloseFeedback();
            return;
        }

        if (feedbackPanel != null)
            feedbackPanel.SetActive(true);

        CreateBlockButtons();
        ShowBlock(0);
    }

    private void ShowBlock(int index)
    {
        if (index < 0 || index >= results.Count)
            return;

        StopNextDelay();

        currentIndex = index;
        isBlockDisplayed = true;
        canGoNext = false;

        BlockResult result = results[index];

        if (blockText != null)
            blockText.text = result.BlockText;

        if (blockIndexText != null)
        {
            blockIndexText.text =
                "Checagem " + (index + 1) + " de " + results.Count;
        }

        DisplayResult(result);
        DisplayAnswers(result);

        if (explanationText != null)
        {
            string explanation = null;

            if (result.Block != null &&
                result.Block.ArticleBlock != null)
            {
                explanation = result.Block.ArticleBlock.Explanation;
            }

            explanationText.text = string.IsNullOrEmpty(explanation)
                ? "No explanation provided."
                : explanation;
        }

        UpdateProgress();
        UpdateBlockButtons();

        if (backButton != null)
        {
            backButton.interactable = true;
            SetButtonText(backButton, index == 0 ? "Return" : "Back");
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;
            SetButtonText(
                nextButton,
                index == results.Count - 1 ? "Finish" : "Next"
            );
        }

        if (displayDelay <= 0f)
        {
            EnableNextButton();
        }
        else if (isActiveAndEnabled)
        {
            enableNextCoroutine =
                StartCoroutine(EnableNextButtonAfterDelay());
        }
    }

    private void DisplayResult(BlockResult result)
    {
        bool correct =
            result.ResultType == BlockResultType.Correct;

        if (resultText != null)
        {
            switch (result.ResultType)
            {
                case BlockResultType.Correct:
                    resultText.text = correctResultText;
                    resultText.color = correctColor;
                    break;

                case BlockResultType.Missed:
                    resultText.text = missedResultText;
                    resultText.color = missedColor;
                    break;

                default:
                    resultText.text = wrongResultText;
                    resultText.color = wrongColor;
                    break;
            }
        }

        SetImage(
            feedbackResultImage,
            correct ? correctResultSprite : missedResultSprite
        );

        SetImage(
            feedbackResultBackground,
            correct ? correctBackgroundSprite : wrongBackgroundSprite
        );
    }

    private void DisplayAnswers(BlockResult result)
    {
        // Use the values captured when validation ran.
        CheckType expected = result.ExpectedCheck;
        CheckType selected = result.PlayerCheck;

        ArticleBlockView view = FindBlockView(result.Block);

        Color expectedColor = GetTypeColor(view, expected);
        Color selectedColor = GetTypeColor(view, selected);

        SetAnswerText(
            playerMarkText,
            correctAnswerPrefix + GetTypeLabel(expected),
            expectedColor
        );

        SetAnswerText(
            expectedAnswerText,
            playerAnswerPrefix + GetTypeLabel(selected),
            selectedColor
        );

        SetAnswerText(
            blockTypeText,
            GetTypeLabel(expected),
            expectedColor
        );

        SetAnswerText(
            markedTypeText,
            GetTypeLabel(selected),
            selectedColor
        );

        SetImage(blockTypeIcon, GetTypeIcon(expected));
        SetImage(markedTypeIcon, GetTypeIcon(selected));

        if (blockTypeIcon != null)
        {
            blockTypeIcon.color = expectedColor;
        }

        if (markedTypeIcon != null)
        {
            markedTypeIcon.color = selectedColor;
        }
    }

    private ArticleBlockView FindBlockView(Block block)
    {
        if (block == null)
            return null;

        ArticleBlockView view = block.GetComponent<ArticleBlockView>();

        if (view == null)
            view = block.GetComponentInParent<ArticleBlockView>();

        if (view == null)
            view = block.GetComponentInChildren<ArticleBlockView>(true);

        return view;
    }

    private Color GetTypeColor(ArticleBlockView view, CheckType type)
    {
        if (view == null)
            return Color.white;

        switch (type)
        {
            case CheckType.True: return view.trueColor;
            case CheckType.Label: return view.labelColor;
            case CheckType.Source: return view.sourceColor;
            case CheckType.AI: return view.aiColor;
            case CheckType.Specialist: return view.specialistColor;
            case CheckType.Falacy: return view.falacyColor;
            default: return view.defaultTextColor;
        }
    }

    private void SetAnswerText(TMP_Text target, string text, Color color)
    {
        if (target == null)
            return;

        target.text = text;
        target.color = color;
    }

    private void SetImage(Image target, Sprite sprite)
    {
        if (target == null)
            return;

        target.sprite = sprite;
        target.preserveAspect = true;
        target.enabled = sprite != null;
    }

    private Sprite GetTypeIcon(CheckType type)
    {
        foreach (TypeIcon entry in typeIcons)
        {
            if (entry != null && entry.type == type)
                return entry.icon;
        }

        return null;
    }

    private string GetTypeLabel(CheckType type)
    {
        switch (type)
        {
            case CheckType.None: return "Fato";
            case CheckType.Label: return "Fraude";
            case CheckType.Source: return "Fonte";
            case CheckType.Specialist: return "Consulta";
            default: return type.ToString();
        }
    }

    private void UpdateProgress()
    {
        int position = results.Count > 0 ? currentIndex + 1 : 0;
        float progress = results.Count > 0
            ? (float)position / results.Count
            : 0f;

        if (progressFillImage != null)
            progressFillImage.fillAmount = progress;

        if (progressBar != null)
            progressBar.SetValueWithoutNotify(progress);

        if (progressText != null)
            progressText.text = position + " / " + results.Count;
    }

    private void CreateBlockButtons()
    {
        if (blockButtonsContainer == null || blockButtonPrefab == null)
            return;

        for (int i = 0; i < results.Count; i++)
        {
            int index = i;
            Button button = Instantiate(
                blockButtonPrefab,
                blockButtonsContainer
            );

            button.gameObject.SetActive(true);
            SetButtonText(button, (i + 1).ToString());
            button.onClick.AddListener(() => ShowFeedbackBlock(index));
            spawnedBlockButtons.Add(button);
        }
    }

    private void UpdateBlockButtons()
    {
        for (int i = 0; i < spawnedBlockButtons.Count; i++)
        {
            Button button = spawnedBlockButtons[i];

            if (button == null)
                continue;

            Color color = i == currentIndex
                ? selectedBlockButtonColor
                : normalBlockButtonColor;

            button.transition = Selectable.Transition.ColorTint;

            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
            colors.selectedColor = color;
            colors.disabledColor = color;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0f;
            button.colors = colors;
        }
    }

    private void ClearBlockButtons()
    {
        foreach (Button button in spawnedBlockButtons)
        {
            if (button == null)
                continue;

            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }

        spawnedBlockButtons.Clear();
    }

    public void ShowFeedbackBlock(int index)
    {
        if (!isBlockDisplayed ||
            index < 0 ||
            index >= results.Count ||
            index == currentIndex ||
            lastNavigationFrame == Time.frameCount)
            return;

        lastNavigationFrame = Time.frameCount;
        ShowBlock(index);
    }

    public void ShowPreviousBlock()
    {
        if (!isBlockDisplayed || lastNavigationFrame == Time.frameCount)
            return;

        lastNavigationFrame = Time.frameCount;

        if (currentIndex == 0)
            FinishFeedback();
        else
            ShowBlock(currentIndex - 1);
    }

    public void ShowNextBlock()
    {
        if (!isBlockDisplayed ||
            !canGoNext ||
            lastNavigationFrame == Time.frameCount)
            return;

        lastNavigationFrame = Time.frameCount;

        if (currentIndex == results.Count - 1)
            FinishFeedback();
        else
            ShowBlock(currentIndex + 1);
    }

    private IEnumerator EnableNextButtonAfterDelay()
    {
        yield return new WaitForSeconds(displayDelay);
        enableNextCoroutine = null;
        EnableNextButton();
    }

    private void EnableNextButton()
    {
        canGoNext = true;

        if (nextButton != null)
            nextButton.interactable = true;
    }

    private void StopNextDelay()
    {
        if (enableNextCoroutine != null)
        {
            StopCoroutine(enableNextCoroutine);
            enableNextCoroutine = null;
        }
    }

    private void FinishFeedback()
    {
        EndFeedback(true);
    }

    public void CloseFeedback()
    {
        EndFeedback(false);
    }

    private void EndFeedback(bool returnToChat)
    {
        StopNextDelay();

        Action callback = onFeedbackComplete;
        onFeedbackComplete = null;
        isBlockDisplayed = false;
        canGoNext = false;
        currentIndex = 0;

        ClearBlockButtons();
        results.Clear();
        UpdateProgress();

        if (backButton != null)
            backButton.interactable = false;

        if (nextButton != null)
            nextButton.interactable = false;

        if (feedbackPanel != null)
            feedbackPanel.SetActive(false);

        if (returnToChat && UIManager.Instance != null)
            UIManager.Instance.ShowScreen(UIManager.Screens.Chat);

        callback?.Invoke();
    }

    private void SetButtonText(Button button, string value)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = value;
    }

    private void OnEnable()
    {
        if (isBlockDisplayed && results.Count > 0)
            ShowBlock(currentIndex);
    }

    private void OnDisable()
    {
        StopNextDelay();
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(ShowPreviousBlock);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(ShowNextBlock);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseFeedback);

        ClearBlockButtons();
    }
}