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
    public Button markButton;
    public GameObject markedVisual;

    [Tooltip("Optional text showing the player's selected check.")]
    public TMP_Text selectedCheckTypeText;

    private ArticleBlock block;
    private ArticleViewer articleViewer;

    private bool isMarked;
    private CheckType playerCheckType = CheckType.None;

    public ArticleBlock Block => block;
    public bool IsMarked => isMarked;
    public CheckType PlayerCheckType => playerCheckType;

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public void Initialize(
        ArticleBlock articleBlock,
        ArticleViewer viewer)
    {
        block = articleBlock;
        articleViewer = viewer;

        if (markButton != null)
        {
            markButton.onClick.RemoveAllListeners();
            markButton.onClick.AddListener(OnMarkButtonClicked);
        }

        DisplayBlock();
        ClearPlayerSelection();
    }

    // ============================================================
    // DISPLAY
    // ============================================================

    private void DisplayBlock()
    {
        if (block == null)
            return;

        // --------------------------------------------------------
        // IMAGE BLOCK
        // --------------------------------------------------------

        if (block.Type == BlockType.Image &&
            block.Image != null)
        {
            if (textContainer != null)
                textContainer.SetActive(false);

            if (imageContainer != null)
                imageContainer.SetActive(true);

            if (blockImage != null)
                blockImage.sprite = block.Image;

            return;
        }

        // --------------------------------------------------------
        // TEXT / OTHER BLOCK TYPES
        // --------------------------------------------------------

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

    // ============================================================
    // MARK BLOCK
    // ============================================================

    private void OnMarkButtonClicked()
    {
        if (articleViewer == null)
            return;

        articleViewer.SelectBlock(this);
    }

    public void SetMarked(bool value)
    {
        isMarked = value;

        if (markedVisual != null)
            markedVisual.SetActive(value);
    }

    // ============================================================
    // PLAYER CHECK TYPE
    // ============================================================

    public void SetPlayerCheckType(CheckType type)
    {
        // None is not a valid player selection.
        if (type == CheckType.None)
            return;

        playerCheckType = type;

        // Choosing a check means the player marked this block.
        SetMarked(true);

        if (selectedCheckTypeText != null)
        {
            selectedCheckTypeText.text =
                GetCheckTypeName(type);

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

    // ============================================================
    // CHECK TYPE NAME
    // ============================================================

    private string GetCheckTypeName(CheckType type)
    {
        switch (type)
        {
            case CheckType.TrueCheck:
                return "True Check";

            case CheckType.LabelCheck:
                return "Label Check";

            case CheckType.SourceCheck:
                return "Source Check";

            case CheckType.AICheck:
                return "AI Check";

            case CheckType.SpecialistCheck:
                return "Specialist Check";

            case CheckType.FalacyCheck:
                return "Fallacy Check";

            case CheckType.None:
            default:
                return "None";
        }
    }
}