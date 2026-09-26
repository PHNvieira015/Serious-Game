using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageControlsParentSize : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image childImage;
    [SerializeField] private RectTransform parentRect;

    [Header("Size")]
    [Min(0.01f)]
    public float sizeMultiplier = 1f;

    public Vector2 padding = Vector2.zero;

    [Tooltip("Collapse the size when the Image is disabled or has no sprite.")]
    public bool collapseWhenEmpty = true;

    private RectTransform imageRect;
    private LayoutElement parentLayout;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        RefreshSize();
    }

    private void CacheReferences()
    {
        if (childImage == null)
            childImage = GetComponent<Image>();

        imageRect = childImage.rectTransform;

        if (parentRect == null)
            parentRect = imageRect.parent as RectTransform;

        if (parentRect != null)
            parentLayout = parentRect.GetComponent<LayoutElement>();
    }

    private void LateUpdate()
    {
        RefreshSize();
    }

    public void RefreshSize()
    {
        CacheReferences();

        if (parentRect == null || parentRect == imageRect)
            return;

        bool hasImage =
            childImage.enabled &&
            childImage.overrideSprite != null;

        if (!hasImage && !collapseWhenEmpty)
            return;

        Vector2 imageSize = Vector2.zero;

        if (hasImage)
        {
            // Uses the Image's native sprite size in UI units.
            imageSize = new Vector2(
                childImage.preferredWidth,
                childImage.preferredHeight
            ) * Mathf.Max(0.01f, sizeMultiplier);
        }

        Vector2 parentSize = hasImage
            ? imageSize + new Vector2(
                Mathf.Max(0f, padding.x),
                Mathf.Max(0f, padding.y)
            )
            : Vector2.zero;

        // Center the child without stretching it.
        imageRect.anchorMin = new Vector2(0.5f, 0.5f);
        imageRect.anchorMax = new Vector2(0.5f, 0.5f);
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.anchoredPosition = Vector2.zero;
        imageRect.localScale = Vector3.one;

        SetRectSize(imageRect, imageSize);
        SetRectSize(parentRect, parentSize);

        // Allows an outer Layout Group to use the requested size.
        if (parentLayout != null)
        {
            if (!Mathf.Approximately(
                parentLayout.preferredWidth,
                parentSize.x))
            {
                parentLayout.preferredWidth = parentSize.x;
            }

            if (!Mathf.Approximately(
                parentLayout.preferredHeight,
                parentSize.y))
            {
                parentLayout.preferredHeight = parentSize.y;
            }
        }
    }

    private void SetRectSize(RectTransform target, Vector2 size)
    {
        if (!Mathf.Approximately(target.rect.width, size.x))
        {
            target.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Horizontal,
                size.x
            );
        }

        if (!Mathf.Approximately(target.rect.height, size.y))
        {
            target.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                size.y
            );
        }
    }
}