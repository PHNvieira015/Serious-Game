using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonTooltip :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Tooltip")]
    [TextArea(2, 6)]
    public string tooltipText;

    public RectTransform tooltipPrefab;

    [Header("Position")]
    public Vector2 mouseOffset = new Vector2(16f, -16f);

    [Min(0f)]
    public float edgePadding = 8f;

    private static ButtonTooltip activeTooltip;

    private RectTransform tooltipInstance;
    private RectTransform canvasRect;
    private Canvas rootCanvas;
    private TMP_Text tooltipLabel;

    private string displayedText;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void ShowTooltip(Vector2 pointerPosition)
    {
        if (activeTooltip != null)
        {
            activeTooltip.HideTooltip();
        }

        if (string.IsNullOrWhiteSpace(tooltipText) ||
            tooltipPrefab == null)
        {
            return;
        }

        Canvas parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
        {
            return;
        }

        rootCanvas = parentCanvas.rootCanvas;
        canvasRect = rootCanvas.GetComponent<RectTransform>();

        if (canvasRect == null)
        {
            return;
        }

        tooltipInstance = Instantiate(
            tooltipPrefab,
            canvasRect,
            false
        );

        tooltipLabel =
            tooltipInstance.GetComponentInChildren<TMP_Text>(true);

        if (tooltipLabel == null)
        {
            Debug.LogWarning(
                "ButtonTooltip: Tooltip prefab needs a TMP_Text."
            );

            HideTooltip();
            return;
        }

        activeTooltip = this;

        tooltipInstance.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipInstance.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipInstance.pivot = new Vector2(0f, 1f);
        tooltipInstance.localScale = Vector3.one;
        tooltipInstance.localRotation = Quaternion.identity;

        // Prevent the tooltip from interfering with button hover.
        Graphic[] graphics =
            tooltipInstance.GetComponentsInChildren<Graphic>(true);

        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }

        CanvasGroup group =
            tooltipInstance.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group = tooltipInstance.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;

        LayoutElement layout =
            tooltipInstance.GetComponent<LayoutElement>();

        if (layout == null)
        {
            layout = tooltipInstance.gameObject.AddComponent<LayoutElement>();
        }

        layout.ignoreLayout = true;

        tooltipInstance.gameObject.SetActive(true);
        tooltipInstance.SetAsLastSibling();

        RefreshText();
        UpdatePosition(pointerPosition);
    }

    private void LateUpdate()
    {
        if (tooltipInstance == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tooltipText))
        {
            HideTooltip();
            return;
        }

        if (displayedText != tooltipText)
        {
            RefreshText();
        }

        if (EventSystem.current != null &&
            EventSystem.current.currentInputModule != null)
        {
            UpdatePosition(
                EventSystem.current.currentInputModule.input.mousePosition
            );
        }
    }

    private void RefreshText()
    {
        displayedText = tooltipText;
        tooltipLabel.text = displayedText;
        tooltipLabel.ForceMeshUpdate();

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            tooltipInstance
        );
    }

    private void UpdatePosition(Vector2 pointerPosition)
    {
        if (tooltipInstance == null || canvasRect == null)
        {
            return;
        }

        Camera uiCamera =
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;

        Vector2 localPoint;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            pointerPosition,
            uiCamera,
            out localPoint))
        {
            return;
        }

        Vector2 position = localPoint + mouseOffset;

        float width = tooltipInstance.rect.width;
        float height = tooltipInstance.rect.height;

        Rect bounds = canvasRect.rect;

        float minX = bounds.xMin + edgePadding;
        float maxX = bounds.xMax - edgePadding - width;

        float minY = bounds.yMin + edgePadding + height;
        float maxY = bounds.yMax - edgePadding;

        position.x = maxX >= minX
            ? Mathf.Clamp(position.x, minX, maxX)
            : minX;

        position.y = maxY >= minY
            ? Mathf.Clamp(position.y, minY, maxY)
            : maxY;

        tooltipInstance.localPosition = new Vector3(
            position.x,
            position.y,
            0f
        );
    }

    public void HideTooltip()
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.gameObject.SetActive(false);
            Destroy(tooltipInstance.gameObject);
        }

        tooltipInstance = null;
        tooltipLabel = null;
        displayedText = null;

        if (activeTooltip == this)
        {
            activeTooltip = null;
        }
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnDestroy()
    {
        HideTooltip();
    }
}
