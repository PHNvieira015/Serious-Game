using UnityEngine;
using UnityEngine.EventSystems;

public class BlockClickReceiver : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    public Block block;

    private void Awake()
    {
        if (block == null)
        {
            block = GetComponent<Block>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ButtonClickMarker selected = ButtonClickMarker.GetSelected();

        if (selected == null)
        {
            Debug.Log("No button selected.");
            return;
        }

        if (block == null)
        {
            Debug.LogWarning("Block is null on " + gameObject.name);
            return;
        }

        if (GameManager.Instance != null)
        {
            int remaining = GameManager.Instance.GetRemainingChecks(selected.GetCheckType());
            if (remaining <= 0)
            {
                Debug.Log("Not enough " + selected.GetCheckType() + " remaining!");
                return;
            }
        }

        // Find a draggable of the selected type that is not placed
        DraggableObject[] draggables = FindObjectsByType<DraggableObject>(FindObjectsSortMode.None);

        DraggableObject matchingDraggable = null;

        foreach (DraggableObject draggable in draggables)
        {
            if (draggable.CheckType == selected.GetCheckType() && !draggable.IsPlaced)
            {
                matchingDraggable = draggable;
                break;
            }
        }

        if (matchingDraggable != null)
        {
            // Place on block (will replace existing)
            matchingDraggable.PlaceCopyOnBlock(block);
            Debug.Log("Placed " + selected.GetCheckType() + " on block via click.");
        }
        else
        {
            Debug.LogWarning("No available draggable found for type: " + selected.GetCheckType());
        }

        ButtonClickMarker.ClearSelection();
    }
}