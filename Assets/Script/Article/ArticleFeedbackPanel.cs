using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ArticleFeedbackPanel : MonoBehaviour
{
    [Header("Feedback Panel")]
    public GameObject feedbackPanel;

    [Header("UI References")]
    public TMP_Text blockText;
    public TMP_Text blockIndexText;
    public TMP_Text resultText;
    public TMP_Text expectedAnswerText;
    public TMP_Text playerMarkText;
    public TMP_Text explanationText;

    [Header("Navigation")]
    public Button backButton;
    public Button nextButton;
    public Button closeButton;

    [Header("Colors")]
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color missedColor = Color.yellow;

    [Header("Settings")]
    public float displayDelay = 1.5f;

    private List<BlockResult> results = new List<BlockResult>();
    private int currentIndex = 0;
    private System.Action onFeedbackComplete;
    private bool isBlockDisplayed = false;
    private bool isTransitioning = false;
    private Coroutine enableNextCoroutine;

    private int lastNextFrame = -1;
    private int lastPrevFrame = -1;

    private void Start()
    {
        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ShowPreviousBlock);
            backButton.interactable = false;
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowNextBlock);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseFeedback);
        }
    }

    public void StartFeedback(ValidationResult result, System.Action onComplete = null)
    {
        if (result == null || result.BlockResults == null || result.BlockResults.Count == 0)
        {
            Debug.LogWarning("[FEEDBACK] No results to show");
            CloseFeedback();
            return;
        }

        results = result.BlockResults;
        currentIndex = 0;
        onFeedbackComplete = onComplete;
        isBlockDisplayed = false;
        isTransitioning = false;
        lastNextFrame = -1;
        lastPrevFrame = -1;

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(true);
        }

        ShowBlock(currentIndex);
    }

    private void ShowBlock(int index)
    {
        if (index < 0 || index >= results.Count)
        {
            Debug.LogWarning($"[FEEDBACK] ShowBlock out of range: {index}");
            CloseFeedback();
            return;
        }

        BlockResult result = results[index];

        // Skip nulls with ShowBlock (not ShowNextBlock) so we don't double-increment the index.
        if (result == null)
        {
            Debug.LogWarning($"[FEEDBACK] result[{index}] is NULL - skipping.");

            if (index < results.Count - 1)
            {
                ShowBlock(index + 1);
            }
            else
            {
                CloseFeedback();
            }
            return;
        }

        isBlockDisplayed = true;

        if (backButton != null)
        {
            backButton.interactable = true;

            TMP_Text backButtonText =
                backButton.GetComponentInChildren<TMP_Text>();

            if (backButtonText != null)
            {
                backButtonText.text = (index == 0) ? "Return" : "Back";
            }
        }

        if (blockIndexText != null)
        {
            blockIndexText.text = "Block " + (index + 1) + " of " + results.Count;
            blockIndexText.ForceMeshUpdate();
        }

        if (blockText != null)
        {
            blockText.text = result.BlockText;
            blockText.ForceMeshUpdate();
        }

        if (resultText != null)
        {
            Color color = correctColor;

            switch (result.ResultType)
            {
                case BlockResultType.Correct:
                    color = correctColor;
                    break;

                case BlockResultType.Wrong:
                    color = wrongColor;
                    break;

                case BlockResultType.Missed:
                    color = missedColor;
                    break;
            }

            resultText.text = result.GetResultText();
            resultText.color = color;
            resultText.ForceMeshUpdate();
        }

        if (expectedAnswerText != null)
        {
            expectedAnswerText.text =
                "Checagem Correta: " +
                GetCheckTypeDisplayName(result.ExpectedCheck);

            expectedAnswerText.ForceMeshUpdate();
        }

        if (playerMarkText != null)
        {
            if (result.HasDraggable)
            {
                playerMarkText.text =
                    "Sua Marcação: " +
                    GetCheckTypeDisplayName(result.PlayerCheck);
            }
            else
            {
                playerMarkText.text = "Sua Marcação: Nenhuma";
            }

            playerMarkText.ForceMeshUpdate();
        }

        if (explanationText != null)
        {
            if (result.Block != null && result.Block.ArticleBlock != null)
            {
                string explanation = result.Block.ArticleBlock.Explanation;

                explanationText.text =
                    string.IsNullOrEmpty(explanation)
                        ? "No explanation provided."
                        : explanation;
            }
            else
            {
                explanationText.text = "No explanation available.";
            }

            explanationText.ForceMeshUpdate();
        }

        if (nextButton != null)
        {
            nextButton.interactable = false;

            TMP_Text buttonText =
                nextButton.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                if (index == results.Count - 1)
                {
                    buttonText.text = "Finish";
                }
                else
                {
                    buttonText.text = "Next";
                }
            }
        }

        if (enableNextCoroutine != null)
        {
            StopCoroutine(enableNextCoroutine);
            enableNextCoroutine = null;
        }

        enableNextCoroutine = StartCoroutine(EnableNextButtonAfterDelay());
    }

    private IEnumerator EnableNextButtonAfterDelay()
    {
        yield return new WaitForSeconds(displayDelay);

        if (nextButton != null)
        {
            nextButton.interactable = true;
        }

        enableNextCoroutine = null;
    }

    public void ShowPreviousBlock()
    {
        // Frame lock: prevents duplicate button events from double-advancing.
        if (lastPrevFrame == Time.frameCount)
        {
            return;
        }

        if (isTransitioning || !isBlockDisplayed)
        {
            return;
        }

        if (currentIndex <= 0)
        {
            FinishFeedback();
            return;
        }

        lastPrevFrame = Time.frameCount;
        isTransitioning = true;
        currentIndex--;
        ShowBlock(currentIndex);
        isTransitioning = false;
    }

    public void ShowNextBlock()
    {
        // Frame lock: prevents duplicate button events from double-advancing.
        if (lastNextFrame == Time.frameCount)
        {
            return;
        }

        if (!isBlockDisplayed || isTransitioning)
        {
            return;
        }

        if (currentIndex >= results.Count - 1)
        {
            FinishFeedback();
            return;
        }

        lastNextFrame = Time.frameCount;
        isTransitioning = true;
        currentIndex++;
        ShowBlock(currentIndex);
        isTransitioning = false;
    }

    private void FinishFeedback()
    {
        StopAllCoroutines();
        enableNextCoroutine = null;

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowScreen(UIManager.Screens.Chat);
        }

        if (onFeedbackComplete != null)
        {
            onFeedbackComplete.Invoke();
        }

        results.Clear();
        currentIndex = 0;
        isBlockDisplayed = false;
        isTransitioning = false;
        lastNextFrame = -1;
        lastPrevFrame = -1;

        if (backButton != null)
        {
            backButton.interactable = false;
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

            case CheckType.None:
            default:
                return "Nenhum";
        }
    }

    public void CloseFeedback()
    {
        StopAllCoroutines();
        enableNextCoroutine = null;

        if (feedbackPanel != null)
        {
            feedbackPanel.SetActive(false);
        }

        if (onFeedbackComplete != null)
        {
            onFeedbackComplete.Invoke();
        }

        results.Clear();
        currentIndex = 0;
        isBlockDisplayed = false;
        isTransitioning = false;
        lastNextFrame = -1;
        lastPrevFrame = -1;

        if (backButton != null)
        {
            backButton.interactable = false;
        }
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
    }
}