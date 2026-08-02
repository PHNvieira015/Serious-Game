using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "FakeNews/Article")]
public class ArticleData : ScriptableObject
{
    public string Titulo;
    public string Subtitulo;

    public AuthorData Autor;

    public List<ArticleBlock> Bloco;
}
