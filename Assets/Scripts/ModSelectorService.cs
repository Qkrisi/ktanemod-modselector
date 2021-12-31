using UnityEngine;
using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

public class ModSelectorService : MonoBehaviour
{
    #region Nested Types
    public sealed class ModWrapper
    {
        static ModWrapper()
        {
            ModType = ReflectionHelper.FindTypeInGame("Mod");
            TitleProperty = ModType.GetProperty("Title", BindingFlags.Instance | BindingFlags.Public);
            ModObjectsProperty = ModType.GetProperty("ModObjects", BindingFlags.Instance | BindingFlags.Public);

            ModDirectoryField = ModType.GetField("modDirectory", BindingFlags.Instance | BindingFlags.NonPublic);
            UnityVersionField = ModType.GetField("modUnityVersion", BindingFlags.Instance | BindingFlags.NonPublic);

            BombType = ReflectionHelper.FindTypeInGame("ModBomb");
            WidgetType = ReflectionHelper.FindTypeInGame("ModWidget");
            GameplayRoomType = ReflectionHelper.FindTypeInGame("ModGameplayRoom");
        }

        public ModWrapper(object modObject)
        {
            ModObject = modObject;

            ModName = (string)TitleProperty.GetValue(ModObject, null);
            ModDirectory = (string)ModDirectoryField.GetValue(ModObject);
            UnityVersion = (string)UnityVersionField.GetValue(ModObject);
            ModObjects = (List<GameObject>)ModObjectsProperty.GetValue(ModObject, null);

            try
            {
                string modInfoText = File.ReadAllText(Path.Combine(ModDirectory, "modInfo.json"));
                if (modInfoText != null)
                {
                    Dictionary<string, object> modInfoDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(modInfoText);
                    ModTitle = (string)modInfoDictionary["title"];
                    if (ModTitle == null)
                    {
                        ModTitle = ModName;
                    }

                    ModVersion = (string)modInfoDictionary["version"];
                }
                else
                {
                    ModTitle = ModName;
                    ModVersion = null;
                }
            }
            catch (Exception ex)
            {
                ModTitle = ModName;
                ModVersion = null;
            }

            _activeModObjects = ModObjects;
            _inactiveModObjects = new List<GameObject>();

            Debug.LogFormat("[ModSelector] Found mod '{0}', which contains the following {1} mod object(s):", ModName, _activeModObjects.Count);
            foreach (GameObject gameObject in _activeModObjects)
            {
                Debug.LogFormat("  {0}", gameObject.name);
            }
            Debug.Log(" ");
        }

        public GameObject GetModObject(string modObjectName)
        {
            foreach (GameObject modObject in _activeModObjects)
            {
                if (modObject.name.Equals(modObjectName))
                {
                    return modObject;
                }
            }

            foreach (GameObject modObject in _inactiveModObjects)
            {
                if (modObject.name.Equals(modObjectName))
                {
                    return modObject;
                }
            }

            return null;
        }

        public IEnumerable<KeyValuePair<GameObject, bool>> GetModObjects(Type modType)
        {
            foreach (GameObject modObject in _activeModObjects)
            {
                if (modObject.GetComponent(modType) != null)
                {
                    yield return new KeyValuePair<GameObject, bool>(modObject, true);
                }
            }

            foreach (GameObject modObject in _inactiveModObjects)
            {
                if (modObject.GetComponent(modType) != null)
                {
                    yield return new KeyValuePair<GameObject, bool>(modObject, false);
                }
            }
        }

        public bool EnableModObject(Type modType, string modObjectName)
        {
            KeyValuePair<GameObject, bool> modObject = GetModObjects(modType).Where((x) => x.Value == false).Where((x) => x.Key.name.Equals(modObjectName)).FirstOrDefault();
            if (modObject.Key != null)
            {
                _inactiveModObjects.Remove(modObject.Key);
                _activeModObjects.Add(modObject.Key);
                return true;
            }

            return false;
        }

