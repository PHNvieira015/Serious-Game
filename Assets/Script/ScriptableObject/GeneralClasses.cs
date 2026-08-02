using UnityEngine;

[System.Serializable]
public class AuthorData
{
    public string NomeAutor;
    public Sprite FotoAutor;
    public string DescriçãoAutor;
}

public enum CheckType
{
    None,
    LabelCheck,
    SourceCheck,
    AICheck,
    SpecialistCheck,
    FalacyCheck
}

public enum BlockType
{
    Paragraph,
    Quote,
    Image,
    Claim
}

[System.Serializable]
public class ArticleBlock
{
    public BlockType Type;

    [TextArea(3, 10)]
    public string Text;

    public Sprite Image;

    // False information tracking
    public bool IsFalseInformation;
    [TextArea(2, 3)]
    public string Hint;

    // The correct answer
    public CheckType CorrectCheckType;

    // One explanation for EACH possible check type
    // (only one matches CorrectCheckType, but all 5 have an explanation)
    [Header("Explanations for each check type")]
    [TextArea(2, 4)]
    public string LabelCheckExplanation;

    [TextArea(2, 4)]
    public string SourceCheckExplanation;

    [TextArea(2, 4)]
    public string AICheckExplanation;

    [TextArea(2, 4)]
    public string SpecialistCheckExplanation;

    [TextArea(2, 4)]
    public string FalacyCheckExplanation;

    // Helper method
    public string GetExplanationForCheck(CheckType checkType)
    {
        switch (checkType)
        {
            case CheckType.LabelCheck: return LabelCheckExplanation;
            case CheckType.SourceCheck: return SourceCheckExplanation;
            case CheckType.AICheck: return AICheckExplanation;
            case CheckType.SpecialistCheck: return SpecialistCheckExplanation;
            case CheckType.FalacyCheck: return FalacyCheckExplanation;
            default: return "";
        }
    }
}