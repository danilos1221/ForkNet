using UnityEngine;
using System.Collections.Generic;

public class GalleryManager : MonoBehaviour, INavigableScreen
{
    [Header("Панели (переключение экранов)")]
    [SerializeField] private GameObject gridPanel;              // корневой объект сетки картинок
    [SerializeField] private FullscreenGalleryView fullscreenView; // дочерний объект, теперь не префаб

    [SerializeField] private Transform imageGrid;
    [SerializeField] private GameObject galleryItemPrefab;
    [SerializeField] private Sprite lockedPlaceholderSprite;

    private readonly Dictionary<string, GalleryItemUI> galleryItemUIs = new();

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────

    private void Awake()
    {
        if (fullscreenView != null)
            fullscreenView.gameObject.SetActive(false);
    }

    private void Start()
    {
        RefreshGallery();
        ShowGrid();
    }

    /// <summary>
    /// Вызывается извне (например, GameLoadManager) после того как сохранение применено.
    /// SaveSystem.ApplyLoadedSave полностью заменяет gameData.galleryItems данными из сейва,
    /// поэтому после загрузки нужно пересобрать UI, чтобы отобразить актуально
    /// разблокированные изображения.
    /// </summary>
    public void RefreshAfterLoad()
    {
        RefreshGallery();
        ShowGrid();
    }

    private void OnEnable()
    {
        if (GalleryService.Instance != null)
            GalleryService.Instance.OnItemUnlocked += OnItemUnlockedHandler;

        // Пока этот объект был неактивен (например, открыт другой app-window),
        // подписки на OnItemUnlocked не было — картинки могли разблокироваться
        // в GameData, но UI об этом не узнал. Досчитываем актуальное состояние.
        RefreshGallery();
    }

    private void OnDisable()
    {
        if (GalleryService.Instance != null)
            GalleryService.Instance.OnItemUnlocked -= OnItemUnlockedHandler;
    }

    public void RefreshGallery()
    {
        if (imageGrid == null || galleryItemPrefab == null)
        {
            Debug.LogWarning("[GalleryManager] imageGrid или galleryItemPrefab не назначены. Этот экземпляр галереи не будет рендерить карточки.");
            return;
        }
        if (GalleryService.Instance == null) return;

        foreach (Transform child in imageGrid)
            Destroy(child.gameObject);

        galleryItemUIs.Clear();

        List<GalleryImageData> items = GalleryService.Instance.GetAllItems();
        int createdCount = 0;
        foreach (GalleryImageData item in items)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.itemId)) continue;
            if (!item.isUnlocked) continue;
            CreateGalleryItemUI(item);
            createdCount++;
        }

        Debug.Log($"[GalleryManager] RefreshGallery: unlocked items rendered = {createdCount}");
    }

    private void CreateGalleryItemUI(GalleryImageData item)
    {
        GameObject itemGO = Instantiate(galleryItemPrefab, imageGrid);
        GalleryItemUI itemUI = itemGO.GetComponent<GalleryItemUI>();

        if (itemUI == null)
            return;

        itemUI.Initialize(item.itemId, SelectItem);
        galleryItemUIs[item.itemId] = itemUI;
        UpdateGalleryItemUI(item.itemId);
    }

    private void SelectItem(string itemId)
    {
        if (!GalleryService.Instance.IsUnlocked(itemId)) return;
        Sprite sprite = GalleryService.Instance.GetSprite(itemId);
        if (sprite == null) return;
        OpenFullscreenView(sprite, itemId);
    }

    private void UpdateGalleryItemUI(string itemId)
    {
        if (!galleryItemUIs.TryGetValue(itemId, out GalleryItemUI itemUI) || itemUI == null) return;
        GalleryImageData data = GalleryService.Instance.GetItem(itemId);
        if (data == null) return;
        Sprite unlockedSprite = data.isUnlocked ? GalleryService.Instance.GetSprite(itemId) : null;
        itemUI.UpdateVisual(data.isUnlocked, unlockedSprite, lockedPlaceholderSprite);
    }

    private void OpenFullscreenView(Sprite sprite, string itemId)
    {
        if (fullscreenView == null)
        {
            Debug.LogError("[GalleryManager] fullscreenView не назначен!");
            return;
        }

        fullscreenView.Initialize(sprite, itemId, OnFullscreenClosed);
        ShowFullscreenInternal();
    }

    public void OpenFullscreenViewForImage(Sprite sprite, string itemId)
    {
        OpenFullscreenView(sprite, itemId);
    }

    public void UnlockGalleryItem(string itemId)
    {
        // Делегируем в сервис. Обновление UI случится в OnItemUnlockedHandler через событие.
        GalleryService.Instance?.UnlockItem(itemId);
    }

    // Обработчик события GalleryService.OnItemUnlocked
    private void OnItemUnlockedHandler(string itemId)
    {
        if (!galleryItemUIs.ContainsKey(itemId))
        {
            GalleryImageData item = GalleryService.Instance.GetItem(itemId);
            if (item != null) CreateGalleryItemUI(item);
            return;
        }
        UpdateGalleryItemUI(itemId);
    }

    // ──────────────────────────────────────────────
    // Переключение экранов: сетка <-> фуллскрин
    // ──────────────────────────────────────────────

    private void ShowGrid()
    {
        if (fullscreenView != null) fullscreenView.gameObject.SetActive(false);
        if (gridPanel != null) gridPanel.SetActive(true);
    }

    private void ShowFullscreenInternal()
    {
        if (gridPanel != null) gridPanel.SetActive(false);
        // fullscreenView сам себя активирует внутри Initialize()
    }

    /// <summary>Callback, когда пользователь закрыл фуллскрин через его собственную кнопку Close.</summary>
    private void OnFullscreenClosed()
    {
        ShowGrid();
    }

    // ──────────────────────────────────────────────
    // INavigableScreen — обработка кнопки "назад"
    // ──────────────────────────────────────────────

    public bool TryHandleBack()
    {
        if (fullscreenView != null && fullscreenView.gameObject.activeSelf)
        {
            fullscreenView.Close(); // запустит fade-out и в конце вызовет OnFullscreenClosed() -> ShowGrid()
            return true;
        }

        return false; // мы уже на сетке — пусть DesktopManager сворачивает приложение
    }
}