using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Components
{
    [Serializable]
    public class TabItem
    {
        public string tabId;
        public TabButtonUI tabButton; // Menggunakan TabButtonUI kustom
        public GameObject tabPanel;
    }

    [AddComponentMenu("UI/Components/Tab Controller")]
    public class TabController : MonoBehaviour
    {
        [SerializeField] private List<TabItem> tabs = new List<TabItem>();
        [SerializeField] private int defaultTabIndex = 0;

        private int currentTabIndex = -1;

        private void Start()
        {
            InitializeTabs();
            if (tabs.Count > 0 && defaultTabIndex < tabs.Count)
            {
                SelectTab(defaultTabIndex);
            }
        }

        private void InitializeTabs()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i;
                if (tabs[i].tabButton != null && tabs[i].tabButton.Button != null)
                {
                    tabs[i].tabButton.Button.onClick.AddListener(() => SelectTab(index));
                }
            }
        }

        public void SelectTab(int index)
        {
            if (index < 0 || index >= tabs.Count) return;

            currentTabIndex = index;

            for (int i = 0; i < tabs.Count; i++)
            {
                bool isActive = (i == currentTabIndex);

                // Aktifkan/Matikan Panel
                if (tabs[i].tabPanel != null)
                    tabs[i].tabPanel.SetActive(isActive);

                // Perbarui Visual Warna & Status Tombol
                if (tabs[i].tabButton != null)
                    tabs[i].tabButton.SetTabState(isActive);
            }
        }

        public void SelectTabById(string tabId)
        {
            int index = tabs.FindIndex(t => t.tabId.Equals(tabId, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) SelectTab(index);
        }
    }
}