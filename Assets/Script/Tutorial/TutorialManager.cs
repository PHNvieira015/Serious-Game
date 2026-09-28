using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialHighlight highlight;

    [Header("Tutorial Text")]
    [SerializeField] private TMP_Text tutorialText;

    [Header("Starting Tutorial")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private TutorialStep[] steps;

    private class TutorialRequest
    {
        public TutorialStep[] steps;
        public Action onStarted;
        public Action onFinished;
    }

    private readonly Queue<TutorialRequest> requests =
        new Queue<TutorialRequest>();

    private bool isRunning;

    public bool IsRunning => isRunning;

    private void Start()
    {
        if (playOnStart)
        {
            PlaySteps(steps);
        }
    }

    public bool PlaySteps(
        TutorialStep[] tutorialSteps,
        Action onStarted = null,
        Action onFinished = null)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning(
                "[TUTORIAL] Keep TutorialManager active.",
                this
            );
            return false;
        }

        if (highlight == null || tutorialText == null)
        {
            Debug.LogWarning(
                "[TUTORIAL] Assign Highlight and Tutorial Text.",
                this
            );
            return false;
        }

        if (tutorialSteps == null || tutorialSteps.Length == 0)
        {
            return false;
        }

        requests.Enqueue(new TutorialRequest
        {
            steps = (TutorialStep[])tutorialSteps.Clone(),
            onStarted = onStarted,
            onFinished = onFinished
        });

        if (!isRunning)
        {
            isRunning = true;
            StartCoroutine(ProcessRequests());
        }

        return true;
    }

    private IEnumerator ProcessRequests()
    {
        while (requests.Count > 0)
        {
            TutorialRequest request = requests.Dequeue();

            request.onStarted?.Invoke();

            // Allow newly activated UI to update.
            yield return null;

            foreach (TutorialStep step in request.steps)
            {
                if (step.target == null)
                {
                    Debug.LogWarning(
                        "[TUTORIAL] A step has no target.",
                        this
                    );
                    continue;
                }

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
            tutorialText.text = string.Empty;

            request.onFinished?.Invoke();
        }

        isRunning = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        requests.Clear();
        isRunning = false;

        if (highlight != null)
        {
            highlight.Hide();
        }

        if (tutorialText != null)
        {
            tutorialText.text = string.Empty;
        }
    }
}