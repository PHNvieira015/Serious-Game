using UnityEngine;
using DG.Tweening;
public class TesteDOTween : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.DOScale(1.2f, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