        public bool DisableModObject(Type modType, string modObjectName)
        {
            KeyValuePair<GameObject, bool> modObject = GetModObjects(modType).Where((x) => x.Value == true).Where((x) => x.Key.name.Equals(modObjectName)).FirstOrDefault();
            if (modObject.Key != null)
            {
                _activeModObjects.Remove(modObject.Key);
                _inactiveModObjects.Add(modObject.Key);
                return true;
            }

            return false;
        }

        public void EnableModObjects(Type modType)
        {
            foreach (KeyValuePair<GameObject, bool> modObject in GetModObjects(modType).ToArray())
            {
                if (!modObject.Value)
                {
                    _inactiveModObjects.Remove(modObject.Key);
                    _activeModObjects.Add(modObject.Key);
                }
            }
        }

        public void DisableModObjects(Type modType)
        {
            foreach (KeyValuePair<GameObject, bool> modObject in GetModObjects(modType).ToArray())
            {
                if (!modObject.Value)
                {
                    _activeModObjects.Remove(modObject.Key);
                    _inactiveModObjects.Add(modObject.Key);
                }
            }
        }

        public static readonly Type ModType = null;
        public static readonly PropertyInfo TitleProperty = null;
        public static readonly PropertyInfo ModObjectsProperty = null;
        public static readonly FieldInfo ModDirectoryField = null;
        public static readonly FieldInfo UnityVersionField = null;

        public static readonly Type BombType = null;
        public static readonly Type WidgetType = null;
        public static readonly Type GameplayRoomType = null;

        public readonly object ModObject;
        public readonly string ModName;
        public readonly string ModTitle;
        public readonly string ModVersion;
        public readonly string ModDirectory;
        public readonly string UnityVersion;
        public readonly List<GameObject> ModObjects;

        private readonly List<GameObject> _activeModObjects;
        private readonly List<GameObject> _inactiveModObjects;
    }

    public interface Module
    {
        string ModuleName
        {
            get;
        }

        string ModuleType
        {
            get;
        }
    }

    public sealed class SolvableModule : Module
    {
        public SolvableModule(KMBombModule solvableBombModule, object component)
        {
            SolvableBombModule = solvableBombModule;
            Component = component;
        }

        public readonly KMBombModule SolvableBombModule;
        public readonly object Component;

        public string ModuleName
        {
            get
            {
                return SolvableBombModule.ModuleDisplayName;
            }
        }

        public string ModuleType
        {
            get
            {
                return SolvableBombModule.ModuleType;
            }
        }
    }

    public sealed class NeedyModule : Module
    {
        public NeedyModule(KMNeedyModule needyBombModule, object component)
        {
            NeedyBombModule = needyBombModule;
            Component = component;
        }

        public readonly KMNeedyModule NeedyBombModule;
        public readonly object Component;

        public string ModuleName
        {
            get
            {
                return NeedyBombModule.ModuleDisplayName;
            }
        }

        public string ModuleType
        {
            get
            {
                return NeedyBombModule.ModuleType;
            }
        }
    }

    public sealed class Service
    {
        public Service(KMService service)
        {
            ServiceObject = service.gameObject;
        }

        public readonly GameObject ServiceObject;

        public string ServiceName
        {
            get
            {
                return ServiceObject.name;
            }
        }

        public bool IsEnabled
        {
            get
            {
                return ServiceObject.activeSelf;
            }
            set
            {
                ServiceObject.SetActive(value);
            }
        }
    }

    public enum ModType
    {
        [Description("Solvable Modules")]
        SolvableModule,

        [Description("Needy Modules")]
        NeedyModule,

        [Description("Bombs")]
        Bomb,

        [Description("Widgets")]
        Widget,

        [Description("Gameplay Rooms")]
        GameplayRoom,

        [Description("Services")]
        Service,

        [Description("Unknown")]
        Unknown
    }
    #endregion

    public KMSelectable[] PagePrefabs = null;

    public KMSelectable ProfilesMainPrefab = null;
    public Texture2D ProfilesMainIcon = null;

    public KMSelectable ModsMainPrefab = null;
    public Texture2D ModsMainIcon = null;

    private APIProperties _properties = null;

