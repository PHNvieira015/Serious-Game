using DG.Tweening;
using UnityEngine;

namespace ZonaDaVerdade.UI
{
    public class SidebarScreenTransition : MonoBehaviour
    {
        [Header("Referência")]
        [SerializeField] RectTransform screen;

        [Header("Animação")]
        [SerializeField] float duration = 0.35f;

        [SerializeField] Ease enterEase = Ease.OutCubic;
        [SerializeField] Ease exitEase = Ease.InCubic;

        [Header("Posição")]
        [SerializeField] float slideDistance = 1920f;

        Tween currentTween;

        Vector2 visiblePosition;
        Vector2 hiddenPosition;

        [Header("Sons")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip enterSound;
        [SerializeField] AudioClip exitSound;

        [SerializeField][Range(0f, 1f)] float soundVolume = 0.5f;

        void Awake()
        {
            if (screen == null)
                screen = GetComponent<RectTransform>();

            visiblePosition = screen.anchoredPosition;

            hiddenPosition = visiblePosition + Vector2.left * slideDistance;

            screen.anchoredPosition = hiddenPosition;
        }

        public void Enter()
        {
            currentTween?.Kill();

            if (audioSource != null && enterSound != null)
            {
                audioSource.PlayOneShot(enterSound, soundVolume);
            }
            screen.gameObject.SetActive(true);

            // Garante que começa escondida
            screen.anchoredPosition = hiddenPosition;

            currentTween = screen
                .DOAnchorPos(visiblePosition, duration)
                .SetEase(enterEase)
                .SetUpdate(true);
        }

        public void Exit()
        {
            currentTween?.Kill();

            if (audioSource != null && enterSound != null)
            {
                audioSource.PlayOneShot(enterSound, soundVolume);
            }

            // Garante que começa da posição visível
            screen.anchoredPosition = visiblePosition;

            currentTween = screen
                .DOAnchorPos(hiddenPosition, duration)
                .SetEase(exitEase)
                .SetUpdate(true);
        }

        void OnDisable()
        {
            currentTween?.Kill();
        }
    }
}