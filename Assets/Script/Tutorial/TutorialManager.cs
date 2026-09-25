using System.Collections;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialHighlight highlight;

    [Header("Tutorial Text")]
    [SerializeField] private TMP_Text tutorialText;

    [SerializeField] private TutorialStep[] steps;

    private int currentStep;

    private void Start()
    {
        StartCoroutine(PlayTutorial());
    }

    private IEnumerator PlayTutorial()
    {
        for (currentStep = 0; currentStep < steps.Length; currentStep++)
        {
            TutorialStep step = steps[currentStep];

            if (step.target == null)
            {
                Debug.LogWarning(
                    $"Tutorial step {currentStep} has no target."
                );

                continue;
            }

            // Show the text for this step
            tutorialText.text = step.tutorialText;

            if (step.waitForClick && step.button != null)
            {
                yield return highlight.ShowUntilClicked(
                    step.target,
                    step.button
                );
            }
            else
            {
                yield return highlight.Show(
                    step.target,
                    step.duration
                );
            }
        }

        highlight.Hide();

        // Clear tutorial text
        tutorialText.text = "";

        Debug.Log("Tutorial finished.");
    }
}
