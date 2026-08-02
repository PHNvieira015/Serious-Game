using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Progress")]
    public int currentDay = 1;
    public int maxDays = 30;

    [Header("Score & Mistakes")]
    public int currentScore = 0;
    public int mistakesAllowed = 3;
    public int currentMistakes = 0;

    [Header("Resources - Available checks per day")]
    public int labelChecksRemaining = 3;
    public int sourceChecksRemaining = 3;
    public int aiChecksRemaining = 3;
    public int specialistChecksRemaining = 2;
    public int falacyChecksRemaining = 0;

    [Header("Daily Budget")]
    public int baseDailyChecks = 0;
    public int bonusChecksPerDay = 0;

    [Header("UI Elements")]
    public Text scoreText;
    public Text dayText;
    public Text mistakesText;
    public Text dateText;

    [Header("Resource UI (Optional)")]
    public Text labelChecksText;
    public Text sourceChecksText;
    public Text aiChecksText;
    public Text specialistChecksText;
    public Text falacyChecksText;

    private System.DateTime currentDate;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNewDay();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateUI();
    }

    public void InitializeNewDay()
    {
        labelChecksRemaining = baseDailyChecks + bonusChecksPerDay;
        sourceChecksRemaining = baseDailyChecks + bonusChecksPerDay;
        aiChecksRemaining = baseDailyChecks + bonusChecksPerDay;
        specialistChecksRemaining = baseDailyChecks + bonusChecksPerDay;
        falacyChecksRemaining = baseDailyChecks + bonusChecksPerDay;

        currentDate = new System.DateTime(2019, 10, 01).AddDays(currentDay + 15);

        UpdateUI();
    }

    public bool UseCheck(CheckType checkType)
    {
        switch (checkType)
        {
            case CheckType.LabelCheck:
                if (labelChecksRemaining > 0)
                {
                    labelChecksRemaining--;
                    UpdateUI();
                    return true;
                }
                break;
            case CheckType.SourceCheck:
                if (sourceChecksRemaining > 0)
                {
                    sourceChecksRemaining--;
                    UpdateUI();
                    return true;
                }
                break;
            case CheckType.AICheck:
                if (aiChecksRemaining > 0)
                {
                    aiChecksRemaining--;
                    UpdateUI();
                    return true;
                }
                break;
            case CheckType.SpecialistCheck:
                if (specialistChecksRemaining > 0)
                {
                    specialistChecksRemaining--;
                    UpdateUI();
                    return true;
                }
                break;
            case CheckType.FalacyCheck:
                if (falacyChecksRemaining > 0)
                {
                    falacyChecksRemaining--;
                    UpdateUI();
                    return true;
                }
                break;
        }

        Debug.Log("Not enough " + checkType + " remaining!");
        return false;
    }

    public int GetRemainingChecks(CheckType checkType)
    {
        switch (checkType)
        {
            case CheckType.LabelCheck: return labelChecksRemaining;
            case CheckType.SourceCheck: return sourceChecksRemaining;
            case CheckType.AICheck: return aiChecksRemaining;
            case CheckType.SpecialistCheck: return specialistChecksRemaining;
            case CheckType.FalacyCheck: return falacyChecksRemaining;
            default: return 0;
        }
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateUI();
    }

    public void AddMistake()
    {
        currentMistakes++;
        UpdateUI();

        if (currentMistakes >= mistakesAllowed)
        {
            GameOver();
        }
    }

    public void NextDay()
    {
        if (currentDay < maxDays)
        {
            currentDay++;
            currentMistakes = 0;
            InitializeNewDay();
            UpdateUI();

            Debug.Log("Starting Day " + currentDay + " - " + GetDateString());
        }
        else
        {
            Debug.Log("Congratulations! You completed all days!");
        }
    }

    public string GetDateString()
    {
        return currentDate.ToString("dd/MM/yyyy");
    }

    void UpdateUI()
    {
        if (scoreText) scoreText.text = "Score: " + currentScore;
        if (dayText) dayText.text = "Day: " + currentDay;
        if (mistakesText) mistakesText.text = "Mistakes: " + currentMistakes + "/" + mistakesAllowed;
        if (dateText) dateText.text = GetDateString();

        if (labelChecksText) labelChecksText.text = "Label: " + labelChecksRemaining;
        if (sourceChecksText) sourceChecksText.text = "Source: " + sourceChecksRemaining;
        if (aiChecksText) aiChecksText.text = "AI: " + aiChecksRemaining;
        if (specialistChecksText) specialistChecksText.text = "Specialist: " + specialistChecksRemaining;
        if (falacyChecksText) falacyChecksText.text = "Falacy: " + falacyChecksRemaining;
    }

    void GameOver()
    {
        Debug.Log("Game Over! You made too many mistakes.");
        Time.timeScale = 0;
    }

    public void ApplyEndOfDayBonus()
    {
        int bonus = Mathf.FloorToInt(currentScore / 1000);
        bonusChecksPerDay += bonus;
        Debug.Log("End of day bonus! +" + bonus + " checks per type tomorrow.");
    }
}