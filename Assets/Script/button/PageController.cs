using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PageController : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] private List<GameObject> pages = new List<GameObject>();
    [SerializeField] private bool loopPages = false;

    [Header("Events")]
    public UnityEvent<int, int> onPageChanged; // (currentPageIndex, totalPages)

    private int _currentPage = 0;

    public int CurrentPage => _currentPage;
    public int TotalPages => pages.Count;

    private void OnEnable()
    {
        _currentPage = 0;
        UpdatePages();
    }

    public void NextPage()
    {
        if (pages.Count == 0) return;

        if (_currentPage < pages.Count - 1)
        {
            _currentPage++;
            UpdatePages();
        }
        else if (loopPages)
        {
            _currentPage = 0;
            UpdatePages();
        }
    }

    public void PrevPage()
    {
        if (pages.Count == 0) return;

        if (_currentPage > 0)
        {
            _currentPage--;
            UpdatePages();
        }
        else if (loopPages)
        {
            _currentPage = pages.Count - 1;
            UpdatePages();
        }
    }

    private void UpdatePages()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i] != null)
                pages[i].SetActive(i == _currentPage);
        }

        onPageChanged?.Invoke(_currentPage, pages.Count);
    }
}