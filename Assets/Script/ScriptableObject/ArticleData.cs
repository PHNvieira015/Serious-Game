using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(
    fileName = "NewArticle",
    menuName = "Articles/Article Data"
)]
public class ArticleData : ScriptableObject
{
    [Header("Article")]
    public string Title;

    public string Subtitle;

    [Header("Author")]
    public WriterData Writer;

    [Header("Blocks")]
    public List<ArticleBlock> Blocks = new List<ArticleBlock>();
}