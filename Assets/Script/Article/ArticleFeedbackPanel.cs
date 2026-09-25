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
    public TMP_Text markedTypeText;
    public TMP_Text blockTypeText;

    [Header("Type Icons")]
    public Image blockTypeIcon;
    public Image markedTypeIcon;

    [Tooltip("Assign one sprite for each CheckType.")]
    public List<TypeIcon> typeIcons = new List<TypeIcon>();

    [Header("Progress")]
    [Tooltip("Displays current position in the feedback list.")]
    public Slider progressBar;

    [Tooltip("Optional text showing the current position.")]
    public TMP_Text progressText;

    [Header("Block Navigation Buttons")]
    public RectTransform blockButtonsContainer;

    [Tooltip("Button prefab with a TMP_Text child.")]
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
    public Color missedColor = Color.yellow;

    [Header("Settings")]
    [Min(0f)]
    public float displayDelay = 1.5f;

    private readonly List<BlockResult> results =
        new List<BlockResult>();

    private readonly List<Button> spawnedBlockButtons =
        new List<Button>();

    private int currentIndex;
    private Action onFeedbackComplete;
    private bool isBlockDisplayed;
    private bool canGoNext;
    private bool initialized;
    private Coroutine enableNextCoroutine;

    private int lastNavigationFrame = -1;

    private void Awake()
    {
        InitializeControls();
    }

    private void InitializeControls()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (backButton != null)
        {
            backButton.onClick.AddListener(ShowPreviousBlock);
            backButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.onClick.AddListener(ShowNextBlock);
            nextButton.interactable = false;
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseFeedback);
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.wholeNumbers = false;
            progressBar.interactable = false;
            progressBar.SetValueWithoutNotify(0f);
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
                {
                    results.Add(item);
                }
            }
        }

        onFeedbackComplete = onComplete;
        currentIndex = 0;
        isBlockDisplayed = false;
        canGoNext = false;
        lastNavigationFrame = -1;

        if (results.Count == 0)
        {
            Debug.LogWarning("[FEEDBACK] No results to show.");
            CloseFeedback();
            return;
        }

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
        }

        CreateBlockButtons();
        ShowBlock(0);
    }

    private void ShowBlock(int index)
    {
        if (index < 0 || index >= results.Count)
        {
            return;
        }

        StopNextDelay();

        currentIndex = index;
        isBlockDisplayed = true;
        canGoNext = false;

        BlockResult result = results[index];

        if (blockText != null)
        {
            blockText.text = result.BlockText;
        }

        if (blockIndexText != null)
        {
            blockIndexText.text =
                "Checagem " + (index + 1) + " de " + results.Count;
        }

        if (resultText != null)
        {
            resultText.text = result.GetResultText();
            resultText.color = GetResultColor(result.ResultType);
        }

        DisplayTypes(result);
        DisplayExplanation(result);
        UpdateProgress();
        UpdateBlockButtons();

        if (backButton != null)
        {
            backButton.interactable = true;
            SetButtonText(
                backButton,
                index == 0 ? "Return" : "Back"
            );
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

    private void DisplayTypes(BlockResult result)
    {
        CheckType expectedType = result.ExpectedCheck;
        CheckType selectedType = result.PlayerCheck;

        if (result.Block != null)
        {
            expectedType = result.Block.BlockType;
            selectedType = result.Block.MarkType;
        }

        if (expectedAnswerText != null)
        {
            expectedAnswerText.text =
                "Checagem Correta: " +
                GetCheckTypeDisplayName(expectedType);
        }

        if (playerMarkText != null)
        {
            playerMarkText.text =
                "Sua marcacao: " +
                GetCheckTypeDisplayName(selectedType);
        }

        // SWITCHED:
        // blockTypeText now displays the player's marked type.
        if (blockTypeText != null)
        {
            blockTypeText.text = GetTypeLabel(selectedType);
        }

        // SWITCHED:
        // markedTypeText now displays the expected/correct type.
        if (markedTypeText != null)
        {
            markedTypeText.text = GetTypeLabel(expectedType);
        }

        SetTypeIcon(blockTypeIcon, expectedType);
        SetTypeIcon(markedTypeIcon, selectedType);
    }

    private void SetTypeIcon(Image image, CheckType type)
    {
        if (image == null)
        {
            return;
        }

        Sprite sprite = GetTypeIcon(type);

        image.sprite = sprite;
        image.preserveAspect = true;
        image.enabled = sprite != null;
    }

    private Sprite GetTypeIcon(CheckType type)
    {
        foreach (TypeIcon entry in typeIcons)
        {
            if (entry != null && entry.type == type)
            {
                return entry.icon;
            }
        }

        return null;
    }

    private void DisplayExplanation(BlockResult result)
    {
        if (explanationText == null)
        {
            return;
        }

        if (result.Block != null &&
            result.Block.ArticleBlock != null)
        {
            string explanation =
                result.Block.ArticleBlock.Explanation;

            explanationText.text =
                string.IsNullOrEmpty(explanation)
                    ? "No explanation provided."
                    : explanation;
        }
        else
        {
            explanationText.text = "No explanation available.";
        }
    }

    private void CreateBlockButtons()
    {
        if (blockButtonsContainer == null ||
            blockButtonPrefab == null)
        {
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            int targetIndex = i;

            Button newButton = Instantiate(
                blockButtonPrefab,
                blockButtonsContainer
            );

            newButton.name = "FeedbackBlock_" + (i + 1);
            newButton.gameObject.SetActive(true);

            SetButtonText(newButton, (i + 1).ToString());

            newButton.onClick.AddListener(
                () => ShowFeedbackBlock(targetIndex)
            );

            spawnedBlockButtons.Add(newButton);
        }
    }

    private void UpdateBlockButtons()
    {
        for (int i = 0; i < spawnedBlockButtons.Count; i++)
        {
            Button button = spawnedBlockButtons[i];

            if (button == null)
            {
                continue;
            }

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
            if (button != null)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
        }

        spawnedBlockButtons.Clear();
    }

    private void UpdateProgress()
    {
        int position = results.Count > 0
            ? currentIndex + 1
            : 0;

        if (progressBar != null)
        {
            float progress = results.Count > 0
                ? (float)position / results.Count
                : 0f;

            progressBar.SetValueWithoutNotify(progress);
        }

        if (progressText != null)
        {
            progressText.text =
                position + " / " + results.Count;
        }
    }

    // Uses zero-based indices: 0 is the first feedback block.
    public void ShowFeedbackBlock(int index)
    {
        if (!isBlockDisplayed ||
            index < 0 ||
            index >= results.Count ||
            index == currentIndex ||
            lastNavigationFrame == Time.frameCount)
        {
            return;
        }

        lastNavigationFrame = Time.frameCount;
        ShowBlock(index);
    }

    public void ShowPreviousBlock()
    {
        if (!isBlockDisplayed ||
            lastNavigationFrame == Time.frameCount)
        {
            return;
        }

        lastNavigationFrame = Time.frameCount;

        if (currentIndex <= 0)
        {
            FinishFeedback();
            return;
        }

        ShowBlock(currentIndex - 1);
    }

    public void ShowNextBlock()
    {
        if (!isBlockDisplayed ||
            !canGoNext ||
            lastNavigationFrame == Time.frameCount)
        {
            return;
        }

        lastNavigationFrame = Time.frameCount;

        if (currentIndex >= results.Count - 1)
        {
            FinishFeedback();
            return;
        }

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
        {
            nextButton.interactable = true;
        }
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
        lastNavigationFrame = -1;

        ClearBlockButtons();
        results.Clear();
        UpdateProgress();

        if (backButton != null)
        {
            backButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        if (returnToChat && UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIManager.Screens.Chat);
        }

        callback?.Invoke();
    }

    private Color GetResultColor(BlockResultType type)
    {
        switch (type)
        {
            case BlockResultType.Wrong:
                return wrongColor;

            case BlockResultType.Missed:
                return missedColor;

            default:
                return correctColor;
        }
    }

    private string GetTypeLabel(CheckType type)
    {
        switch (type)
        {
            case CheckType.None:
                return "Fato";

            case CheckType.Label:
                return "Fraude";

            case CheckType.Source:
                return "Fonte";

            case CheckType.Specialist:
                return "Consulta";

            default:
                return type.ToString();
        }
    }

    private string GetCheckTypeDisplayName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True:
                return "Verdadeiro";

            case CheckType.Label:
                return "Tendencioso";

            case CheckType.Source:
                return "Fonte";

            case CheckType.AI:
                return "IA";

            case CheckType.Specialist:
                return "Especialista";

            case CheckType.Falacy:
                return "Falacia";

            default:
                return "Nenhum";
        }
    }

    private void SetButtonText(Button button, string value)
    {
        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
        {
            label.text = value;
        }
    }

    private void OnEnable()
    {
        if (isBlockDisplayed && results.Count > 0)
        {
            ShowBlock(currentIndex);
        }
    }

    private void OnDisable()
    {
        StopNextDelay();
    }

    private void OnDestroy()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(ShowPreviousBlock);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextBlock);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseFeedback);
        }

        ClearBlockButtons();
    }
}
