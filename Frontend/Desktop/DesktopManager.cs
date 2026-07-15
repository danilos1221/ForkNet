using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class DesktopManager : MonoBehaviour, INavigableScreen
{
    [Header("UI References")]
    [SerializeField] private Canvas desktopCanvas;
    [SerializeField] private TextMeshProUGUI nicknameText;
    
    [Header("Desktop Icons")]
    [SerializeField] private Transform desktopIconsContainer;
    [SerializeField] private GameObject iconPrefab;
    
    [Header("Taskbar")]
    [SerializeField] private Transform taskbarContainer;
    [SerializeField] private GameObject taskbarItemPrefab;
    
    [Header("Window Registry")]
    [SerializeField] private List<AppWindowEntry> appWindowEntries = new();
    
    // State
    private bool iconsCreated = false;
    private DesktopIcon.AppType? currentOpenApp = null;
    
    // Runtime dictionaries
    private Dictionary<DesktopIcon.AppType, GameObject> appWindows = new();
    private Dictionary<DesktopIcon.AppType, GameObject> activeTaskbarItems = new();
    private List<DraggableIcon> draggableIcons = new();

    #region Unity Lifecycle

    private void Start()
    {
        BuildAppWindowDictionary();
        InitializeWindows();
        CreateDesktopIconsOnce();
        UpdateNicknameText();
    }

    #endregion

    #region Initialization

    private void BuildAppWindowDictionary()
    {
        appWindows.Clear();
        
        foreach (var entry in appWindowEntries)
        {
            if (entry.windowObject == null)
            {
                Debug.LogWarning($"[DesktopManager] Window for {entry.appType} is null");
                continue;
            }
            
            appWindows[entry.appType] = entry.windowObject;
        }
    }

    private void InitializeWindows()
    {
        foreach (var window in appWindows.Values)
        {
            window.SetActive(false);
        }
    }

    private void CreateDesktopIconsOnce()
    {
        if (iconsCreated) return;
        CreateDesktopIcons();
        iconsCreated = true;
    }

    private void UpdateNicknameText()
    {
        if (GameManager.Instance != null)
            nicknameText.text = GameManager.Instance.nickname;
    }

    #endregion

    #region Desktop Icons

    private void CreateDesktopIcons()
    {
        foreach (var entry in appWindowEntries)
        {
            CreateIcon(entry.appType, entry.appLabel, LoadIconSprite(entry.iconName));
        }
    }

    private Sprite LoadIconSprite(string iconName)
    {
        return Resources.Load<Sprite>($"Images/Icons/{iconName}");
    }

    private void CreateIcon(DesktopIcon.AppType type, string label, Sprite icon)
    {
        GameObject iconGO = Instantiate(iconPrefab, desktopIconsContainer, false);
        
        DesktopIcon desktopIcon = iconGO.GetComponent<DesktopIcon>();
        DraggableIcon draggable = iconGO.GetComponent<DraggableIcon>();
        
        desktopIcon?.SetupIcon(type, label, icon);
        
        if (draggable != null)
            draggableIcons.Add(draggable);
    }

    #endregion

    #region App Management

    public void OpenApp(DesktopIcon.AppType appType)
    {
        if (!appWindows.ContainsKey(appType)) return;
        
        if (TryRestoreExistingApp(appType)) return;
        
        MinimizeCurrentApp();
        CloseAllWindows();
        
        appWindows[appType].SetActive(true);
        OnAppOpened(appType);
        
        SpawnTaskbarItemIfNeeded(appType);
        SetupWindowButtonsForApp(appType);
        
        currentOpenApp = appType;
    }

    private void OnAppOpened(DesktopIcon.AppType appType)
    {
        // Здесь можно добавить специфичную логику для разных приложений
        // Например, для галереи — обновить контент при открытии
        // Для этого можно использовать события или проверять GetComponent
        
        // Пример для галереи (если появится компонент):
        // if (appWindows[appType].TryGetComponent<GalleryManager>(out var gallery))
        //     gallery.RefreshGallery();
    }

    private bool TryRestoreExistingApp(DesktopIcon.AppType appType)
    {
        if (currentOpenApp == appType && activeTaskbarItems.ContainsKey(appType))
        {
            RestoreApp(appType);
            return true;
        }
        return false;
    }

    private void MinimizeCurrentApp()
    {
        if (currentOpenApp.HasValue)
        {
            MinimizeApp(currentOpenApp.Value);
        }
    }

    private void CloseAllWindows()
    {
        foreach (var window in appWindows.Values)
        {
            window.SetActive(false);
        }
    }

    #endregion

    #region App Minimize/Restore/Close

    public void MinimizeApp(DesktopIcon.AppType appType)
    {
        if (appWindows.TryGetValue(appType, out var window))
        {
            window.SetActive(false);
        }
        currentOpenApp = null;
    }

    public void RestoreApp(DesktopIcon.AppType appType)
    {
        if (!appWindows.ContainsKey(appType)) return;
        
        if (currentOpenApp.HasValue && currentOpenApp != appType)
        {
            MinimizeApp(currentOpenApp.Value);
        }
        
        CloseAllWindows();
        appWindows[appType].SetActive(true);
        currentOpenApp = appType;
    }

    public void CloseSpecificApp(DesktopIcon.AppType appType)
    {
        if (appWindows.TryGetValue(appType, out var window))
        {
            window.SetActive(false);
        }
        
        RemoveTaskbarItem(appType);
        
        if (currentOpenApp == appType)
        {
            currentOpenApp = null;
        }
    }

    public void CloseApp()
    {
        CloseAllWindows();
        ClearAllTaskbarItems();
        currentOpenApp = null;
    }

    #endregion

    #region Navigation

    public bool TryHandleBack()
    {
        if (!currentOpenApp.HasValue) return false;
        
        // Если у окна есть компонент с навигацией, пробуем его
        if (appWindows.TryGetValue(currentOpenApp.Value, out var windowGO))
        {
            var navigables = windowGO.GetComponentsInChildren<INavigableScreen>();
            foreach (var navigable in navigables)
            {
                if (navigable.TryHandleBack())
                    return true;
            }
        }

        MinimizeApp(currentOpenApp.Value);
        return true;
    }

    #endregion

    #region Taskbar

    private void SpawnTaskbarItemIfNeeded(DesktopIcon.AppType appType)
    {
        if (activeTaskbarItems.ContainsKey(appType)) return;
        
        var entry = GetAppWindowEntry(appType);
        if (entry == null) return;
        
        SpawnTaskbarItem(appType, entry.appLabel, LoadIconSprite(entry.iconName));
    }

    private AppWindowEntry GetAppWindowEntry(DesktopIcon.AppType appType)
    {
        return appWindowEntries.FirstOrDefault(e => e.appType == appType);
    }

    private void SpawnTaskbarItem(DesktopIcon.AppType appType, string label, Sprite icon)
    {
        if (activeTaskbarItems.ContainsKey(appType)) return;
        
        GameObject taskbarItem = Instantiate(taskbarItemPrefab, taskbarContainer);
        SetupTaskbarItemVisuals(taskbarItem, icon, label);
        SetupTaskbarItemButton(taskbarItem, appType);
        
        activeTaskbarItems[appType] = taskbarItem;
    }

    private void SetupTaskbarItemVisuals(GameObject taskbarItem, Sprite icon, string label)
    {
        Image[] images = taskbarItem.GetComponentsInChildren<Image>();
        if (images.Length > 0)
        {
            Image itemImage = images.Length > 1 ? images[1] : images[0];
            itemImage.sprite = icon;
        }
        
        TextMeshProUGUI itemText = taskbarItem.GetComponentInChildren<TextMeshProUGUI>();
        if (itemText != null)
        {
            itemText.text = label;
        }
    }

    private void SetupTaskbarItemButton(GameObject taskbarItem, DesktopIcon.AppType appType)
    {
        Button taskbarButton = taskbarItem.GetComponentInChildren<Button>();
        if (taskbarButton != null)
        {
            taskbarButton.onClick.AddListener(() => RestoreApp(appType));
        }
    }

    private void RemoveTaskbarItem(DesktopIcon.AppType appType)
    {
        if (!activeTaskbarItems.TryGetValue(appType, out GameObject item)) return;
        
        Destroy(item);
        activeTaskbarItems.Remove(appType);
    }

    private void ClearAllTaskbarItems()
    {
        foreach (var item in activeTaskbarItems.Values)
        {
            Destroy(item);
        }
        activeTaskbarItems.Clear();
    }

    #endregion

    #region Window Buttons

    private void SetupWindowButtonsForApp(DesktopIcon.AppType appType)
    {
        if (!appWindows.TryGetValue(appType, out var windowGO)) return;
        
        SetupMinimizeButton(windowGO, appType);
        SetupCloseButton(windowGO, appType);
    }

    private void SetupMinimizeButton(GameObject appWindow, DesktopIcon.AppType appType)
    {
        Button minimizeBtn = FindButtonByName(appWindow, "MinimizeButton");
        if (minimizeBtn == null) return;
        
        minimizeBtn.onClick.RemoveAllListeners();
        minimizeBtn.onClick.AddListener(() => MinimizeApp(appType));
    }

    private void SetupCloseButton(GameObject appWindow, DesktopIcon.AppType appType)
    {
        Button closeBtn = FindButtonByName(appWindow, "CloseButton");
        if (closeBtn == null) return;
        
        closeBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.AddListener(() => CloseSpecificApp(appType));
    }

    private Button FindButtonByName(GameObject parent, string buttonName)
    {
        Transform buttonTransform = FindChildByName(parent.transform, buttonName);
        return buttonTransform?.GetComponent<Button>();
    }

    private Transform FindChildByName(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child.name == childName)
                return child;
        }
        return null;
    }

    #endregion
}

// Сериализуемая структура для регистрации окон в инспекторе
[System.Serializable]
public class AppWindowEntry
{
    public DesktopIcon.AppType appType;
    public string appLabel;
    public string iconName;
    public GameObject windowObject;
}