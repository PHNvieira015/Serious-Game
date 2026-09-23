using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class BlockTypeDropdown : MonoBehaviour
{
    [System.Serializable]
    public class CheckOption
    {
        public string label;
        public Sprite icon;

        [Tooltip("The original sidebar button's DraggableObject.")]
        public DraggableObject iconSource;
    }

    [Header("References")]
    public TMP_Dropdown dropdown;

    [Tooltip("The block this dropdown will mark.")]
    public Block block;

    [Header("Dropdown Options")]
    public string placeholderText = "Select check...";

    public List<CheckOption> options =
        new List<CheckOption>();

    [Header("Debug")]
    public bool showDebugMessages = true;

    private readonly List<DraggableObject> optionSources =
        new List<DraggableObject>();

    private void Awake()
    {
        CacheReferences();
        BuildOptions();

        dropdown.onValueChanged.AddListener(
            OnOptionSelected
        );
    }

    private void CacheReferences()
    {
        if (dropdown == null)
        {
            dropdown = GetComponent<TMP_Dropdown>();
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

    private void BuildOptions()
    {
        optionSources.Clear();

        List<TMP_Dropdown.OptionData> entries =
            new List<TMP_Dropdown.OptionData>();

        entries.Add(
            new TMP_Dropdown.OptionData
            {
                text = placeholderText,
                image = null,
                color = Color.white
            }
        );

        foreach (CheckOption option in options)
        {
            if (option == null)
            {
                continue;
            }

            if (option.iconSource == null)
            {
                Debug.LogWarning(
                    "[DROPDOWN] Option has no Icon Source: " +
                    option.label,
                    this
                );

                continue;
            }

            string label = option.label;

            if (string.IsNullOrWhiteSpace(label))
            {
                label =
                    option.iconSource.CheckType.ToString();
            }

            entries.Add(
                new TMP_Dropdown.OptionData
                {
                    text = label,
                    image = option.icon,
                    color = Color.white
                }
            );

            optionSources.Add(option.iconSource);
        }

        dropdown.ClearOptions();
        dropdown.AddOptions(entries);

        ResetDropdown();
    }

    private void OnOptionSelected(int index)
    {
        // The first option is only the placeholder.
        if (index <= 0)
        {
            return;
        }

        CacheReferences();

        int sourceIndex = index - 1;

        if (block == null)
        {
            Debug.LogWarning(
                "[DROPDOWN] Assign the target Block on " +
                gameObject.name,
                this
            );

            ResetDropdown();
            return;
        }

        if (sourceIndex < 0 ||
            sourceIndex >= optionSources.Count)
        {
            ResetDropdown();
            return;
        }

        DraggableObject source =
            optionSources[sourceIndex];

        if (source == null)
        {
            Debug.LogWarning(
                "[DROPDOWN] The selected Icon Source is missing.",
                this
            );

            ResetDropdown();
            return;
        }

        CheckType selectedType = source.CheckType;

        // Selecting the current mark does not spend another check.
        if (block.CurrentDraggable != null &&
            block.CurrentDraggable.CheckType == selectedType)
        {
            ButtonClickMarker.ClearSelection();
            ResetDropdown();
            return;
        }

        // Use the existing placement logic to create the copy,
        // update the block and position the icon in its container.
        bool placed = source.TryPlaceCopyOnBlock(
            block,
            selectedType
        );

        if (placed)
        {
            ButtonClickMarker.ClearSelection();

            if (showDebugMessages)
            {
                Debug.Log(
                    "[DROPDOWN] Placed " +
                    selectedType +
                    " on " +
                    block.name,
                    this
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[DROPDOWN] Could not place " +
                selectedType +
                " on " +
                block.name,
                this
            );
        }

        ResetDropdown();
    }

    private void ResetDropdown()
    {
        if (dropdown == null)
        {
            return;
        }

        dropdown.SetValueWithoutNotify(0);
        dropdown.RefreshShownValue();
    }

    private void OnDisable()
    {
        if (dropdown != null)
        {
            dropdown.Hide();
        }

        ResetDropdown();
    }

    private void OnDestroy()
    {
        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(
                OnOptionSelected
            );
        }
    }
}