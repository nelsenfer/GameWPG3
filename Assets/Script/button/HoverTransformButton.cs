using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace UI.Components
{
    [AddComponentMenu("UI/Components/Hover Transform Button")]
    public class HoverTransformButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Transform Effects")]
        [SerializeField] private Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private float duration = 0.15f;

        private Vector3 defaultScale;
        private Coroutine scaleCoroutine;

        private void Awake()
        {
            defaultScale = transform.localScale;
        }

        private void OnEnable()
        {
            transform.localScale = defaultScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            StartScaleAnimation(hoverScale);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            StartScaleAnimation(defaultScale);
        }

        private void StartScaleAnimation(Vector3 targetScale)
        {
            if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(AnimateScale(targetScale));
        }

        private IEnumerator AnimateScale(Vector3 targetScale)
        {
            Vector3 startScale = transform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = targetScale;
        }
    }
}