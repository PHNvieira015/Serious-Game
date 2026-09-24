using UnityEngine;
using DG.Tweening;

public class UI_ClickAnimation : MonoBehaviour
{
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void ClickAnimation()
    {
        transform.DOKill();

        transform.DOScale(
            originalScale * 0.90f,
            0.02f
        )
        .SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            transform.DOScale(
                originalScale,
                0.12f
            )
            .SetEase(Ease.OutBack);
        });
    }
}