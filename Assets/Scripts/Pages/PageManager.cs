using System.Collections.Generic;
using UnityEngine;

public class PageManager : MonoBehaviour
{
    public class HomePageEntry
    {
        public string DisplayName;
        public KMSelectable PageSelectable;
        public Texture2D Icon;
        public KMHoldable.HoldableAvailabilityEnum Availability;

        public bool Enabled
        {
            get
            {
                return Availability == KMHoldable.HoldableAvailabilityEnum.ALL ||
                       Availability == KMHoldable.HoldableAvailabilityEnum.SETUP &&
                       CurrentState == KMGameInfo.State.Setup ||
                       Availability == KMHoldable.HoldableAvailabilityEnum.GAMEPLAY &&
                       CurrentState == KMGameInfo.State.Gameplay;
            }
        }
    }

    public static KMGameInfo.State CurrentState = KMGameInfo.State.Setup;

    public Transform RootTransform = null;

    public KMSelectable this[string pageName]
    {
        get
        {
            KMSelectable page = null;
            if (Pages.TryGetValue(pageName, out page))
            {
                return page;
            }

            KMSelectable pagePrefab = null;
            if (PagePrefabs.TryGetValue(pageName, out pagePrefab))
            {
                page = Instantiate<KMSelectable>(pagePrefab, RootTransform, false);
                page.transform.localPosition = Vector3.zero;
                page.gameObject.SetActive(false);
                Pages[pageName] = page;
                return page;
            }

            return null;
        }
    }

    public static IEnumerable<HomePageEntry> HomePageEntries
    {
        get
        {
            return HomePageEntryList;
        }
    }

    private static readonly Dictionary<string, KMSelectable> PagePrefabs = new Dictionary<string, KMSelectable>();
    private static readonly List<HomePageEntry> HomePageEntryList = new List<HomePageEntry>();

    private readonly Dictionary<string, KMSelectable> Pages = new Dictionary<string, KMSelectable>();

    public static void AddPagePrefabs(KMSelectable[] pageSelectables)
    {
        foreach (KMSelectable pageSelectable in pageSelectables)
        {
            AddPagePrefab(pageSelectable);
        }
    }

    public static void AddPagePrefab(KMSelectable pageSelectable)
    {
        AddPagePrefab(pageSelectable.name, pageSelectable);
    }

    public static void AddPagePrefab(string name, KMSelectable pageSelectable)
    {
        if (PagePrefabs.ContainsKey(name))
        {
            return;
        }

        PagePrefabs[name] = pageSelectable;
        pageSelectable.EnsureModSelectable();
        pageSelectable.Reproxy();
    }

    public static void AddHomePageEntry(string displayName, KMSelectable pageSelectable, Texture2D icon)
    {
        HomePageEntryList.Add(new HomePageEntry() { DisplayName = displayName, PageSelectable = pageSelectable, Icon = icon, Availability = KMHoldable.HoldableAvailabilityEnum.SETUP});
    }

    public static void AddRoomHomePageEntry(string displayName, KMSelectable pageSelectable, Texture2D icon, KMHoldable.HoldableAvailabilityEnum availability)
    {
        HomePageEntryList.Add(new HomePageEntry() { DisplayName = displayName, PageSelectable = pageSelectable, Icon = icon, Availability = availability});
    }

    public void DestroyCachedPages()
    {
        foreach (KMSelectable page in Pages.Values)
        {
            DestroyObject(page.gameObject);
        }

        Pages.Clear();
    }
}
