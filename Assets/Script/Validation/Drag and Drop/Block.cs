using UnityEngine;
using UnityEngine.EventSystems;

public class Block : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private ArticleBlock articleBlock;
    private bool isSolved;
    private bool isHovered;

    private DraggableObject currentDraggable;

    [Header("Validation Information")]
    [SerializeField]
    private CheckType blockType;

    [SerializeField]
    private CheckType markType = CheckType.None;

    public ArticleBlock ArticleBlock => articleBlock;
    public bool IsSolved => isSolved;
    public DraggableObject CurrentDraggable => currentDraggable;

    public CheckType BlockType => blockType;
    public CheckType MarkType => markType;

    public System.Action<Block> OnBlockSolved;

    [Header("Visual Feedback")]
    public GameObject solvedVisual;
    public GameObject selectedVisual;
    public GameObject hoverVisual;

    public void Initialize(ArticleBlock data)
    {
        articleBlock = data;
        isSolved = false;
        isHovered = false;
        currentDraggable = null;

        markType = CheckType.None;
        blockType = CheckType.None;

        UpdateVisuals();

        if (articleBlock == null)
        {
            Debug.LogError(
                "Block " + name + ": ArticleBlock is null.",
                this
            );

            return;
        }

        blockType = articleBlock.CheckType;
    }

    public void SetMarkType(CheckType type)
    {
        markType = type;
    }

    // Connect this to TMP_Dropdown's Dynamic int event.
    public void SetMarkTypeFromDropdown(int value)
    {
        if (!System.Enum.IsDefined(typeof(CheckType), value))
        {
            Debug.LogWarning(
                "[BLOCK] Invalid CheckType value: " + value,
                this
            );

            return;
        }

        markType = (CheckType)value;

        Debug.Log(
            "[BLOCK] " + name + " Mark Type: " + markType,
            this
        );
    }

    public bool ValidateCheck(DraggableObject action)
    {
        if (articleBlock == null ||
            action == null ||
            isSolved)
        {
            return false;
        }

        return action.CheckType == blockType;
    }

    public bool ValidateCurrentDraggable()
    {
        return ValidateMark();
    }

    public bool ValidateMark()
    {
        if (articleBlock == null ||
            isSolved ||
            markType == CheckType.None)
        {
            return false;
        }

        return markType == blockType;
    }

    public void SetDraggable(DraggableObject draggable)
    {
        currentDraggable = draggable;

        markType = draggable != null
            ? draggable.CheckType
            : CheckType.None;
    }

    public void RemoveDraggable(DraggableObject draggable)
    {
        if (currentDraggable == draggable)
        {
            currentDraggable = null;
            markType = CheckType.None;
        }
    }

    public void MarkAsSolved()
    {
        isSolved = true;

        UpdateVisuals();

        if (OnBlockSolved != null)
        {
            OnBlockSolved.Invoke(this);
        }
    }

    private void UpdateVisuals()
    {
        if (solvedVisual != null)
        {
            solvedVisual.SetActive(isSolved);
        }

        if (selectedVisual != null)
        {
            selectedVisual.SetActive(false);
        }

        if (hoverVisual != null)
        {
            hoverVisual.SetActive(false);
        }
    }

    public void SelectBlock()
    {
        if (selectedVisual != null && !isSolved)
        {
            selectedVisual.SetActive(true);
        }
    }

    public void DeselectBlock()
    {
        if (selectedVisual != null)
        {
            selectedVisual.SetActive(false);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSolved)
        {
            isHovered = true;

            if (hoverVisual != null)
            {
                hoverVisual.SetActive(true);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (hoverVisual != null)
        {
            hoverVisual.SetActive(false);
        }
    }
}