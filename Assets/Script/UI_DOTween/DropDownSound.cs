using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

namespace ZonaDaVerdade.UI
{
    public class DropdownSound : MonoBehaviour, IPointerClickHandler
    {
        [Header("Áudio")]
        [SerializeField] AudioSource audioSource;

        [Header("Som ao abrir")]
        [SerializeField] AudioClip openSound;

        [Header("Som ao selecionar")]
        [SerializeField] AudioClip selectSound;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] float volume = 0.5f;

        TMP_Dropdown dropdown;

        void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
        }

        void OnEnable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(PlaySelectSound);
        }

        void OnDisable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(PlaySelectSound);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (audioSource != null && openSound != null)
            {
                audioSource.PlayOneShot(openSound, volume);
            }
        }

        void PlaySelectSound(int value)
        {
            if (audioSource != null && selectSound != null)
            {
                audioSource.PlayOneShot(selectSound, volume);
            }
        }
    }
}