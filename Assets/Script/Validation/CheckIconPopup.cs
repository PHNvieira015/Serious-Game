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

        [Tooltip("Assign the original sidebar draggable for this icon.")]
        public DraggableObject iconSource;

        [System.NonSerialized]
        public UnityAction clickAction;
    }

    [Header("Popup")]
    public RectTransform popupPanel;

    [Tooltip("Full-screen transparent button behind the popup.")]
    public Button outsideClickButton;

    [Header("Icon Buttons")]
    public List<IconOption> options = new List<IconOption>();

    [Header("Position")]
    public Vector2 offset = new Vector2(0f, -8f);

    [Min(0f)]
    public float edgePadding = 8f;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private Block targetBlock;
    private RectTransform currentAnchor;
    private bool initialized;

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
        {
            return;
        }

        initialized = true;

        if (outsideClickButton != null)
        {
            outsideClickButton.onClick.AddListener(Hide);
        }

        foreach (IconOption option in options)
        {
            if (option == null || option.button == null)
            {
                continue;
            }

            IconOption capturedOption = option;

            option.clickAction = () =>
            {
                SelectOption(capturedOption);
            };

            option.button.onClick.AddListener(
                option.clickAction
            );
        }
    }

    public void Toggle(
        Block block,
        RectTransform checklistContainer)
    {
        if (IsOpen &&
            targetBlock == block &&
            currentAnchor == checklistContainer)
        {
            Hide();
            return;
        }

        Show(block, checklistContainer);
    }

    public void Show(
        Block block,
        RectTransform checklistContainer)
    {
        InitializeButtons();

        if (block == null ||
            checklistContainer == null ||
            popupPanel == null)
        {
            Debug.LogWarning(
                "[CHECK POPUP] Assign the Block, Checklist " +
                "Container, and Popup Panel."
            );

            return;
        }

        targetBlock = block;
        currentAnchor = checklistContainer;

        // The blocker goes above the article, below the popup.
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

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHECK POPUP] Opened for " + targetBlock.name
            );
        }
    }

    private void LateUpdate()
    {
        if (!IsOpen)
        {
            return;
        }

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

    private void PositionPopup()
    {
        if (popupPanel == null || currentAnchor == null)
        {
            return;
        }

        RectTransform popupParent =
            popupPanel.parent as RectTransform;

        if (popupParent == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        currentAnchor.GetWorldCorners(corners);

        // Corner zero is the checklist's bottom-left corner.
        Vector3 localCorner =
            popupParent.InverseTransformPoint(corners[0]);

        popupPanel.anchorMin = new Vector2(0.5f, 0.5f);
        popupPanel.anchorMax = new Vector2(0.5f, 0.5f);
        popupPanel.pivot = new Vector2(0f, 1f);
        popupPanel.localScale = Vector3.one;
        popupPanel.localRotation = Quaternion.identity;

        Vector2 position = new Vector2(
            localCorner.x + offset.x,
            localCorner.y + offset.y
        );

        Rect bounds = popupParent.rect;

        float width = popupPanel.rect.width;
        float height = popupPanel.rect.height;

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

        popupPanel.localPosition = new Vector3(
            position.x,
            position.y,
            0f
        );
    }

    private void SelectOption(IconOption option)
    {
        if (!IsOpen || targetBlock == null)
        {
            return;
        }

        if (option == null || option.iconSource == null)
        {
            Debug.LogWarning(
                "[CHECK POPUP] Assign an Icon Source " +
                "for this popup button."
            );

            return;
        }

        CheckType type = option.iconSource.CheckType;

        // Choosing the current mark does not spend another check.
        if (targetBlock.CurrentDraggable != null &&
            targetBlock.CurrentDraggable.CheckType == type)
        {
            ButtonClickMarker.ClearSelection();
            Hide();
            return;
        }

        bool placed = option.iconSource.TryPlaceCopyOnBlock(
            targetBlock,
            type
        );

        if (!placed)
        {
            Debug.LogWarning(
                "[CHECK POPUP] Could not place " +
                type +
                " on " +
                targetBlock.name
            );

            return;
        }

        if (showDebugMessages)
        {
            Debug.Log(
                "[CHECK POPUP] Marked " +
                targetBlock.name +
                " as " +
                type
            );
        }

        ButtonClickMarker.ClearSelection();
        Hide();
    }

    public void Hide()
    {
        targetBlock = null;
        currentAnchor = null;

        if (popupPanel != null)
        {
            popupPanel.gameObject.SetActive(false);
        }

        if (outsideClickButton != null)
        {
            outsideClickButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        if (outsideClickButton != null)
        {
            outsideClickButton.onClick.RemoveListener(Hide);
        }

        foreach (IconOption option in options)
        {
            if (option != null &&
                option.button != null &&
                option.clickAction != null)
            {
                option.button.onClick.RemoveListener(
                    option.clickAction
                );
            }
        }
    }
}