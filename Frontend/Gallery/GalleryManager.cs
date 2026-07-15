using UnityEngine;
using System.Collections.Generic;

public class GalleryManager : MonoBehaviour, INavigableScreen
{
    [Header("Панели (переключение экранов)")]
    [SerializeField] private GameObject gridPanel;              // корневой объект сетки картинок
    [SerializeField] private FullscreenGalleryView fullscreenView; // дочерний объект, теперь не префаб

    [SerializeField] private Transform imageGrid;
    [SerializeField] private GameObject galleryItemPrefab;
    [SerializeField] private List<string> allGalleryItemIds = new List<string>();
    [SerializeField] private Sprite lockedPlaceholderSprite;

    private GameData gameData;
    private readonly Dictionary<string, GalleryItemUI> galleryItemUIs = new Dictionary<string, GalleryItemUI>();
    private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    private void Awake()
    {
        // На старте фуллскрин должен быть скрыт
        if (fullscreenView != null)
            fullscreenView.gameObject.SetActive(false);
    }

    private void Start()
    {
        InitializeData();
        RefreshGallery();
        ShowGrid();
    }

    private void InitializeData()
    {
        if (gameData == null)
            gameData = GameManager.Instance.GameData;
    }

    public void RefreshGallery()
    {
        InitializeData();

        if (imageGrid == null || galleryItemPrefab == null) return;

        foreach (Transform child in imageGrid)
        {
            Destroy(child.gameObject);
        }

        galleryItemUIs.Clear();

        List<GalleryImageData> items = gameData.GetGalleryItems();
        for (int i = 0; i < items.Count; i++)
        {
            GalleryImageData item = items[i];
            if (item == null || string.IsNullOrWhiteSpace(item.itemId))
                continue;

            CreateGalleryItemUI(item);
        }
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
        if (!gameData.IsGalleryItemUnlocked(itemId))
            return;

        Sprite sprite = GetGallerySprite(itemId);
        if (sprite == null)
            return;

        OpenFullscreenView(sprite, itemId);
    }

    private void UpdateGalleryItemUI(string itemId)
    {
        if (!galleryItemUIs.TryGetValue(itemId, out GalleryItemUI itemUI) || itemUI == null)
            return;

        GalleryImageData data = gameData.GetGalleryItem(itemId);
        if (data == null)
            return;

        Sprite unlockedSprite = data.isUnlocked ? GetGallerySprite(itemId) : null;
        itemUI.UpdateVisual(data.isUnlocked, unlockedSprite, lockedPlaceholderSprite);
    }

    private Sprite GetGallerySprite(string itemId)
    {
        if (spriteCache.TryGetValue(itemId, out Sprite cached))
            return cached;

        Sprite loaded = Resources.Load<Sprite>($"Images/Gallery/{itemId}");
        spriteCache[itemId] = loaded;
        return loaded;
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
        InitializeData();

        bool changed = gameData.UnlockGalleryItem(itemId);
        if (!changed)
            return;

        if (!galleryItemUIs.ContainsKey(itemId))
        {
            GalleryImageData item = gameData.GetGalleryItem(itemId);
            if (item != null)
                CreateGalleryItemUI(item);
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