    #region Unity Lifecycle
    private void Awake()
    {
        _properties = GetComponentInChildren<APIProperties>();
        _properties.Add("AddPageMethod", () => (Action<KMSelectable>)PageManager.AddPagePrefab, null);
        _properties.Add("AddPagesMethod", () => (Action<KMSelectable[]>)PageManager.AddPagePrefabs, null);
        _properties.Add("AddHomePageMethod", () => (Action<string, KMSelectable, Texture2D>)PageManager.AddHomePageEntry, null);
        _properties.Add("AddRoomHomePageMethod", () => (Action<string, KMSelectable, Texture2D, KMHoldable.HoldableAvailabilityEnum>)PageManager.AddRoomHomePageEntry, null);
        _properties.Add("GoToPageMethod", () => (Action<string>)FindObjectOfType<PageNavigation>().GoToPage, null);
        _properties.Add("GoBackMethod", () => (Action)FindObjectOfType<PageNavigation>().GoBack, null);

        _properties.Add("AllMods", () => GetAllModNames(), null);
        _properties.Add("AllSolvableModules", () => GetModNames(ModType.SolvableModule), null);
        _properties.Add("AllNeedyModules", () => GetModNames(ModType.NeedyModule), null);
        _properties.Add("AllBombs", () => GetModNames(ModType.Bomb), null);
        _properties.Add("AllWidgets", () => GetModNames(ModType.Widget), null);
        _properties.Add("AllGameplayRooms", () => GetModNames(ModType.GameplayRoom), null);
        _properties.Add("AllServices", () => GetModNames(ModType.Service), null);

        _properties.Add("GetModuleDisplayNameMethod", () => (Func<string, string>)GetModuleDisplayName, null);

        _properties.Add("DisabledMods", () => ProfileManager.ActiveDisableSet, null);
        _properties.Add("DisabledSolvableModules", () => ProfileManager.GetActiveDisableList(ModType.SolvableModule), null);
        _properties.Add("DisabledNeedyModules", () => ProfileManager.GetActiveDisableList(ModType.NeedyModule), null);
        _properties.Add("DisabledBombs", () => ProfileManager.GetActiveDisableList(ModType.Bomb), null);
        _properties.Add("DisabledWidgets", () => ProfileManager.GetActiveDisableList(ModType.Widget), null);
        _properties.Add("DisabledGameplayRooms", () => ProfileManager.GetActiveDisableList(ModType.GameplayRoom), null);
        _properties.Add("DisabledServices", () => ProfileManager.GetActiveDisableList(ModType.Service), null);

        //TOOD: The above is a little nasty, but not too bad yet. If there's more methods to expose, improve this!

        PageManager.AddPagePrefabs(PagePrefabs);

        PageManager.AddHomePageEntry("Profiles", ProfilesMainPrefab, ProfilesMainIcon);
        PageManager.AddHomePageEntry("Mods Info", ModsMainPrefab, ModsMainIcon);

        KMGameInfo gameInfo = GetComponent<KMGameInfo>();
        gameInfo.OnStateChange += OnStateChange;
    }

    private void Start()
    {
        _instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (!_updateRequired && SceneManagerWatcher.CurrentState == SceneManagerWatcher.State.ModManager)
        {
            //Clear out the mod info, in preparation for new info when coming back from the mod manager
            ClearModInfo();

            _updateRequired = true;
        }
    }
    #endregion

    #region Setup
    private void OnStateChange(KMGameInfo.State state)
    {
        PageManager.CurrentState = state;
        if (_updateRequired && state == KMGameInfo.State.Setup)
        {
            //Update the mod info
            SetupModInfo();

            //Reload the active configuration
            ProfileManager.ReloadActiveConfiguration();

            _updateRequired = false;
        }
    }

    private void ClearModInfo()
    {
        //Ensure to re-enable everything back first!
        EnableAll();
        _activeModules = null;

        _allSolvableModules.Clear();
        _allNeedyModules.Clear();

        _allServices.Clear();
        _allMods.Clear();
    }

