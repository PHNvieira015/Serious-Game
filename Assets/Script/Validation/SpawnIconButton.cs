using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SpawnIconButton : MonoBehaviour
{
    [Header("References")]
    public Button button;

    [Tooltip("The prefab or scene object to copy.")]
    public GameObject objectToCopy;

    [Tooltip("A dedicated container for the placed icon.")]
    public RectTransform targetContainer;

    [Header("Icon Layout")]
    public Vector2 iconSize = new Vector2(64f, 64f);

    [Tooltip("Prevent the placed icon from blocking clicks or drops.")]
    public bool ignoreRaycasts = true;

    [Header("Optional")]
    [Tooltip("Panel to close after placing the icon.")]
    public GameObject panelToClose;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        button.onClick.AddListener(PlaceIcon);
    }

    public void PlaceIcon()
    {
        if (objectToCopy == null || targetContainer == null)
        {
            Debug.LogWarning(
                "[ICON BUTTON] Assign Object To Copy and Target Container.",
                this
            );

            return;
        }

        // The source and menu button must stay outside the icon holder.
        if (objectToCopy.transform == targetContainer ||
            objectToCopy.transform.IsChildOf(targetContainer) ||
            targetContainer.IsChildOf(objectToCopy.transform) ||
            transform == targetContainer ||
            transform.IsChildOf(targetContainer))
        {
            Debug.LogWarning(
                "[ICON BUTTON] Keep the source object and this button " +
                "outside the target container.",
                this
            );

            return;
        }

        GameObject copy = Instantiate(
            objectToCopy,
            targetContainer,
            false
        );

        copy.name = objectToCopy.name + "_Placed";

        // Prevent copied menu buttons from spawning additional icons.
        foreach (SpawnIconButton spawner in
                 copy.GetComponentsInChildren<SpawnIconButton>(true))
        {
            spawner.enabled = false;

            Button copiedButton = spawner.GetComponent<Button>();

            if (copiedButton != null)
            {
                copiedButton.onClick.RemoveListener(spawner.PlaceIcon);
            }
        }

        // This container must contain only placed icons.
        for (int i = targetContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = targetContainer.GetChild(i);

            if (child == copy.transform)
            {
                continue;
            }

            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        RectTransform rect = copy.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            rect.anchoredPosition3D = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            rect.sizeDelta = iconSize;
        }

        if (ignoreRaycasts)
        {
            foreach (Graphic graphic in
                     copy.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        copy.SetActive(true);

        LayoutRebuilder.MarkLayoutForRebuild(targetContainer);

        if (panelToClose != null &&
            panelToClose != targetContainer.gameObject &&
            !targetContainer.IsChildOf(panelToClose.transform))
        {
            panelToClose.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlaceIcon);
        }
    }
}