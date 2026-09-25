using DG.Tweening;
using UnityEngine;

namespace ZonaDaVerdade.UI
{
    public class SidebarScreenTransition : MonoBehaviour
    {
        [Header("Referência")]
        [Tooltip("RectTransform da tela/painel que vai entrar e sair.")]
        [SerializeField] RectTransform screen;

        [Header("Animação")]
        [SerializeField] float duration = 0.35f;
        [SerializeField] Ease enterEase = Ease.OutCubic;
        [SerializeField] Ease exitEase = Ease.InCubic;

        [Header("Posição")]
        [Tooltip("Distância para fora da Sidebar. Normalmente use a largura da tela.")]
        [SerializeField] float slideDistance = 1920f;

        [Header("Opções")]
        [Tooltip("Se marcado, a tela começa escondida fora da Sidebar.")]
        [SerializeField] bool startHidden = true;

        [Tooltip("Desativa a interação enquanto a tela está animando.")]
        [SerializeField] bool blockInteractionDuringAnimation = true;

        RectTransform rect;
        Vector2 visiblePosition;
        Vector2 hiddenPosition;
        Tween currentTween;

        void Awake()
        {
            rect = screen != null ? screen : GetComponent<RectTransform>();

            visiblePosition = rect.anchoredPosition;

            hiddenPosition = visiblePosition + Vector2.left * slideDistance;

            if (startHidden)
            {
                rect.anchoredPosition = hiddenPosition;
            }
        }

        public void Enter()
        {
            KillCurrentTween();

            gameObject.SetActive(true);

            if (blockInteractionDuringAnimation)
                SetInteraction(false);

            currentTween = rect.DOAnchorPos(visiblePosition, duration)
                .SetEase(enterEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SetInteraction(true);
                });
        }

        public void Exit()
        {
            KillCurrentTween();

            if (blockInteractionDuringAnimation)
                SetInteraction(false);

            currentTween = rect.DOAnchorPos(hiddenPosition, duration)
                .SetEase(exitEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    SetInteraction(false);
                });
        }

        void KillCurrentTween()
        {
            currentTween?.Kill();
        }

        void SetInteraction(bool value)
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup != null)
            {
                canvasGroup.interactable = value;
                canvasGroup.blocksRaycasts = value;
            }
        }

        void OnDisable()
        {
            KillCurrentTween();
        }
    }
}