    private void SetupModInfo()
    {
        //For modules
        GetSolvableModules();
        GetNeedyModules();
        GetActiveModules();

        //For services
        GetModServices();

        //For all other mod types
        GetModList();

        _allExpertMods.UnionWith(GetModNames(ModType.SolvableModule));
        _allExpertMods.UnionWith(GetModNames(ModType.NeedyModule));
        _allExpertMods.UnionWith(GetModNames(ModType.Widget));
    }

    private void GetSolvableModules()
    {
        UnityEngine.Object modManager = ModManager;

        MethodInfo getSolvableBombModulesMethod = _modManagerType.GetMethod("GetSolvableBombModules", BindingFlags.Instance | BindingFlags.Public);
        IList solvableBombModuleList = getSolvableBombModulesMethod.Invoke(modManager, null) as IList;

        Type modBombComponentType = ReflectionHelper.FindTypeInGame("ModBombComponent");
        FieldInfo moduleField = modBombComponentType.GetField("module", BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (object solvableBombModule in solvableBombModuleList)
        {
            KMBombModule module = moduleField.GetValue(solvableBombModule) as KMBombModule;
            string moduleTypeName = module.ModuleType;

            if (!_allSolvableModules.ContainsKey(moduleTypeName))
            {
                _allSolvableModules[moduleTypeName] = new SolvableModule(module, solvableBombModule);
            }
            else
            {
                Debug.LogErrorFormat("***** A duplicate regular/solvable module was found under the name {0}! *****", moduleTypeName);
            }
        }
    }

    private void GetNeedyModules()
    {
        UnityEngine.Object modManager = ModManager;

        MethodInfo getNeedyModulesMethod = _modManagerType.GetMethod("GetNeedyModules", BindingFlags.Instance | BindingFlags.Public);
        IList needyModuleList = getNeedyModulesMethod.Invoke(modManager, null) as IList;

        Type modNeedyComponentType = ReflectionHelper.FindTypeInGame("ModNeedyComponent");
        FieldInfo moduleField = modNeedyComponentType.GetField("module", BindingFlags.Instance | BindingFlags.NonPublic);

        foreach (object needyModule in needyModuleList)
        {
            KMNeedyModule module = moduleField.GetValue(needyModule) as KMNeedyModule;
            string moduleTypeName = module.ModuleType;

            if (!_allNeedyModules.ContainsKey(moduleTypeName))
            {
                _allNeedyModules[moduleTypeName] = new NeedyModule(module, needyModule);
            }
            else
            {
                Debug.LogErrorFormat("***** A duplicate needy module was found under the name {0}! *****", moduleTypeName);
            }
        }
    }

    private void GetActiveModules()
    {
        UnityEngine.Object modManager = ModManager;

        FieldInfo loadedBombComponentsField = _modManagerType.GetField("loadedBombComponents", BindingFlags.Instance | BindingFlags.NonPublic);
        _activeModules = loadedBombComponentsField.GetValue(modManager) as IDictionary;
    }

    private void GetModServices()
    {
        KMService[] modServices = FindObjectsOfType<KMService>();

        foreach (KMService modService in modServices)
        {
            ModSelectorService itself = modService.GetComponent<ModSelectorService>();
            if (itself != null)
            {
                //Don't add mod selector service/itself to this dictionary!
                continue;
            }

            Service service = new Service(modService);
            if (!_allServices.ContainsKey(service.ServiceName))
            {
                _allServices.Add(service.ServiceName, service);
            }
            else
            {
                Debug.LogErrorFormat("***** A duplicate service was found under the name {0}! *****", service.ServiceName);
            }
        }
    }

    private void GetModList()
    {
        UnityEngine.Object modManager = ModManager;

        FieldInfo modsField = _modManagerType.GetField("loadedMods", BindingFlags.Instance | BindingFlags.NonPublic);
        IDictionary modsList = (IDictionary)modsField.GetValue(modManager);
        foreach (object modObject in modsList.Values)
        {
            ModWrapper modWrapper = new ModWrapper(modObject);

            if (!_allMods.ContainsKey(modWrapper.ModName))
            {
                _allMods[modWrapper.ModName] = modWrapper;
            }
            else
            {
                Debug.LogErrorFormat("***** A duplicate mod was found under the name {0}! *****", modWrapper.ModName);
            }
        }
    }
    #endregion

    #region Actions
    #region General
    public ModType GetModType(string modObjectName)
    {
        if (_allSolvableModules.Values.Any((x) => modObjectName.Equals(x.ModuleType)))
        {
            return ModType.SolvableModule;
        }

        if (_allNeedyModules.Values.Any((x) => modObjectName.Equals(x.ModuleType)))
        {
            return ModType.NeedyModule;
        }

        GameObject modObject = _allMods.Values.Select((x) => x.GetModObject(modObjectName)).FirstOrDefault((y) => y != null);
        if (modObject != null)
        {
            if (modObject.GetComponent(ModWrapper.BombType) != null)
            {
                return ModType.Bomb;
            }

            if (modObject.GetComponent(ModWrapper.GameplayRoomType) != null)
            {
                return ModType.GameplayRoom;
            }

            if (modObject.GetComponent(ModWrapper.WidgetType) != null)
            {
                return ModType.Widget;
            }
        }

        if (_allServices.Values.Any((x) => x.ServiceName.Equals(modObjectName)))
        {
            return ModType.Service;
        }

        return ModType.Unknown;
    }

    public IEnumerable<ModWrapper> GetModWrappers()
    {
        return _allMods.Values.OrderBy((x) => x.ModTitle);
    }

    public IEnumerable<string> GetModNames(ModType modType)
    {
        switch (modType)
        {
            case ModType.SolvableModule:
                return _allSolvableModules.Values.Select((x) => x.ModuleType);

            case ModType.NeedyModule:
                return _allNeedyModules.Values.Select((x) => x.ModuleType);

            case ModType.Bomb:
                return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.BombType)).Select((y) => y.Key.name);

            case ModType.GameplayRoom:
                return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.GameplayRoomType)).Select((y) => y.Key.name);

            case ModType.Widget:
                return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.WidgetType)).Select((y) => y.Key.name);

            case ModType.Service:
                return _allServices.Values.Select((x) => x.ServiceName);

            default:
                return null;
        }
    }

    public IEnumerable<string> GetAllModNames()
    {
        return GetModNames(ModType.SolvableModule)
            .Concat(GetModNames(ModType.NeedyModule))
            .Concat(GetModNames(ModType.Widget))
            .Concat(GetModNames(ModType.Bomb))
            .Concat(GetModNames(ModType.GameplayRoom))
            .Concat(GetModNames(ModType.Service));
    }

    public IEnumerable<KeyValuePair<string, string>> GetModNamesAndDisplayNames(ModType modType)
    {
        switch (modType)
        {
            case ModType.SolvableModule:
                return AllSolvableModules.Select((x) => new KeyValuePair<string, string>(x.ModuleType, x.ModuleName));

            case ModType.NeedyModule:
                return AllNeedyModules.Select((x) => new KeyValuePair<string, string>(x.ModuleType, x.ModuleName));

            case ModType.Bomb:
                return AllBombMods.Select((x) => new KeyValuePair<string, string>(x.name, x.name.Replace("(Clone)", "")));

            case ModType.GameplayRoom:
                return AllGameplayRoomMods.Select((x) => new KeyValuePair<string, string>(x.name, x.name.Replace("(Clone)", "")));

            case ModType.Widget:
                return AllWidgetMods.Select((x) => new KeyValuePair<string, string>(x.name, x.name.Replace("(Clone)", "")));

            case ModType.Service:
                return AllServices.Select((x) => new KeyValuePair<string, string>(x.ServiceName, x.ServiceName.Replace("(Clone)", "")));

            default:
                return null;
        }
    }

    public IEnumerable<KeyValuePair<string, string>> GetAllModNamesAndDisplayNames()
    {
        return GetModNamesAndDisplayNames(ModType.SolvableModule)
            .Concat(GetModNamesAndDisplayNames(ModType.NeedyModule))
            .Concat(GetModNamesAndDisplayNames(ModType.Bomb))
            .Concat(GetModNamesAndDisplayNames(ModType.GameplayRoom))
            .Concat(GetModNamesAndDisplayNames(ModType.Widget))
            .Concat(GetModNamesAndDisplayNames(ModType.Service));
    }

    public string GetModuleDisplayName(string moduleType)
    {
        SolvableModule solvableModule;
        if (_allSolvableModules.TryGetValue(moduleType, out solvableModule))
        {
            return solvableModule.ModuleName;
        }
        NeedyModule needyModule;
        if (_allNeedyModules.TryGetValue(moduleType, out needyModule))
        {
            return needyModule.ModuleName;
        }
        return null;
    }

    public void EnableAll()
    {
        EnableAllModules();
        EnableAllMods();
        EnableAllServices();
    }

    public bool Disable(string modObjectName)
    {
        switch (GetModType(modObjectName))
        {
            case ModType.SolvableModule:
            case ModType.NeedyModule:
                return DisableModule(modObjectName);

            case ModType.Bomb:
                return DisableMod(ModWrapper.BombType, modObjectName);

            case ModType.GameplayRoom:
                return DisableMod(ModWrapper.GameplayRoomType, modObjectName);

            case ModType.Widget:
                return DisableMod(ModWrapper.WidgetType, modObjectName);

            case ModType.Service:
                return DisableService(modObjectName);

            default:
                Debug.LogWarningFormat("Cannot disable mod object '{0}'; Could not deduce the mod classification of '{0}'.", modObjectName);
                return false;
        }
    }
    #endregion

    #region Mods
    public IEnumerable<GameObject> AllBombMods
    {
        get
        {
            return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.BombType)).Select((y) => y.Key).OrderBy((z) => z.name);
        }
    }

    public IEnumerable<GameObject> AllGameplayRoomMods
    {
        get
        {
            return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.GameplayRoomType)).Select((y) => y.Key).OrderBy((z) => z.name);
        }
    }

    public IEnumerable<GameObject> AllWidgetMods
    {
        get
        {
            return _allMods.Values.SelectMany((x) => x.GetModObjects(ModWrapper.WidgetType)).Select((y) => y.Key).OrderBy((z) => z.name);
        }
    }

    public bool EnableMod(Type modType, string modName, string modObjectName)
    {
        if (_allMods.ContainsKey(modName))
        {
            return _allMods[modName].EnableModObject(modType, modObjectName);
        }
        else
        {
            Debug.LogErrorFormat("[ModSelector] Cannot enable mod '{1}'; Could not find a mod with name '{0}'.", modName, modObjectName);
        }

        return false;
    }

    public bool DisableMod(Type modType, string modName, string modObjectName)
    {
        if (_allMods.ContainsKey(modName))
        {
            return _allMods[modName].DisableModObject(modType, modObjectName);
        }
        else
        {
            Debug.LogErrorFormat("[ModSelector] Cannot enable mod '{1}'; Could not find a mod with name '{0}'.", modName, modObjectName);
        }

        return false;
    }

    public bool EnableMod(Type modType, string modObjectName)
    {
        return _allMods.Values.ToArray().Any((x) => x.EnableModObject(modType, modObjectName));
    }

    public bool DisableMod(Type modType, string modObjectName)
    {
        return _allMods.Values.ToArray().Any((x) => x.DisableModObject(modType, modObjectName));
    }

    public void EnableAllMods()
    {
        EnableAllMods(ModWrapper.BombType);
        EnableAllMods(ModWrapper.WidgetType);
        EnableAllMods(ModWrapper.GameplayRoomType);
    }

    public void EnableAllMods(Type modType)
    {
        foreach (ModWrapper modWrapper in _allMods.Values)
        {
            modWrapper.EnableModObjects(modType);
        }
    }

    public void DisableAllMods(Type modType)
    {
        foreach (ModWrapper modWrapper in _allMods.Values)
        {
            modWrapper.DisableModObjects(modType);
        }
    }
    #endregion

    #region Modules
    public IEnumerable<SolvableModule> AllSolvableModules
    {
        get
        {
            return _allSolvableModules.Select((x) => x.Value).OrderBy((y) => y.ModuleName);
        }
    }

    public IEnumerable<NeedyModule> AllNeedyModules
    {
        get
        {
            return _allNeedyModules.Select((x) => x.Value).OrderBy((y) => y.ModuleName);
        }
    }

    public bool EnableModule(string typeName)
    {
        if (_activeModules.Contains(typeName))
        {
            return false;
        }

        bool success = true;

        if (_allSolvableModules.ContainsKey(typeName))
        {
            _activeModules.Add(typeName, _allSolvableModules[typeName].Component);
        }
        else if (_allNeedyModules.ContainsKey(typeName))
        {
            _activeModules.Add(typeName, _allNeedyModules[typeName].Component);
        }
        else
        {
            Debug.LogErrorFormat("[ModSelector] Cannot enable module with type name '{0}'.", typeName);
            success = false;
        }

        _disabledModules.Remove(typeName);
        return success;
    }

    public bool DisableModule(string typeName)
    {
        if (!_activeModules.Contains(typeName))
        {
            return false;
        }

        _activeModules.Remove(typeName);
        _disabledModules.Add(typeName);
        return true;
    }

    public void EnableAllModules()
    {
        _activeModules.Clear();
        _disabledModules.Clear();

        foreach (KeyValuePair<string, SolvableModule> solvableModule in _allSolvableModules)
        {
            _activeModules.Add(solvableModule.Key, solvableModule.Value.Component);
        }

        foreach (KeyValuePair<string, NeedyModule> needyModule in _allNeedyModules)
        {
            _activeModules.Add(needyModule.Key, needyModule.Value.Component);
        }
    }

    public void DisableAllModules()
    {
        _activeModules.Clear();

        _disabledModules.Clear();
        _disabledModules.AddRange(_allSolvableModules.Keys);
        _disabledModules.AddRange(_allNeedyModules.Keys);
    }
    #endregion

    #region Services
    public IEnumerable<Service> AllServices
    {
        get
        {
            return _allServices.Values.OrderBy((x) => x.ServiceName);
        }
    }

    public bool EnableService(string serviceName)
    {
        if (!_allServices.ContainsKey(serviceName))
        {
            return false;
        }

        _allServices[serviceName].IsEnabled = true;
        return true;
    }

    public bool DisableService(string serviceName)
    {
        if (!_allServices.ContainsKey(serviceName))
        {
            return false;
        }

        _allServices[serviceName].IsEnabled = false;
        return true;
    }

    public void EnableAllServices()
    {
        foreach (Service service in _allServices.Values)
        {
            service.IsEnabled = true;
        }
    }

    public void DisableAllServices()
    {
        foreach (Service service in _allServices.Values)
        {
            service.IsEnabled = false;
        }
    }
    #endregion
    #endregion

    #region Public Properties
    private static ModSelectorService _instance = null;
    public static ModSelectorService Instance
    {
        get
        {
            return _instance;
        }
    }
    #endregion

    #region Private Fields & Properties
    #region Mods
    private Dictionary<string, ModWrapper> _allMods = new Dictionary<string, ModWrapper>();
    internal HashSet<string> _allExpertMods = new HashSet<string>();
    #endregion

    #region Modules
    private Dictionary<string, SolvableModule> _allSolvableModules = new Dictionary<string, SolvableModule>();
    private Dictionary<string, NeedyModule> _allNeedyModules = new Dictionary<string, NeedyModule>();

    private IDictionary _activeModules = new Dictionary<string, object>();
    private List<string> _disabledModules = new List<string>();
    #endregion

    #region Services
    private Dictionary<string, Service> _allServices = new Dictionary<string, Service>();
    #endregion

    #region Mod Manager Discovery
    private Type _modManagerType = null;

    private UnityEngine.Object _modManager = null;
    private UnityEngine.Object ModManager
    {
        get
        {
            if (_modManager == null)
            {
                _modManagerType = ReflectionHelper.FindTypeInGame("ModManager");
                _modManager = FindObjectOfType(_modManagerType);
            }

            return _modManager;
        }
    }
    #endregion

    private bool _updateRequired = true;
    #endregion
}
