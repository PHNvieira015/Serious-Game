using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour
{
    public TextMeshProUGUI headerField;
    public TextMeshProUGUI ContentField;
    public LayoutElement layoutElement;
    public int characterWrapLimit;

    public RectTransform rectTransform;

    [Header("Tooltip Position")]
    public float tooltipRightOffset = 20f;
    public float tooltipLeftOffset = 20f;
    public float tooltipAboveOffset = 20f;
    public float tooltipBelowOffset = 20f;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetText(string content, string header = "")
    {
        if (string.IsNullOrEmpty(header))
        {
            headerField.gameObject.SetActive(false);
        }
        else
        {
            headerField.gameObject.SetActive(true);
            headerField.text = header;
        }

        ContentField.text = content;

        int headerLength = headerField.text.Length;
        int contentLength = ContentField.text.Length;

        layoutElement.enabled =
            headerLength > characterWrapLimit ||
            contentLength > characterWrapLimit;
    }

    private void Update()
    {
        int headerLength = headerField.text.Length;
        int contentLength = ContentField.text.Length;

        layoutElement.enabled =
            headerLength > characterWrapLimit ||
            contentLength > characterWrapLimit;

        if (Mouse.current != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 tooltipPosition = mousePosition;

            // Horizontal positioning
            if (mousePosition.x < Screen.width / 2f)
            {
                // Mouse is on the left -> tooltip goes to the right
                tooltipPosition.x += tooltipRightOffset;

                rectTransform.pivot = new Vector2(
                    0f,
                    rectTransform.pivot.y
                );
            }
            else
            {
                // Mouse is on the right -> tooltip goes to the left
                tooltipPosition.x -= tooltipLeftOffset;

                rectTransform.pivot = new Vector2(
                    1f,
                    rectTransform.pivot.y
                );
            }

            // Vertical positioning
            if (mousePosition.y < Screen.height / 2f)
            {
                // Mouse is at the bottom -> tooltip goes above
                tooltipPosition.y += tooltipAboveOffset;

                rectTransform.pivot = new Vector2(
                    rectTransform.pivot.x,
                    0f
                );
            }
            else
            {
                // Mouse is at the top -> tooltip goes below
                tooltipPosition.y -= tooltipBelowOffset;

                rectTransform.pivot = new Vector2(
                    rectTransform.pivot.x,
                    1f
                );
            }

            transform.position = tooltipPosition;
        }
    }
}
