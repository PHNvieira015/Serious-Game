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

        if (block == null)
        {
            block = GetComponentInParent<Block>();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        ButtonClickMarker selected =
            ButtonClickMarker.GetSelected();

        if (selected == null || block == null)
        {
            return;
        }

        CheckType selectedType = selected.GetCheckType();

        // Use the selected button's own source first.
        DraggableObject source = selected.checkTypeSource;

        if (source == null)
        {
            source = selected.GetComponent<DraggableObject>();
        }

        if (source == null)
        {
            source = selected.GetComponentInChildren<DraggableObject>(
                true
            );
        }

        // Compatibility with buttons that use a separate source.
        if (source == null)
        {
            DraggableObject[] available =
                FindObjectsByType<DraggableObject>(
                    FindObjectsSortMode.None
                );

            foreach (DraggableObject candidate in available)
            {
                if (!candidate.IsPlaced &&
                    !candidate.IsSpawnedInstance &&
                    candidate.CheckType == selectedType)
                {
                    source = candidate;
                    break;
                }
            }
        }

        if (source == null)
        {
            Debug.LogWarning(
                "[CLICK] No icon source assigned for " +
                selectedType +
                ". Assign Check Type Source on " +
                selected.name
            );

            return;
        }

        bool placed = source.TryPlaceCopyOnBlock(
            block,
            selectedType
        );

        if (!placed)
        {
            Debug.LogWarning(
                "[CLICK] Placement failed on " +
                block.name +
                ". See the preceding warning."
            );

            return;
        }

        Debug.Log(
            "[CLICK] Placed " +
            selectedType +
            " in the checklist container for " +
            block.name
        );

        ButtonClickMarker.ClearSelection();
    }
}