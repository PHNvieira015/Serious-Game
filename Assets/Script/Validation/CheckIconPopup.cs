using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CheckIconPopup : MonoBehaviour
{
    [System.Serializable]
    public class IconOption
    {
        public Button button;

        [Tooltip("Original sidebar draggable for this option.")]
        public DraggableObject iconSource;

        [System.NonSerialized]
        public UnityAction clickAction;
    }

    [Header("Popup")]
    public RectTransform popupPanel;

    [Tooltip("Optional transparent button behind the popup.")]
    public Button outsideClickButton;

    [Header("Options")]
    public List<IconOption> options = new List<IconOption>();

    [Header("Position")]
    [Tooltip("Assign the article ScrollRect's viewport.")]
    public RectTransform visibleArea;

    [Min(0f)]
    public float gap = 8f;

    [Min(0f)]
    public float edgePadding = 8f;

    private Block targetBlock;
    private RectTransform currentAnchor;
    private bool initialized;

    private readonly Vector3[] corners = new Vector3[4];

    public bool IsOpen =>
        popupPanel != null &&
        popupPanel.gameObject.activeInHierarchy;

    private void Awake()
    {
        InitializeButtons();
        Hide();
    }

    private void InitializeButtons()
    {
        if (initialized)
            return;

        initialized = true;

        if (outsideClickButton != null)
            outsideClickButton.onClick.AddListener(Hide);

        foreach (IconOption option in options)
        {
            if (option == null || option.button == null)
                continue;

            IconOption capturedOption = option;

            option.clickAction = () => SelectOption(capturedOption);

            option.button.onClick.AddListener(option.clickAction);
        }
    }

    public void Toggle(Block block, RectTransform anchor)
    {
        if (IsOpen &&
            targetBlock == block &&
            currentAnchor == anchor)
        {
            Hide();
            return;
        }

        Show(block, anchor);
    }

    public void Show(Block block, RectTransform anchor)
    {
        InitializeButtons();

        if (block == null || anchor == null || popupPanel == null)
        {
            Debug.LogWarning(
                "[CHECK POPUP] Assign Block, anchor and Popup Panel.",
                this
            );

            return;
        }

        targetBlock = block;
        currentAnchor = anchor;

        if (outsideClickButton != null)
        {
            outsideClickButton.gameObject.SetActive(true);
            outsideClickButton.transform.SetAsLastSibling();
        }

        popupPanel.gameObject.SetActive(true);
        popupPanel.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupPanel);

        PositionPopup();
    }

    private void LateUpdate()
    {
        if (!IsOpen)
            return;

        if (targetBlock == null ||
            currentAnchor == null ||
            !targetBlock.gameObject.activeInHierarchy ||
            !currentAnchor.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        PositionPopup();
    }

    private Rect GetBounds(
        RectTransform rect,
        RectTransform relativeTo)
    {
        rect.GetWorldCorners(corners);

        Vector2 minimum = new Vector2(
            float.PositiveInfinity,
            float.PositiveInfinity
        );

        Vector2 maximum = new Vector2(
            float.NegativeInfinity,
            float.NegativeInfinity
        );

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 local = relativeTo.InverseTransformPoint(corners[i]);
            Vector2 point = new Vector2(local.x, local.y);

            minimum = Vector2.Min(minimum, point);
            maximum = Vector2.Max(maximum, point);
        }

        return Rect.MinMaxRect(
            minimum.x,
            minimum.y,
            maximum.x,
            maximum.y
        );
    }

    private void PositionPopup()
    {
        RectTransform parent = popupPanel.parent as RectTransform;

        if (parent == null || currentAnchor == null)
            return;

        Rect bounds = parent.rect;

        if (visibleArea != null)
        {
            Rect visible = GetBounds(visibleArea, parent);

            bounds = Rect.MinMaxRect(
                Mathf.Max(bounds.xMin, visible.xMin),
                Mathf.Max(bounds.yMin, visible.yMin),
                Mathf.Min(bounds.xMax, visible.xMax),
                Mathf.Min(bounds.yMax, visible.yMax)
            );
        }

        Rect anchorBounds = GetBounds(currentAnchor, parent);

        if (!bounds.Overlaps(anchorBounds))
        {
            Hide();
            return;
        }

        popupPanel.anchorMin = new Vector2(0.5f, 0.5f);
        popupPanel.anchorMax = new Vector2(0.5f, 0.5f);
        popupPanel.pivot = new Vector2(0f, 1f);
        popupPanel.localScale = Vector3.one;
        popupPanel.localRotation = Quaternion.identity;

        float width = popupPanel.rect.width;
        float height = popupPanel.rect.height;

        float left = bounds.xMin + edgePadding;
        float right = bounds.xMax - edgePadding;
        float bottom = bounds.yMin + edgePadding;
        float top = bounds.yMax - edgePadding;

        float spaceBelow = anchorBounds.yMin - gap - bottom;
        float spaceAbove = top - anchorBounds.yMax - gap;

        bool openBelow =
            height <= spaceBelow ||
            (height > spaceAbove && spaceBelow >= spaceAbove);

        float x = anchorBounds.xMin;

        // With a top-left pivot, y is the popup's top edge.
        float y = openBelow
            ? anchorBounds.yMin - gap
            : anchorBounds.yMax + gap + height;

        x = right - width >= left
            ? Mathf.Clamp(x, left, right - width)
            : left;

        y = bottom + height <= top
            ? Mathf.Clamp(y, bottom + height, top)
            : top;

        popupPanel.localPosition = new Vector3(x, y, 0f);
    }

    private void SelectOption(IconOption option)
    {
        if (!IsOpen || targetBlock == null)
            return;

        if (option == null || option.iconSource == null)
        {
            Debug.LogWarning(
                "[CHECK POPUP] Assign this option's Icon Source.",
                this
            );

            return;
        }

        if (TryPlace(targetBlock, option.iconSource))
        {
            ButtonClickMarker.ClearSelection();
            Hide();
        }
    }

    public bool TryPlaceSelectedType(Block block, CheckType type)
    {
        foreach (IconOption option in options)
        {
            if (option == null || option.iconSource == null)
                continue;

            if (option.iconSource.CheckType == type)
                return TryPlace(block, option.iconSource);
        }

        Debug.LogWarning(
            "[CHECK POPUP] No Icon Source assigned for " + type,
            this
        );

        return false;
    }

    private bool TryPlace(Block block, DraggableObject source)
    {
        if (block == null || source == null)
            return false;

        CheckType type = source.CheckType;

        // Keep the existing icon without spending another check.
        if (block.CurrentDraggable != null &&
            block.CurrentDraggable.CheckType == type)
        {
            return true;
        }

        // Shared placement handles the mark, replacement and container.
        return source.TryPlaceCopyOnBlock(block, type);
    }

    public void Hide()
    {
        targetBlock = null;
        currentAnchor = null;

        if (popupPanel != null && popupPanel.gameObject.activeSelf)
            popupPanel.gameObject.SetActive(false);

        if (outsideClickButton != null &&
            outsideClickButton.gameObject.activeSelf)
        {
            outsideClickButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Do not call Hide here: it can cause recursive SetActive calls.
        targetBlock = null;
        currentAnchor = null;
    }

    private void OnDestroy()
    {
        if (outsideClickButton != null)
            outsideClickButton.onClick.RemoveListener(Hide);

        foreach (IconOption option in options)
        {
            if (option != null &&
                option.button != null &&
                option.clickAction != null)
            {
                option.button.onClick.RemoveListener(option.clickAction);
            }
        }
    }
}