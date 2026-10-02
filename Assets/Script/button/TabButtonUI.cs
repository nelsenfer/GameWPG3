using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Components
{
    [RequireComponent(typeof(Button))]
    public class TabButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image buttonImage;
        [SerializeField] private TextMeshProUGUI buttonText;

        [Header("Active State Colors")]
        [SerializeField] private Color activeBgColor = Color.white;
        [SerializeField] private Color activeTextColor = Color.black;

        [Header("Inactive State Colors")]
        [SerializeField] private Color inactiveBgColor = Color.gray;
        [SerializeField] private Color inactiveTextColor = Color.white;

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
        }

        public void SetTabState(bool isActive)
        {
            // Cukup matikan raycastTarget agar tombol yang sedang aktif tidak bisa diklik ulang, 
            // tanpa memicu efek 'Disabled Color' bawaan Unity Button
            if (buttonImage != null)
            {
                buttonImage.raycastTarget = !isActive;
                buttonImage.color = isActive ? activeBgColor : inactiveBgColor;
            }

            // Ubah warna Teks (jika ada)
            if (buttonText != null)
            {
                buttonText.color = isActive ? activeTextColor : inactiveTextColor;
            }
        }
    }
}