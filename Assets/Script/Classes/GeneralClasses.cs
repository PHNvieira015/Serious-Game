using UnityEngine;

[System.Serializable]
public class WriterData
{
    public string WriterName;
    public Sprite WriterPhoto;

    [TextArea(2, 4)]
    public string WriterDescription;
}

public enum CheckType
{
    None,
    TrueCheck,
    LabelCheck,
    SourceCheck,
    AICheck,
    SpecialistCheck,
    FalacyCheck
}

public enum BlockType
{
    Null,
    Paragraph,
    Quote,
    Image,
    Claim
}

[System.Serializable]
public class ArticleBlock
{
    [Header("Block")]
    public BlockType Type;

    [TextArea(3, 10)]
    public string Text;

    public Sprite Image;

    [Header("Verification")]
    public CheckType CheckType;

    [TextArea(2, 3)]
    public string Hint;

    [TextArea(2, 5)]
    public string Explanation;
}