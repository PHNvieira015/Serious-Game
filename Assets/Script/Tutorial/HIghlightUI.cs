using UnityEngine;
using System.Collections;


public class HighlightUI : MonoBehaviour
{
    [SerializeField] private RectTransform highlight;
    [SerializeField] private Canvas canvas;

    public IEnumerator Show(RectTransform target, float duration)
    {
        highlight.gameObject.SetActive(true);

        // Position around target
        highlight.position = target.position;
        highlight.sizeDelta = target.rect.size + new Vector2(30, 30);

        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            // Example pulsing effect
            float pulse = 1f + Mathf.Sin(time * 5f) * 0.05f;
            highlight.localScale = Vector3.one * pulse;

            yield return null;
        }

        highlight.gameObject.SetActive(false);
    }
}
