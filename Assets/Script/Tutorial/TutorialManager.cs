using System.Collections;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialHighlight highlight;

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

        Debug.Log("Tutorial finished.");
    }
}
