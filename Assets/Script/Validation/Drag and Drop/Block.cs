using UnityEngine;
using UnityEngine.EventSystems;

public class Block : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private ArticleBlock articleBlock;
    private bool isSolved;
    private bool isHovered;

    // Compatibility with existing scripts still in the project.
    private DraggableObject currentDraggable;

    [Header("Validation Information")]
    [SerializeField]
    private CheckType blockType;

    [SerializeField]
    private CheckType markType = CheckType.None;

    [Header("Article View")]
    public ArticleBlockView articleBlockView;

    [Header("Visual Feedback")]
    public GameObject solvedVisual;
    public GameObject selectedVisual;
    public GameObject hoverVisual;

    public ArticleBlock ArticleBlock => articleBlock;
    public bool IsSolved => isSolved;
    public DraggableObject CurrentDraggable => currentDraggable;

    public CheckType BlockType => blockType;
    public CheckType MarkType => markType;

    public System.Action<Block> OnBlockSolved;

    private void Awake()
    {
        CacheView();
    }

    private void CacheView()
    {
        if (articleBlockView != null)
        {
            return;
        }

        articleBlockView = GetComponent<ArticleBlockView>();

        if (articleBlockView == null)
        {
            articleBlockView =
                GetComponentInParent<ArticleBlockView>();
        }
    }

    public void Initialize(ArticleBlock data)
    {
        articleBlock = data;
        isSolved = false;
        isHovered = false;
        currentDraggable = null;

        blockType = articleBlock != null
            ? articleBlock.CheckType
            : CheckType.None;

        UpdateVisuals();

        // Reset the answer, text color and selection label.
        SetMarkType(CheckType.None);

        if (articleBlock == null)
        {
            Debug.LogError(
                "Block " + name + ": ArticleBlock is null.",
                this
            );
        }
    }

    public void SetMarkType(CheckType type)
    {
        markType = type;

        CacheView();

        if (articleBlockView != null)
        {
            articleBlockView.SetPlayerCheckType(markType);
        }
        else
        {
            Debug.LogWarning(
                "[BLOCK] Assign Article Block View on " + name,
                this
            );
        }
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

        // Update both the answer and its visual appearance.
        SetMarkType((CheckType)value);

        Debug.Log(
            "[BLOCK] " + name + " Mark Type: " + markType,
            this
        );
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

    // Compatibility methods for existing callers.
    // Dropdown marking does not use these methods.

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

    public void SetDraggable(DraggableObject draggable)
    {
        currentDraggable = draggable;

        SetMarkType(
            draggable != null
                ? draggable.CheckType
                : CheckType.None
        );
    }

    public void RemoveDraggable(DraggableObject draggable)
    {
        if (currentDraggable == draggable)
        {
            currentDraggable = null;
            SetMarkType(CheckType.None);
        }
    }
}