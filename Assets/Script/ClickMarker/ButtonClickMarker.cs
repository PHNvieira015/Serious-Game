using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonClickMarker :
    MonoBehaviour,
    IPointerClickHandler
{
    [Header("Settings")]
    public CheckType checkType;

    [Tooltip(
        "Optional. When assigned, the marker automatically " +
        "uses this DraggableObject's CheckType."
    )]
    public DraggableObject checkTypeSource;

    [Header("Visual Feedback")]
    public GameObject selectedFeedback;

    private bool isSelected;
    private static ButtonClickMarker currentlySelected;

    private void Awake()
    {
        FindCheckTypeSource();
        SynchronizeCheckType();
    }

    private void Start()
    {
        if (selectedFeedback != null)
        {
            selectedFeedback.SetActive(false);
        }
    }

    private void FindCheckTypeSource()
    {
        if (checkTypeSource != null)
        {
            return;
        }

        checkTypeSource = GetComponent<DraggableObject>();

        if (checkTypeSource == null)
        {
            checkTypeSource =
                GetComponentInChildren<DraggableObject>(true);
        }
    }

    private void SynchronizeCheckType()
    {
        if (checkTypeSource != null)
        {
            checkType = checkTypeSource.CheckType;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        SynchronizeCheckType();

        if (currentlySelected != null &&
            currentlySelected != this)
        {
            currentlySelected.Deselect();
        }

        if (isSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    private void Select()
    {
        isSelected = true;
        currentlySelected = this;

        if (selectedFeedback != null)
        {
            selectedFeedback.SetActive(true);
        }
    }

    private void Deselect()
    {
        isSelected = false;

        if (currentlySelected == this)
        {
            currentlySelected = null;
        }

        if (selectedFeedback != null)
        {
            selectedFeedback.SetActive(false);
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public CheckType GetCheckType()
    {
        SynchronizeCheckType();
        return checkType;
    }

    public static ButtonClickMarker GetSelected()
    {
        return currentlySelected;
    }

    public static void ClearSelection()
    {
        if (currentlySelected != null)
        {
            currentlySelected.Deselect();
        }
    }

}