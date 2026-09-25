using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZonaDaVerdade.UI
{
    /// <summary>
    /// Dá ênfase visual a um card (linha do Guia das Ferramentas, item de lista,
    /// etc.) quando o mouse passa por cima: escala suavemente pra cima e volta
    /// ao normal ao sair. Usa DOTween, sem alocar closures a cada hover.
    ///
    /// Requisitos na cena:
    /// - O Canvas precisa ter um Graphic Raycaster.
    /// - O próprio GameObject (ou algum filho) precisa ter um componente
    ///   Graphic (Image, por exemplo) com "Raycast Target" ligado, senão o
    ///   Unity nunca dispara OnPointerEnter/Exit aqui.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HoverScaleDOTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Escala")]
        [Tooltip("Multiplicador de escala ao passar o mouse. 1.05 = 5% maior.")]
        [SerializeField] float hoverScale = 1.05f;

        [Header("Timing")]
        [SerializeField] float duration = 0.18f;
        [SerializeField] Ease easeIn = Ease.OutBack;
        [SerializeField] Ease easeOut = Ease.OutQuad;

        [Tooltip("Se ligado, a animação roda mesmo com o jogo pausado (Time.timeScale = 0).")]
        [SerializeField] bool ignoreTimeScale = true;

        [Tooltip("Opcional: se preenchido, o card também é levemente elevado (útil se quiser dar profundidade junto com a escala).")]
        [SerializeField] float hoverYOffset = 0f;

        RectTransform rect;
        Vector3 baseScale;
        Vector2 baseAnchoredPos;
        Tween scaleTween;
        Tween moveTween;

        [Header("Som de Hover")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip hoverSound;
        [SerializeField][Range(0f, 1f)] float hoverVolume = 0.5f;

        void Awake()
        {
            rect = (RectTransform)transform;
            baseScale = rect.localScale;
            baseAnchoredPos = rect.anchoredPosition;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            KillTweens();

            if (audioSource != null && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound, hoverVolume);
            }

            scaleTween = rect.DOScale(baseScale * hoverScale, duration)
                             .SetEase(easeIn)
                             .SetUpdate(ignoreTimeScale)
                             .SetTarget(this);

            if (hoverYOffset != 0f)
            {
                moveTween = rect.DOAnchorPosY(baseAnchoredPos.y + hoverYOffset, duration)
                                .SetEase(easeIn)
                                .SetUpdate(ignoreTimeScale)
                                .SetTarget(this);
            }

            // Sobe o card na hierarquia de desenho pra ele não ficar
            // "atrás" dos vizinhos enquanto cresce.
            //transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            KillTweens();

            scaleTween = rect.DOScale(baseScale, duration)
                             .SetEase(easeOut)
                             .SetUpdate(ignoreTimeScale)
                             .SetTarget(this);

            if (hoverYOffset != 0f)
            {
                moveTween = rect.DOAnchorPosY(baseAnchoredPos.y, duration)
                                .SetEase(easeOut)
                                .SetUpdate(ignoreTimeScale)
                                .SetTarget(this);
            }
        }

        void KillTweens()
        {
            scaleTween?.Kill();
            moveTween?.Kill();
        }

        void OnDisable()
        {
            // Evita tween órfão se o card for desativado no meio da animação
            // (ex.: fechando o guia com o mouse ainda em cima de um item).
            KillTweens();
            rect.localScale = baseScale;
            rect.anchoredPosition = baseAnchoredPos;
        }
    }
}