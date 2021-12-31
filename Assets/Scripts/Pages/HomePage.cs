using System.Collections;
using System.Linq;
using UnityEngine;

public class HomePage : MonoBehaviour
{
    private Page _page = null;
    private PageLink[] _pageLinks = null;

    private PageManager.HomePageEntry[] _homePageEntries = null;

    private void Awake()
    {
        _page = GetComponent<Page>();
        _pageLinks = GetComponentsInChildren<PageLink>(true);
    }

    private void OnEnable()
    {
        StartCoroutine(DisplayPages());
    }
    
    private IEnumerator DisplayPages()
    {
        yield return new WaitUntil(() => PageManager.CurrentState != KMGameInfo.State.Transitioning);
        
        _homePageEntries = PageManager.HomePageEntries.Where(page => page.Enabled).ToArray();

        for (int pageLinkIndex = 0; pageLinkIndex < _pageLinks.Length; ++pageLinkIndex)
        {
            PageLink pageLink = _pageLinks[pageLinkIndex];

            if (pageLinkIndex >= _homePageEntries.Length)
            {
                pageLink.gameObject.SetActive(false);
                continue;
            }

            PageManager.HomePageEntry homePageEntry = _homePageEntries[pageLinkIndex];

            pageLink.gameObject.SetActive(true);

            UIElement element = pageLink.GetComponent<UIElement>();
            element.Text = homePageEntry.DisplayName;
            element.Icon = homePageEntry.Icon;
            pageLink.PagePrefab = homePageEntry.PageSelectable;
        }

        if(_homePageEntries.Length == 0)
            Destroy(ModSelectorTablet.Instance.gameObject);
    }
}
