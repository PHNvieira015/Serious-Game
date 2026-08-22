using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArticleBlockView : MonoBehaviour
{
    [Header("Text / Image")]
    public TMP_Text blockText;
    public Image blockImage;
    public GameObject textContainer;
    public GameObject imageContainer;

    [Header("Selection")]
    public GameObject markedVisual;
    public TMP_Text selectedCheckTypeText;

    [Header("Block Component")]
    public Block blockComponent;

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
    }

    public void Initialize(ArticleBlock articleBlock)
    {
        block = articleBlock;

        if (block == null)
        {
            Debug.LogError($"ArticleBlockView: ArticleBlock is null on {gameObject.name}");
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
        if (block == null)
            return;

        if (block.Type == BlockType.Image && block.Image != null)
        {
            if (textContainer != null)
                textContainer.SetActive(false);

            if (imageContainer != null)
                imageContainer.SetActive(true);

            if (blockImage != null)
                blockImage.sprite = block.Image;

            return;
        }

        if (imageContainer != null)
            imageContainer.SetActive(false);

        if (textContainer != null)
            textContainer.SetActive(true);

        if (blockText != null)
        {
            blockText.text = block.Text;
            blockText.ForceMeshUpdate();
        }
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
        SetMarked(true);

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text = GetCheckTypeName(type);
            selectedCheckTypeText.gameObject.SetActive(true);
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