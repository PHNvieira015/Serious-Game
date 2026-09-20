using UnityEngine;
using UnityEngine.EventSystems;

public class ChecklistPopupButton :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("References")]
    public Block block;
    public RectTransform checklistContainer;
    public CheckIconPopup iconPopup;

    private void Awake()
    {
        CacheReferences();
    }

    private void CacheReferences()
    {
        if (checklistContainer == null)
        {
            checklistContainer = GetComponent<RectTransform>();
        }

        if (block == null)
        {
            block = GetComponentInParent<Block>();
        }

        if (block == null)
        {
            ArticleBlockView view =
                GetComponentInParent<ArticleBlockView>();

            if (view != null)
            {
                block = view.blockComponent;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OpenPopup();
    }

    public void OpenPopup()
    {
        CacheReferences();

        if (iconPopup == null)
        {
            Debug.LogWarning(
                "[CHECKLIST] Assign Icon Popup on " +
                gameObject.name
            );

            return;
        }

        if (block == null || checklistContainer == null)
        {
            Debug.LogWarning(
                "[CHECKLIST] Assign Block and Checklist Container on " +
                gameObject.name
            );

            return;
        }

        iconPopup.Toggle(block, checklistContainer);
    }
}