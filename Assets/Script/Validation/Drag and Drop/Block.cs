using UnityEngine;
using UnityEngine.EventSystems;

public class Block : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ArticleBlock articleBlock;
    private bool isSolved;
    private bool isHovered;

    private DraggableObject currentDraggable;

    [Header("Validation Information")]
    [SerializeField]
    private CheckType blockType;

    [SerializeField]
    private CheckType markType;

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

        markType = default(CheckType);

        if (articleBlock == null)
        {
            Debug.LogError($"Block {name}: ArticleBlock is null.");
            return;
        }

        blockType = articleBlock.CheckType;

        UpdateVisuals();
    }

    public bool ValidateCheck(DraggableObject action)
    {
        if (articleBlock == null)
        {
            return false;
        }

        if (action == null)
        {
            return false;
        }

        if (isSolved)
        {
            return false;
        }

        CheckType correctCheck = blockType;
        CheckType playerCheck = action.CheckType;

        return playerCheck == correctCheck;
    }

    public bool ValidateCurrentDraggable()
    {
        if (currentDraggable == null)
        {
            return false;
        }

        return ValidateCheck(currentDraggable);
    }

    public void SetDraggable(DraggableObject draggable)
    {
        currentDraggable = draggable;

        if (draggable != null)
        {
            markType = draggable.CheckType;
        }
        else
        {
            markType = default(CheckType);
        }
    }

    public void RemoveDraggable(DraggableObject draggable)
    {
        if (currentDraggable == draggable)
        {
            currentDraggable = null;
            markType = default(CheckType);
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