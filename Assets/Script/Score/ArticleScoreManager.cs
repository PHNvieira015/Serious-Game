using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArticleScoreManager : MonoBehaviour
{
    [Header("Score Screen")]
    public TMP_Text articleTitleText;
    public TMP_Text correctBlocksText;
    public TMP_Text totalPointsText;

    [Header("Credibility Bar")]
    public Slider credibilitySlider;
    public TMP_Text feedbackScoreText;
    public TMP_Text feedbackPointsChangeText;
    public TMP_Text feedbackMultiplierText;

    [Tooltip("The number of points represented by a full bar.")]
    [Min(1)]
    public int pointsForFullBar = 500;

    [Min(0f)]
    public float scoreAnimationDuration = 0.6f;

    public Color pointsGainedColor = Color.green;
    public Color pointsLostColor = Color.red;
    public Color normalMultiplierColor = Color.white;
    public Color activeMultiplierColor = Color.yellow;

    [Header("Block Points")]
    public int pointsPerCorrectBlock = 10;
    public int pointsPerWrongBlock = -5;
    public int pointsPerMissedBlock = -5;

    [Header("Consecutive Correct Blocks")]
    [Tooltip("Multiplier increase for each correct block after the first.")]
    [Min(0f)]
    public float multiplierIncrease = 0.5f;

    [Min(1f)]
    public float maximumMultiplier = 3f;

    [Tooltip(
        "Article-end bonus per step beyond the first in the " +
        "consecutive correct-block streak remaining at article end."
    )]
    [Min(0)]
    public int bonusPerStreakStep = 25;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private class ArticleScore
    {
        public string title;
        public int totalBlocks;
        public int correctBlocks;

        public int startingPoints;
        public int endingPoints;
        public int streakBonus;

        public float startingMultiplier;
        public float endingMultiplier;

        public int[] blockPoints;
        public float[] blockMultipliers;
        public bool[] viewed;

        public int viewedCount;
        public bool visualCompleted;
    }

    private readonly Dictionary<ArticleData, ArticleScore> scoredArticles =
        new Dictionary<ArticleData, ArticleScore>();

    private readonly Dictionary<BlockResult, int> resultIndices =
        new Dictionary<BlockResult, int>();

    private ArticleScore lastArticle;
    private ValidationResult currentValidation;

    private int totalPoints;
    private int totalCorrectBlocks;
    private int completedArticles;
    private int correctArticles;

    // These now count consecutive correct blocks.
    private int currentStreak;
    private int bestStreak;

    private int revealedBlockPoints;
    private bool replayingArticle;

    private float displayedPoints;
    private float displayedMultiplier = 1f;
    private int visualTarget;
    private Coroutine scoreAnimation;

    public int TotalPoints => totalPoints;
    public int CurrentStreak => currentStreak;
    public int BestStreak => bestStreak;
    public int TotalCorrectBlocks => totalCorrectBlocks;
    public int CompletedArticles => completedArticles;
    public int CorrectArticles => correctArticles;

    private void OnEnable()
    {
        RefreshUI();
        UpdateFeedbackBar();
        UpdateMultiplierText();
    }

    private float GetMultiplier(int streak)
    {
        int steps = Mathf.Max(0, streak - 1);

        return Mathf.Min(
            Mathf.Max(1f, maximumMultiplier),
            1f + steps * Mathf.Max(0f, multiplierIncrease)
        );
    }

    public bool RecordArticle(
        ArticleData article,
        ValidationResult result)
    {
        if (article == null ||
            result == null ||
            result.TotalCount == 0)
        {
            Debug.LogWarning(
                "[SCORE] Article or validation results are missing.",
                this
            );

            return false;
        }

        StopScoreAnimation();
        resultIndices.Clear();

        currentValidation = result;
        revealedBlockPoints = 0;

        // Revalidating an article cannot change score or streak.
        if (scoredArticles.TryGetValue(
            article,
            out ArticleScore existing))
        {
            lastArticle = existing;
            replayingArticle = true;

            RefreshUI();

            if (showDebugMessages)
            {
                Debug.Log(
                    "[SCORE] Article already scored: " + article.Title,
                    this
                );
            }

            return false;
        }

        replayingArticle = false;

        List<BlockResult> source = result.AllBlockResults.Count > 0
            ? result.AllBlockResults
            : result.BlockResults;

        List<BlockResult> validResults = new List<BlockResult>();

        foreach (BlockResult item in source)
        {
            if (item != null)
                validResults.Add(item);
        }

        int startingPoints = totalPoints;
        float startingMultiplier = GetMultiplier(currentStreak);

        int[] blockPoints = new int[validResults.Count];
        float[] blockMultipliers = new float[validResults.Count];

        int earnedBlockPoints = 0;

        // Calculate in article order, independent of feedback navigation.
        for (int i = 0; i < validResults.Count; i++)
        {
            BlockResult item = validResults[i];

            int points;
            float multiplier;

            if (item.ResultType == BlockResultType.Correct)
            {
                currentStreak++;
                bestStreak = Mathf.Max(bestStreak, currentStreak);

                multiplier = GetMultiplier(currentStreak);

                points = Mathf.RoundToInt(
                    pointsPerCorrectBlock * multiplier
                );
            }
            else
            {
                currentStreak = 0;
                multiplier = 1f;

                points = item.ResultType == BlockResultType.Missed
                    ? pointsPerMissedBlock
                    : pointsPerWrongBlock;
            }

            blockPoints[i] = points;
            blockMultipliers[i] = multiplier;

            earnedBlockPoints += points;
            resultIndices[item] = i;

            if (showDebugMessages)
            {
                Debug.Log(
                    "[SCORE BLOCK] " + (i + 1) +
                    " | Result: " + item.ResultType +
                    " | Streak: " + currentStreak +
                    " | Multiplier: x" + multiplier.ToString("0.##") +
                    " | Points: " + points,
                    this
                );
            }
        }

        // Award the article-end bonus from the remaining block streak.
        int streakBonus =
            Mathf.Max(0, currentStreak - 1) *
            Mathf.Max(0, bonusPerStreakStep);

        int earnedPoints = earnedBlockPoints + streakBonus;

        totalPoints = Mathf.Max(0, totalPoints + earnedPoints);

        lastArticle = new ArticleScore
        {
            title = article.Title,
            totalBlocks = result.TotalCount,
            correctBlocks = result.CorrectCount,
            startingPoints = startingPoints,
            endingPoints = totalPoints,
            streakBonus = streakBonus,
            startingMultiplier = startingMultiplier,
            endingMultiplier = GetMultiplier(currentStreak),
            blockPoints = blockPoints,
            blockMultipliers = blockMultipliers,
            viewed = new bool[validResults.Count],
            viewedCount = 0,
            visualCompleted = false
        };

        scoredArticles.Add(article, lastArticle);

        totalCorrectBlocks += result.CorrectCount;
        completedArticles++;

        if (result.IsArticleSolved &&
            result.CorrectCount == result.TotalCount)
        {
            correctArticles++;
        }

        RefreshUI();

        if (showDebugMessages)
        {
            Debug.Log(
                "[SCORE] " + article.Title +
                " | Block points: " + earnedBlockPoints +
                " | Streak bonus: " + streakBonus +
                " | Ending block streak: " + currentStreak +
                " | Total: " + totalPoints,
                this
            );
        }

        return true;
    }

    public void BeginFeedbackScore(ValidationResult result)
    {
        StopScoreAnimation();
        ClearPointsChange();

        bool canReveal =
            lastArticle != null &&
            currentValidation == result &&
            !replayingArticle &&
            !lastArticle.visualCompleted;

        visualTarget = canReveal
            ? Mathf.Max(
                0,
                lastArticle.startingPoints + revealedBlockPoints
            )
            : totalPoints;

        displayedPoints = visualTarget;

        if (canReveal && lastArticle.viewedCount == 0)
        {
            displayedMultiplier = lastArticle.startingMultiplier;
        }
        else if (!canReveal)
        {
            displayedMultiplier = GetMultiplier(currentStreak);
        }

        UpdateFeedbackBar();
        UpdateMultiplierText();
    }

    public void RevealFeedbackBlock(BlockResult result)
    {
        if (lastArticle == null ||
            replayingArticle ||
            result == null ||
            !resultIndices.TryGetValue(result, out int index))
        {
            ClearPointsChange();
            return;
        }

        if (lastArticle.visualCompleted || lastArticle.viewed[index])
        {
            // Do not roll the displayed streak back or replay the gain.
            ClearPointsChange();
            return;
        }

        lastArticle.viewed[index] = true;
        lastArticle.viewedCount++;

        int blockPoints = lastArticle.blockPoints[index];
        revealedBlockPoints += blockPoints;

        // Display the multiplier calculated for this block.
        displayedMultiplier = lastArticle.blockMultipliers[index];
        UpdateMultiplierText();

        bool allViewed =
            lastArticle.viewedCount == lastArticle.viewed.Length;

        int bonus = allViewed ? lastArticle.streakBonus : 0;

        int newTarget = Mathf.Max(
            0,
            lastArticle.startingPoints + revealedBlockPoints + bonus
        );

        if (allViewed)
        {
            newTarget = lastArticle.endingPoints;
            lastArticle.visualCompleted = true;
        }

        ShowPointsChange(blockPoints, bonus);
        AnimateTo(newTarget);
    }

    private void UpdateMultiplierText()
    {
        if (feedbackMultiplierText == null)
            return;

        feedbackMultiplierText.text =
            "x" + displayedMultiplier.ToString("0.##");

        feedbackMultiplierText.color = displayedMultiplier > 1f
            ? activeMultiplierColor
            : normalMultiplierColor;
    }

    private void ShowPointsChange(int blockPoints, int bonus)
    {
        if (feedbackPointsChangeText == null)
            return;

        string text = blockPoints > 0
            ? "+" + blockPoints
            : blockPoints.ToString();

        if (bonus > 0)
            text += " + " + bonus + " bonus";

        feedbackPointsChangeText.text = text;

        feedbackPointsChangeText.color =
            blockPoints + bonus >= 0
                ? pointsGainedColor
                : pointsLostColor;
    }

    private void ClearPointsChange()
    {
        if (feedbackPointsChangeText != null)
            feedbackPointsChangeText.text = string.Empty;
    }

    private void AnimateTo(int target)
    {
        StopScoreAnimation();

        visualTarget = target;

        if (!isActiveAndEnabled ||
            scoreAnimationDuration <= 0f ||
            Mathf.Approximately(displayedPoints, target))
        {
            displayedPoints = target;
            UpdateFeedbackBar();
            return;
        }

        scoreAnimation = StartCoroutine(AnimateScore());
    }

    private IEnumerator AnimateScore()
    {
        float start = displayedPoints;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, scoreAnimationDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            displayedPoints = Mathf.Lerp(
                start,
                visualTarget,
                smoothProgress
            );

            UpdateFeedbackBar();
            yield return null;
        }

        displayedPoints = visualTarget;
        UpdateFeedbackBar();
        scoreAnimation = null;
    }

    private void UpdateFeedbackBar()
    {
        float progress = Mathf.Clamp01(
            displayedPoints / Mathf.Max(1, pointsForFullBar)
        );

        if (credibilitySlider != null)
        {
            credibilitySlider.minValue = 0f;
            credibilitySlider.maxValue = 1f;
            credibilitySlider.wholeNumbers = false;
            credibilitySlider.interactable = false;

            credibilitySlider.SetValueWithoutNotify(progress);
        }

        if (feedbackScoreText != null)
        {
            feedbackScoreText.text =
                Mathf.Max(0, Mathf.RoundToInt(displayedPoints)).ToString();
        }
    }

    public void CompleteFeedbackScore()
    {
        StopScoreAnimation();

        if (lastArticle != null)
        {
            lastArticle.visualCompleted = true;

            for (int i = 0; i < lastArticle.viewed.Length; i++)
                lastArticle.viewed[i] = true;

            lastArticle.viewedCount = lastArticle.viewed.Length;
        }

        visualTarget = totalPoints;
        displayedPoints = totalPoints;
        displayedMultiplier = GetMultiplier(currentStreak);

        ClearPointsChange();
        UpdateFeedbackBar();
        UpdateMultiplierText();
    }

    private void StopScoreAnimation()
    {
        if (scoreAnimation != null)
        {
            StopCoroutine(scoreAnimation);
            scoreAnimation = null;
        }
    }

    public void RefreshUI()
    {
        if (articleTitleText != null)
        {
            articleTitleText.text = lastArticle != null
                ? lastArticle.title
                : string.Empty;
        }

        if (correctBlocksText != null)
        {
            correctBlocksText.text = lastArticle != null
                ? lastArticle.correctBlocks + " / " + lastArticle.totalBlocks
                : "0 / 0";
        }

        if (totalPointsText != null)
            totalPointsText.text = totalPoints.ToString();
    }

    public void OpenScoreScreen()
    {
        RefreshUI();

        if (UIManager.Instance != null)
            UIManager.Instance.OpenScore();
    }

    public void ResetScore()
    {
        StopScoreAnimation();

        scoredArticles.Clear();
        resultIndices.Clear();

        lastArticle = null;
        currentValidation = null;

        replayingArticle = false;
        revealedBlockPoints = 0;

        totalPoints = 0;
        totalCorrectBlocks = 0;
        completedArticles = 0;
        correctArticles = 0;
        currentStreak = 0;
        bestStreak = 0;

        displayedPoints = 0f;
        displayedMultiplier = 1f;
        visualTarget = 0;

        ClearPointsChange();
        UpdateFeedbackBar();
        UpdateMultiplierText();
        RefreshUI();
    }

    private void OnDisable()
    {
        StopScoreAnimation();

        displayedPoints = visualTarget;
        UpdateFeedbackBar();
    }
}