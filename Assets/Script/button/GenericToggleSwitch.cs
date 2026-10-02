using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

namespace UI.Components
{
    [RequireComponent(typeof(Toggle))]
    [AddComponentMenu("UI/Components/Generic Toggle Switch")]
    public class GenericToggleSwitch : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform handleTransform;
        [SerializeField] private Image bgImage;

        [Header("Visual Styles")]
        [SerializeField] private Vector2 offPosition = new Vector2(-20f, 0f);
        [SerializeField] private Vector2 onPosition = new Vector2(20f, 0f);
        [SerializeField] private Color offBgColor = Color.gray;
        [SerializeField] private Color onBgColor = Color.green;
        [SerializeField] private float transitionDuration = 0.2f;

        [Header("Events")]
        public UnityEvent<bool> onToggleChanged;

        private Toggle toggleComponent;
        private Coroutine animateCoroutine;

        private void Awake()
        {
            toggleComponent = GetComponent<Toggle>();
            toggleComponent.onValueChanged.AddListener(OnToggleValueUpdated);
        }

        private void Start()
        {
            UpdateVisualInstant(toggleComponent.isOn);
        }

        private void OnToggleValueUpdated(bool isOn)
        {
            if (animateCoroutine != null) StopCoroutine(animateCoroutine);
            animateCoroutine = StartCoroutine(AnimateToggle(isOn));

            onToggleChanged?.Invoke(isOn);
        }

        private IEnumerator AnimateToggle(bool isOn)
        {
            Vector2 startPos = handleTransform.anchoredPosition;
            Vector2 targetPos = isOn ? onPosition : offPosition;

            Color startColor = bgImage != null ? bgImage.color : Color.white;
            Color targetColor = isOn ? onBgColor : offBgColor;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                if (handleTransform != null)
                    handleTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                if (bgImage != null)
                    bgImage.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }
        }

        public void UpdateVisualInstant(bool isOn)
        {
            if (handleTransform != null) handleTransform.anchoredPosition = isOn ? onPosition : offPosition;
            if (bgImage != null) bgImage.color = isOn ? onBgColor : offBgColor;
        }

        private void OnDestroy()
        {
            if (toggleComponent != null)
                toggleComponent.onValueChanged.RemoveListener(OnToggleValueUpdated);
        }
    }
}