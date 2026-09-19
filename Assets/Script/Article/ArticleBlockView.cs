using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArticleBlockView : MonoBehaviour
{
    [Header("Text / Image")]
    public TMP_Text blockText;
    public GameObject textContainer;
    public GameObject imageContainer;

    [Header("Checklist Icon")]
    public RectTransform checklistContainer;
    //public Vector2 checklistIconSize = new Vector2(150f, 150f);

    [Header("Selection")]
    public GameObject markedVisual;
    public TMP_Text selectedCheckTypeText;

    [Header("Block Component")]
    public Block blockComponent;

    [Header("Text Colors")]
    public Color defaultTextColor = new Color(1f, 1f, 1f, 1f);
    public Color trueColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color labelColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color sourceColor = new Color(0.2f, 0.5f, 1f, 1f);
    public Color aiColor = new Color(0.2f, 0.9f, 0.9f, 1f);
    public Color specialistColor = new Color(0.8f, 0.2f, 0.8f, 1f);
    public Color falacyColor = new Color(1f, 0.2f, 0.2f, 1f);

    private ArticleBlock block;
    private bool isMarked;
    private bool isResettingChecklist;
    private CheckType playerCheckType = CheckType.None;

    public ArticleBlock Block => block;
    public bool IsMarked => isMarked;
    public CheckType PlayerCheckType => playerCheckType;
    public RectTransform ChecklistContainer => checklistContainer;

    private void Awake()
    {
        CacheReferences();

        if (blockText != null)
        {
            blockText.color = defaultTextColor;
        }
    }

    private void CacheReferences()
    {
        if (blockComponent == null)
        {
            blockComponent = GetComponent<Block>();
        }

        if (blockText == null)
        {
            blockText = GetComponent<TMP_Text>();
        }
    }

    public void Initialize(ArticleBlock articleBlock)
    {
        CacheReferences();

        // Clear the previous object before replacing the block data.
        ResetChecklistContainer();

        block = articleBlock;

        if (block == null)
        {
            if (blockText != null)
            {
                blockText.text = string.Empty;
            }

            Debug.LogError(
                "ArticleBlockView: ArticleBlock is null on " +
                gameObject.name
            );

            return;
        }

        if (blockComponent != null)
        {
            blockComponent.Initialize(block);
        }

        DisplayBlock();
    }

    public void ResetChecklistContainer()
    {
        if (isResettingChecklist)
        {
            return;
        }

        isResettingChecklist = true;

        try
        {
            CacheReferences();

            DraggableObject current =
                blockComponent != null
                    ? blockComponent.CurrentDraggable
                    : null;

            // Capture the children before resetting them.
            DraggableObject[] icons =
                checklistContainer != null
                    ? checklistContainer.GetComponentsInChildren<
                        DraggableObject
                    >(true)
                    : new DraggableObject[0];

            if (current != null)
            {
                RemoveChecklistIcon(current);
            }

            // Also remove leftover icons that are no longer registered.
            foreach (DraggableObject icon in icons)
            {
                if (icon == null || icon == current)
                {
                    continue;
                }

                // Never destroy the container itself.
                if (icon.transform == checklistContainer)
                {
                    continue;
                }

                RemoveChecklistIcon(icon);
            }

            ClearPlayerSelection();

            if (checklistContainer != null &&
                checklistContainer.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.MarkLayoutForRebuild(
                    checklistContainer
                );
            }

            Debug.Log(
                "[ARTICLE BLOCK VIEW] Checklist reset on " +
                gameObject.name
            );
        }
        finally
        {
            isResettingChecklist = false;
        }
    }

    private void RemoveChecklistIcon(DraggableObject icon)
    {
        if (icon == null)
        {
            return;
        }

        // Unregister the mark using the existing draggable logic.
        icon.ResetDraggable();

        if (icon == null)
        {
            return;
        }

        // Destroy is deferred, so hide and detach immediately.
        icon.gameObject.SetActive(false);
        icon.transform.SetParent(null, false);

        Destroy(icon.gameObject);
    }

    private void DisplayBlock()
    {
        if (block == null || blockText == null)
        {
            return;
        }

        blockText.text = block.Text;
        blockText.ForceMeshUpdate();

        RectTransform currentRect =
            GetComponent<RectTransform>();

        if (currentRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                currentRect
            );
        }
    }

    public void SetMarked(bool value)
    {
        isMarked = value;

        if (markedVisual != null)
        {
            markedVisual.SetActive(value);
        }
    }

    public void SetPlayerCheckType(CheckType type)
    {
        ApplyPlayerCheckType(type);
    }

    public void SetMarkColorFromDraggable(CheckType type)
    {
        ApplyPlayerCheckType(type);
    }

    private void ApplyPlayerCheckType(CheckType type)
    {
        if (type == CheckType.None)
        {
            ClearPlayerSelection();
            return;
        }

        playerCheckType = type;
        isMarked = true;

        if (markedVisual != null)
        {
            markedVisual.SetActive(true);
        }

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = GetCheckTypeName(type);
            selectedCheckTypeText.gameObject.SetActive(true);
        }

        SetTextColor(type);
    }

    public bool PlaceIconInChecklist(DraggableObject draggable)
    {
        if (draggable == null)
        {
            return false;
        }

        if (checklistContainer == null)
        {
            Debug.LogWarning(
                "[ARTICLE BLOCK VIEW] Checklist Container " +
                "is not assigned on " +
                gameObject.name
            );

            return false;
        }

        RectTransform draggableRect =
            draggable.GetComponent<RectTransform>();

        if (draggableRect == null)
        {
            Debug.LogWarning(
                "[ARTICLE BLOCK VIEW] Icon has no RectTransform."
            );

            return false;
        }

        draggableRect.SetParent(checklistContainer, false);

        draggableRect.anchorMin = new Vector2(0.5f, 0.5f);
        draggableRect.anchorMax = new Vector2(0.5f, 0.5f);
        draggableRect.pivot = new Vector2(0.5f, 0.5f);

        draggableRect.anchoredPosition3D = Vector3.zero;
        draggableRect.localRotation = Quaternion.identity;
        draggableRect.localScale = Vector3.one;

        //if (checklistIconSize.x > 0f &&
        //    checklistIconSize.y > 0f)
        //{
        //    draggableRect.sizeDelta = checklistIconSize;
        //}

        LayoutElement layoutElement =
            draggable.GetComponent<LayoutElement>();

        LayoutGroup layoutGroup =
            checklistContainer.GetComponent<LayoutGroup>();

        //if (layoutElement != null)
        //{
        //    layoutElement.ignoreLayout = layoutGroup == null;

        //    if (checklistIconSize.x > 0f &&
        //        checklistIconSize.y > 0f)
        //    {
        //        layoutElement.preferredWidth = checklistIconSize.x;
        //        layoutElement.preferredHeight = checklistIconSize.y;
        //    }
        //}

        LayoutRebuilder.ForceRebuildLayoutImmediate(
            checklistContainer
        );

        return true;
    }

    public void ClearPlayerSelection()
    {
        // Visual reset only. Keeping this separate prevents recursion
        // when DraggableObject.ResetDraggable calls this method.
        isMarked = false;
        playerCheckType = CheckType.None;

        if (markedVisual != null)
        {
            markedVisual.SetActive(false);
        }

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = string.Empty;
            selectedCheckTypeText.gameObject.SetActive(false);
        }

        if (blockText != null)
        {
            blockText.color = defaultTextColor;
        }
    }

    private void SetTextColor(CheckType type)
    {
        if (blockText != null)
        {
            blockText.color = GetColorForType(type);
        }
    }

    private Color GetColorForType(CheckType type)
    {
        switch (type)
        {
            case CheckType.True:
                return trueColor;

            case CheckType.Label:
                return labelColor;

            case CheckType.Source:
                return sourceColor;

            case CheckType.AI:
                return aiColor;

            case CheckType.Specialist:
                return specialistColor;

            case CheckType.Falacy:
                return falacyColor;

            default:
                return defaultTextColor;
        }
    }

    private string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True:
                return "True Check";

            case CheckType.Label:
                return "Label Check";

            case CheckType.Source:
                return "Source Check";

            case CheckType.AI:
                return "AI Check";

            case CheckType.Specialist:
                return "Specialist Check";

            case CheckType.Falacy:
                return "Fallacy Check";

            default:
                return "None";
        }
    }
}