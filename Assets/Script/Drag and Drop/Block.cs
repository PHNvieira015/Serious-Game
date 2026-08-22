using UnityEngine;
using UnityEngine.EventSystems;

public class Block : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ArticleBlock articleBlock;
    private bool isSolved;
    private bool isHovered;

    public ArticleBlock ArticleBlock => articleBlock;
    public bool IsSolved => isSolved;

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

        if (articleBlock == null)
        {
            Debug.LogError($"Block {name}: ArticleBlock is null.");
        }

        UpdateVisuals();
    }

    public bool ValidateCheck(DraggableObject action)
    {
        if (articleBlock == null || action == null || isSolved)
            return false;

        if (articleBlock.CheckType == CheckType.True)
            return false;

        CheckType correctCheck = articleBlock.CheckType;
        CheckType playerCheck = action.CheckType;

        return playerCheck == correctCheck;
    }

    public void MarkAsSolved()
    {
        isSolved = true;
        UpdateVisuals();

        if (OnBlockSolved != null)
            OnBlockSolved.Invoke(this);
    }

    private void UpdateVisuals()
    {
        if (solvedVisual != null)
            solvedVisual.SetActive(isSolved);

        if (selectedVisual != null)
            selectedVisual.SetActive(false);

        if (hoverVisual != null)
            hoverVisual.SetActive(false);
    }

    public void SelectBlock()
    {
        if (selectedVisual != null && !isSolved)
            selectedVisual.SetActive(true);
    }

    public void DeselectBlock()
    {
        if (selectedVisual != null)
            selectedVisual.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSolved)
        {
            isHovered = true;
            if (hoverVisual != null)
                hoverVisual.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (hoverVisual != null)
            hoverVisual.SetActive(false);
    }
}