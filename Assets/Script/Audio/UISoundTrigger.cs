using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Components
{
    [AddComponentMenu("UI/Components/UI Sound Trigger")]
    public class UISoundTrigger : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Audio Settings")]
        [SerializeField] private string clickSfxId = "sfx_ui_click";
        [SerializeField] private string hoverEnterSfxId = "sfx_ui_hover";
        [SerializeField] private string hoverExitSfxId = "";

        [Header("Options")]
        [SerializeField] private bool playClickSound = true;
        [SerializeField] private bool playHoverEnterSound = false;
        [SerializeField] private bool playHoverExitSound = false;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (playClickSound && !string.IsNullOrEmpty(clickSfxId))
            {
                TriggerSfx(clickSfxId);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (playHoverEnterSound && !string.IsNullOrEmpty(hoverEnterSfxId))
            {
                TriggerSfx(hoverEnterSfxId);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (playHoverExitSound && !string.IsNullOrEmpty(hoverExitSfxId))
            {
                TriggerSfx(hoverExitSfxId);
            }
        }

        private void TriggerSfx(string sfxId)
        {
            // Panggilan generik ke AudioManager jika ada di scene
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(sfxId);
            }
            else
            {
                Debug.LogWarning($"[UISoundTrigger] AudioManager instance tidak ditemukan untuk memutar SFX ID: {sfxId}");
            }
        }
    }
}