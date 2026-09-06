using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArticleBlockView : MonoBehaviour
{
    [Header("Text / Image")]
    public TMP_Text blockText;
    public GameObject textContainer;
    public GameObject imageContainer;

    [Header("Selection")]
    public GameObject markedVisual;
    public TMP_Text selectedCheckTypeText;

    [Header("Block Component")]
    public Block blockComponent;

    [Header("Text Colors")]
    public Color defaultTextColor = new Color(1f, 1f, 1f, 1f);
    public Color trueColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    public Color labelColor = new Color(1f, 0.8f, 0.2f, 1f);
    public Color sourceColor = new Color(0.2f, 0.5f, 1f, 1f);
    public Color aiColor = new Color(0.2f, 0.9f, 0.9f, 1f);
    public Color specialistColor = new Color(0.8f, 0.2f, 0.8f, 1f);
    public Color falacyColor = new Color(1f, 0.2f, 0.2f, 1f);

    private ArticleBlock block;
    private bool isMarked;
    private CheckType playerCheckType = CheckType.None;

    public ArticleBlock Block => block;
    public bool IsMarked => isMarked;
    public CheckType PlayerCheckType => playerCheckType;

    private void Awake()
    {
        if (blockComponent == null)
            blockComponent = GetComponent<Block>();

        if (blockText == null)
            blockText = GetComponent<TMP_Text>();

        if (blockText != null)
        {
            blockText.color = defaultTextColor;
        }
    }

    public void Initialize(ArticleBlock articleBlock)
    {
        block = articleBlock;

        if (block == null)
        {
            Debug.LogError("ArticleBlockView: ArticleBlock is null on " + gameObject.name);
            return;
        }

        if (blockComponent != null)
        {
            blockComponent.Initialize(block);
        }

        DisplayBlock();
        ClearPlayerSelection();
    }

    private void DisplayBlock()
    {
        if (block == null || blockText == null)
            return;

        blockText.text = block.Text;
        blockText.ForceMeshUpdate();

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    public void SetMarked(bool value)
    {
        isMarked = value;

        if (markedVisual != null)
            markedVisual.SetActive(value);
    }

    public void SetPlayerCheckType(CheckType type)
    {
        if (type == CheckType.None)
            return;

        playerCheckType = type;
        isMarked = true;

        if (markedVisual != null)
            markedVisual.SetActive(true);

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = GetCheckTypeName(type);
            selectedCheckTypeText.gameObject.SetActive(true);
        }

        SetTextColor(type);
    }

    public void SetMarkColorFromDraggable(CheckType type)
    {
        playerCheckType = type;
        isMarked = true;

        if (markedVisual != null)
            markedVisual.SetActive(true);

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = GetCheckTypeName(type);
            selectedCheckTypeText.gameObject.SetActive(true);
        }

        SetTextColor(type);
    }

    private void SetTextColor(CheckType type)
    {
        if (blockText != null)
        {
            blockText.color = GetColorForType(type);
            Debug.Log("SetTextColor: " + type + " -> " + GetColorForType(type));
        }
        else
        {
            Debug.LogWarning("blockText is null on " + gameObject.name);
        }
    }

    public void ClearPlayerSelection()
    {
        isMarked = false;
        playerCheckType = CheckType.None;

        if (markedVisual != null)
            markedVisual.SetActive(false);

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = string.Empty;
            selectedCheckTypeText.gameObject.SetActive(false);
        }

        if (blockText != null)
        {
            blockText.color = defaultTextColor;
        }
    }

    private Color GetColorForType(CheckType type)
    {
        switch (type)
        {
            case CheckType.True:
                return trueColor;
            case CheckType.Label:
                return labelColor;
            case CheckType.Source:
                return sourceColor;
            case CheckType.AI:
                return aiColor;
            case CheckType.Specialist:
                return specialistColor;
            case CheckType.Falacy:
                return falacyColor;
            case CheckType.None:
            default:
                return defaultTextColor;
        }
    }

    private string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.True: return "True Check";
            case CheckType.Label: return "Label Check";
            case CheckType.Source: return "Source Check";
            case CheckType.AI: return "AI Check";
            case CheckType.Specialist: return "Specialist Check";
            case CheckType.Falacy: return "Fallacy Check";
            case CheckType.None:
            default: return "None";
        }
    }
}