using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonClickMarker : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    public CheckType checkType;

    [Header("Visual Feedback")]
    public GameObject selectedFeedback;

    private bool isSelected = false;
    private static ButtonClickMarker currentlySelected;

    private void Start()
    {
        if (selectedFeedback != null)
        {
            selectedFeedback.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Deselect previous
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.Deselect();
        }

        // Toggle selection
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

        Debug.Log("Selected: " + checkType);
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

        Debug.Log("Deselected: " + checkType);
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public CheckType GetCheckType()
    {
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