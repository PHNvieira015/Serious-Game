using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TutorialHighlight : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform highlight;
    [SerializeField] private Image spotlight;

    [Header("Highlight")]
    [Tooltip("Extra space around the target.")]
    [SerializeField] private float padding = 25f;

    [Header("Pulse")]
    [SerializeField] private bool pulse = true;

    [Tooltip("How fast the highlight pulses.")]
    [SerializeField] private float pulseSpeed = 4f;

    [Tooltip("How much the highlight grows/shrinks.")]
    [Range(0f, 0.2f)]
    [SerializeField] private float pulseAmount = 0.04f;

    [Header("Glow / Alpha")]
    [SerializeField] private bool fade = true;

    [Tooltip("Minimum alpha during the pulse.")]
    [Range(0f, 1f)]
    [SerializeField] private float minAlpha = 0.65f;

    [Tooltip("Maximum alpha during the pulse.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 1f;

    [Header("Spotlight")]
    [SerializeField] private float softness = 0.01f;

    private RectTransform currentTarget;

    private Material spotlightMaterial;

    private Canvas parentCanvas;

    private static readonly int CenterID =
        Shader.PropertyToID("_Center");

    private static readonly int SizeID =
        Shader.PropertyToID("_Size");

    private static readonly int SoftnessID =
        Shader.PropertyToID("_Softness");


    private void Awake()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (spotlight != null && spotlight.material != null)
        {
            spotlightMaterial =
                Instantiate(spotlight.material);

            spotlight.material =
                spotlightMaterial;
        }

        Hide();
    }


    // =========================================================
    // SHOW FOR A FIXED AMOUNT OF TIME
    // =========================================================

    public IEnumerator Show(
        RectTransform target,
        float duration)
    {
        if (target == null)
            yield break;

        currentTarget = target;

        ShowVisuals();

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            UpdateHighlight();
            AnimateHighlight();

            yield return null;
        }

        Hide();
    }


    // =========================================================
    // SHOW UNTIL BUTTON IS CLICKED
    // =========================================================

    public IEnumerator ShowUntilClicked(
        RectTransform target,
        Button button)
    {
        if (target == null)
            yield break;

        if (button == null)
        {
            yield return Show(target, 3f);
            yield break;
        }

        currentTarget = target;

        ShowVisuals();

        bool clicked = false;

        void OnClick()
        {
            clicked = true;
        }

        button.onClick.AddListener(OnClick);

        while (!clicked)
        {
            UpdateHighlight();
            AnimateHighlight();

            yield return null;
        }

        button.onClick.RemoveListener(OnClick);

        Hide();
    }


    // =========================================================
    // UPDATE EVERYTHING
    // =========================================================

    private void UpdateHighlight()
    {
        if (currentTarget == null)
            return;

        UpdateHighlightTransform();
        UpdateSpotlight();
    }


    // =========================================================
    // POSITION + SIZE OF THE RING
    // =========================================================

    private void UpdateHighlightTransform()
    {
        if (highlight == null)
            return;

        highlight.position =
            currentTarget.position;

        highlight.sizeDelta =
            currentTarget.rect.size +
            new Vector2(padding, padding);
    }


    // =========================================================
    // POSITION + SIZE OF THE SPOTLIGHT HOLE
    // =========================================================

    private void UpdateSpotlight()
    {
        if (spotlightMaterial == null)
            return;

        RectTransform spotlightRect =
            spotlight.rectTransform;

        Vector3 localPosition =
            spotlightRect.InverseTransformPoint(
                currentTarget.position
            );

        Vector2 spotlightSize =
            spotlightRect.rect.size;

        float x =
            (localPosition.x / spotlightSize.x) + 0.5f;

        float y =
            (localPosition.y / spotlightSize.y) + 0.5f;

        Vector2 center =
            new Vector2(x, y);

        Vector2 targetSize =
            currentTarget.rect.size +
            new Vector2(padding, padding);

        Vector2 size =
            new Vector2(
                targetSize.x / spotlightSize.x,
                targetSize.y / spotlightSize.y
            );

        spotlightMaterial.SetVector(
            CenterID,
            center
        );

        spotlightMaterial.SetVector(
            SizeID,
            size
        );

        spotlightMaterial.SetFloat(
            SoftnessID,
            softness
        );
    }


    // =========================================================
    // PULSE + ALPHA ANIMATION
    // =========================================================

    private void AnimateHighlight()
    {
        if (highlight == null)
            return;

        float wave =
            (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;


        // -------------------------
        // Scale pulse
        // -------------------------

        if (pulse)
        {
            float scale =
                1f +
                Mathf.Lerp(
                    -pulseAmount,
                    pulseAmount,
                    wave
                );

            highlight.localScale =
                Vector3.one * scale;
        }
        else
        {
            highlight.localScale =
                Vector3.one;
        }


        // -------------------------
        // Alpha pulse
        // -------------------------

        if (fade)
        {
            Image image =
                highlight.GetComponent<Image>();

            if (image != null)
            {
                Color color = image.color;

                color.a =
                    Mathf.Lerp(
                        minAlpha,
                        maxAlpha,
                        wave
                    );

                image.color = color;
            }
        }
    }


    // =========================================================
    // SHOW
    // =========================================================

    private void ShowVisuals()
    {
        gameObject.SetActive(true);

        if (spotlight != null)
            spotlight.gameObject.SetActive(true);

        if (highlight != null)
        {
            highlight.gameObject.SetActive(true);

            highlight.localScale =
                Vector3.one;
        }
    }


    // =========================================================
    // HIDE
    // =========================================================

    public void Hide()
    {
        if (highlight != null)
        {
            highlight.gameObject.SetActive(false);

            highlight.localScale =
                Vector3.one;
        }

        if (spotlight != null)
            spotlight.gameObject.SetActive(false);

        currentTarget = null;

        gameObject.SetActive(false);
    }
}
