using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TutorialStep
{
    [Header("Target")]
    [Tooltip("UI element that should be highlighted.")]
    public RectTransform target;

    [Header("Tutorial Text")]
    [TextArea(2, 5)]
    [Tooltip("Message displayed to the player during this step.")]
    public string tutorialText;

    [Header("Interaction")]
    [Tooltip("Button that the player needs to interact with.")]
    public Button button;

    [Header("Timing")]
    [Tooltip("How long this step stays highlighted.")]
    [Min(0f)]
    public float duration = 3f;

    [Header("Behaviour")]
    [Tooltip("If enabled, the tutorial waits for the player to click the button.")]
    public bool waitForClick = true;
}
