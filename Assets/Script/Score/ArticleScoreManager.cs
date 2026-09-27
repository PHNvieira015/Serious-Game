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

    [Tooltip("Displays points earned from the current article only.")]
    public TMP_Text totalPointsText;

    [Header("Credibility Bar")]
    public Slider credibilitySlider;
    public TMP_Text feedbackScoreText;
    public TMP_Text feedbackPointsChangeText;
    public TMP_Text feedbackMultiplierText;

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
    [Min(0f)]
    public float multiplierIncrease = 0.5f;

    [Min(1f)]
    public float maximumMultiplier = 3f;

    [Tooltip("Article-end bonus based on the remaining block streak.")]
    [Min(0)]
    public int bonusPerStreakStep = 25;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private class ArticleScore
    {
        public string title;
        public int totalBlocks;
        public int correctBlocks;

        public int articlePoints;
        public int startingPoints;
        public int endingPoints;
        public int streakBonus;

        public float startingMultiplier;
        public int[] blockPoints;
        public float[] blockMultipliers;
        public bool[] viewed;

        public int viewedCount;
        public bool visualCompleted;
    }

    private readonly HashSet<ArticleData> scoredArticles =
        new HashSet<ArticleData>();

    private readonly Dictionary<BlockResult, int> resultIndices =
        new Dictionary<BlockResult, int>();

    private ArticleScore lastArticle;
    private ValidationResult currentValidation;

    private int totalPoints;
    private int totalCorrectBlocks;
    private int completedArticles;
    private int correctArticles;
    private int currentStreak;
    private int bestStreak;

    private int revealedBlockPoints;
    private bool replayingArticle;
    private float displayedPoints;
    private float displayedMultiplier = 1f;
    private int visualTarget;
    private Coroutine scoreAnimation;

    public int TotalPoints => totalPoints;

    public int LastArticlePoints =>
        lastArticle != null ? lastArticle.articlePoints : 0;

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
        return Mathf.Min(
            Mathf.Max(1f, maximumMultiplier),
            1f + Mathf.Max(0, streak - 1) *
            Mathf.Max(0f, multiplierIncrease)
        );
    }

    public bool RecordArticle(
        ArticleData article,
        ValidationResult result)
    {
        return RecordArticle(article, result, true);
    }

    public bool RecordArticle(
        ArticleData article,
        ValidationResult result,
        bool allowPoints)
    {
        if (article == null ||
            result == null ||
            result.TotalCount == 0)
        {
            Debug.LogWarning("[SCORE] Missing article results.", this);
            return false;
        }

        StopScoreAnimation();
        resultIndices.Clear();

        currentValidation = result;
        revealedBlockPoints = 0;

        replayingArticle =
            !allowPoints || scoredArticles.Contains(article);

        if (replayingArticle)
        {
            lastArticle = new ArticleScore
            {
                title = article.Title,
                totalBlocks = result.TotalCount,
                correctBlocks = result.CorrectCount,

                // Retries award no points.
                articlePoints = 0,

                startingPoints = totalPoints,
                endingPoints = totalPoints,
                startingMultiplier = GetMultiplier(currentStreak),
                blockPoints = new int[0],
                blockMultipliers = new float[0],
                viewed = new bool[0],
                visualCompleted = true
            };

            scoredArticles.Add(article);
            RefreshUI();

            if (showDebugMessages)
            {
                Debug.Log(
                    "[SCORE] Retry: results updated, no points or " +
                    "streak changes for " + article.Title,
                    this
                );
            }

            return false;
        }

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
        float[] multipliers = new float[validResults.Count];

        int earnedBlockPoints = 0;

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
            multipliers[i] = multiplier;
            resultIndices[item] = i;

            earnedBlockPoints += points;
        }

        int bonus =
            Mathf.Max(0, currentStreak - 1) *
            Mathf.Max(0, bonusPerStreakStep);

        int earnedArticlePoints = earnedBlockPoints + bonus;

        // Credibility remains the accumulated score, with a zero floor.
        totalPoints = Mathf.Max(
            0,
            totalPoints + earnedArticlePoints
        );

        lastArticle = new ArticleScore
        {
            title = article.Title,
            totalBlocks = result.TotalCount,
            correctBlocks = result.CorrectCount,

            // The score screen displays this article's result.
            articlePoints = earnedArticlePoints,

            startingPoints = startingPoints,
            endingPoints = totalPoints,
            streakBonus = bonus,
            startingMultiplier = startingMultiplier,
            blockPoints = blockPoints,
            blockMultipliers = multipliers,
            viewed = new bool[validResults.Count]
        };

        scoredArticles.Add(article);
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
                " | Article points: " + earnedArticlePoints +
                " | Bonus included: " + bonus +
                " | Credibility total: " + totalPoints,
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
            displayedMultiplier = lastArticle.startingMultiplier;
        else if (!canReveal)
            displayedMultiplier = GetMultiplier(currentStreak);

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
            ClearPointsChange();
            return;
        }

        lastArticle.viewed[index] = true;
        lastArticle.viewedCount++;

        int points = lastArticle.blockPoints[index];
        revealedBlockPoints += points;

        displayedMultiplier = lastArticle.blockMultipliers[index];
        UpdateMultiplierText();

        bool allViewed =
            lastArticle.viewedCount == lastArticle.viewed.Length;

        int bonus = allViewed ? lastArticle.streakBonus : 0;

        int target = Mathf.Max(
            0,
            lastArticle.startingPoints + revealedBlockPoints + bonus
        );

        if (allViewed)
        {
            target = lastArticle.endingPoints;
            lastArticle.visualCompleted = true;
        }

        ShowPointsChange(points, bonus);
        AnimateTo(target);
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

    private void ShowPointsChange(int points, int bonus)
    {
        if (feedbackPointsChangeText == null)
            return;

        string text = points > 0 ? "+" + points : points.ToString();

        if (bonus > 0)
            text += " + " + bonus + " bonus";

        feedbackPointsChangeText.text = text;
        feedbackPointsChangeText.color = points + bonus >= 0
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

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration)
            );

            displayedPoints = Mathf.Lerp(start, visualTarget, progress);
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
        if (scoreAnimation == null)
            return;

        StopCoroutine(scoreAnimation);
        scoreAnimation = null;
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
        {
            totalPointsText.text = lastArticle != null
                ? lastArticle.articlePoints.ToString()
                : "0";
        }
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