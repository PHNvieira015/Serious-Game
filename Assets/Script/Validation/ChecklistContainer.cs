using UnityEngine;
using UnityEngine.EventSystems;

public class CheckListContainer : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    public Block block;

    [Tooltip("The container that receives the placed icon.")]
    public RectTransform checklistContainer;

    public CheckIconPopup iconPopup;

    private int lastClickFrame = -1;

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (block == null)
            block = GetComponentInParent<Block>();

        ArticleBlockView view = GetComponentInParent<ArticleBlockView>();

        if (block == null && view != null)
            block = view.blockComponent;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left ||
            eventData.dragging)
        {
            return;
        }

        OpenPopup();
    }

    public void OpenPopup()
    {
        // Prevent a duplicate call from a Button OnClick in the same frame.
        if (lastClickFrame == Time.frameCount)
            return;

        lastClickFrame = Time.frameCount;

        CacheReferences();

        if (block == null ||
            checklistContainer == null ||
            iconPopup == null)
        {
            Debug.LogWarning(
                "[CHECKLIST] Assign Block, Checklist Container and Icon Popup.",
                this
            );

            return;
        }

        ButtonClickMarker selected = ButtonClickMarker.GetSelected();

        if (selected != null)
        {
            iconPopup.Hide();

            bool placed = iconPopup.TryPlaceSelectedType(
                block,
                selected.GetCheckType()
            );

            if (placed)
                ButtonClickMarker.ClearSelection();

            return;
        }

        iconPopup.Toggle(block, checklistContainer);
    }
}