using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Wajib ditambahkan untuk mengakses komponen UI (Image/Text)
using System.Collections;

namespace UI.Components
{
    [AddComponentMenu("UI/Components/Hover Color Button")]
    public class HoverTransformButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Color Effects")]
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private float duration = 0.15f;

        private Graphic targetGraphic;
        private Color defaultColor;
        private Coroutine colorCoroutine;

        private void Awake()
        {
            // Mengambil komponen Image atau Text yang ada di GameObject ini
            targetGraphic = GetComponent<Graphic>();
            
            if (targetGraphic != null)
            {
                defaultColor = targetGraphic.color;
            }
            else
            {
                Debug.LogWarning("Tidak ada komponen Image/Text yang ditemukan di " + gameObject.name);
            }
        }

        private void OnEnable()
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = defaultColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetGraphic != null)
                StartColorAnimation(hoverColor);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (targetGraphic != null)
                StartColorAnimation(defaultColor);
        }

        private void StartColorAnimation(Color targetColor)
        {
            if (colorCoroutine != null) StopCoroutine(colorCoroutine);
            colorCoroutine = StartCoroutine(AnimateColor(targetColor));
        }

        private IEnumerator AnimateColor(Color targetColor)
        {
            Color startColor = targetGraphic.color;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                targetGraphic.color = Color.Lerp(startColor, targetColor, elapsed / duration);
                yield return null;
            }

            targetGraphic.color = targetColor;
        }
    }
}