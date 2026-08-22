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
    True,
    Label, //serve as label check
    Source, //serve as date check
    AI, 
    Specialist, //serve as specialist check
    Falacy //not implemented
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
//    [EnumButtons]
    public BlockType Type;

    [TextArea(3, 10)]
    public string Text;

    public Sprite Image;

    [Header("Verification")]
//    [EnumButtons]
    public CheckType CheckType;

    [TextArea(2, 3)]
    public string Hint;

    [TextArea(2, 5)]
    public string Explanation;
}